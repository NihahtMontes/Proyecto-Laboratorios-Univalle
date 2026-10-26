using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Confianza de la propuesta generada por <see cref="CatalogClassificationInference"/>.
    /// </summary>
    public enum InferenceConfidence
    {
        /// <summary>Regla aplicada sin contradicciones.</summary>
        High,

        /// <summary>Regla aplicada con senales debiles o incompletas.</summary>
        Medium,

        /// <summary>Requiere revision humana (contradiccion o clase incierta).</summary>
        Review
    }

    /// <summary>
    /// Datos de entrada para inferir la clasificacion de un registro de catalogo.
    /// </summary>
    public sealed record CatalogClassificationInput(
        string CatalogCode,
        string Name,
        bool AnyUnitHasSerial,
        string? SourceTypeHint,
        string? LinkedArticleCategory);

    /// <summary>
    /// Propuesta de clasificacion para un registro de catalogo.
    /// Siempre cumple <see cref="EquipmentClassificationRules.IsConfirmedCombinationValid"/>.
    /// </summary>
    public sealed record CatalogClassificationProposal(
        EquipmentCategory Category,
        EquipmentTypeClassification? TypeClassification,
        UtensilType? UtensilType,
        string? OtherDetail,
        InferenceConfidence Confidence,
        string Rule,
        string Reason);

    /// <summary>
    /// Clasificador puro y determinista de registros de catalogo (Equipo / Utensilio / Otro).
    /// Orden de decision: hint del Excel, nombres compuestos, tabla de palabras clave,
    /// ajustes por articulo vinculado, regla dura de numero de serie y fallback.
    /// </summary>
    public static class CatalogClassificationInference
    {
        // Clases de maquinaria/electronica: un articulo "utensilio" no las degrada,
        // solo marca la contradiccion para revision.
        private static readonly HashSet<EquipmentTypeClassification> MachineryClasses = new()
        {
            EquipmentTypeClassification.Electronico,
            EquipmentTypeClassification.Informatico,
            EquipmentTypeClassification.Audiovisuales,
            EquipmentTypeClassification.Calor,
            EquipmentTypeClassification.Frio,
            EquipmentTypeClassification.Congelacion,
            EquipmentTypeClassification.MaquinasRotativas,
            EquipmentTypeClassification.Electricos,
            EquipmentTypeClassification.Medicion,
            EquipmentTypeClassification.SeguridadIndustrial
        };

        // Primera palabra del nombre que indica mobiliario.
        private static readonly string[] MobiliarioFirstWords =
        {
            "mesa", "meson", "silla", "sillon", "escritorio", "mueble", "estante", "repisa",
            "vitrina", "casillero", "banca", "taburete", "pizarra", "gavetero", "credenza"
        };

        private sealed record NameMatch(
            EquipmentCategory Category,
            EquipmentTypeClassification? TypeClassification,
            UtensilType? UtensilType,
            string? OtherDetail,
            InferenceConfidence Confidence,
            string Rule,
            string Reason);

        /// <summary>
        /// Infiere la clasificacion de un registro de catalogo. Funcion pura: mismos datos de
        /// entrada, misma propuesta de salida, sin estado ni I/O.
        /// </summary>
        public static CatalogClassificationProposal Infer(CatalogClassificationInput input)
        {
            var nameTokens = Tokenize(Normalize(input.Name));
            var hintTokens = Tokenize(Normalize(input.SourceTypeHint));
            var articleTokens = Tokenize(Normalize(input.LinkedArticleCategory));

            // A. Columna "Tipoutensilio" del Excel del cliente: maxima prioridad.
            if (HasPrefix(hintTokens, "ventilacion") || HasPrefix(hintTokens, "aclimatiz"))
            {
                return new CatalogClassificationProposal(
                    EquipmentCategory.Equipment,
                    EquipmentTypeClassification.Electricos,
                    null,
                    null,
                    InferenceConfidence.High,
                    "HINT_VENTILACION",
                    "La columna Tipoutensilio indica ventilacion o aclimatacion, se clasifica como equipo electrico.");
            }

            if ((HasPrefix(hintTokens, "equipo") && HasWord(hintTokens, "calor")))
            {
                return new CatalogClassificationProposal(
                    EquipmentCategory.Equipment,
                    EquipmentTypeClassification.Calor,
                    null,
                    null,
                    InferenceConfidence.Review,
                    "HINT_CALOR",
                    "La columna Tipoutensilio dice equipo de calor y el item puede ser una garrafa, se debe revisar.");
            }

            var articleSaysEquipment = HasPrefix(articleTokens, "equipo");
            var articleSaysUtensil = articleTokens.Count > 0 && !articleSaysEquipment;

            // B y C. Nombres compuestos primero y luego tabla de palabras clave.
            var match = MatchCompoundName(nameTokens) ?? MatchKeywordName(nameTokens, articleSaysUtensil);

            CatalogClassificationProposal proposal;

            if (match is null)
            {
                if (articleSaysEquipment)
                {
                    proposal = new CatalogClassificationProposal(
                        EquipmentCategory.Equipment,
                        EquipmentTypeClassification.Otro,
                        null,
                        "Por articulo vinculado (Equipo)",
                        InferenceConfidence.Review,
                        "ARTICLE_EQUIPO",
                        "Ninguna regla del nombre aplico y el articulo vinculado es de tipo Equipo.");
                }
                else if (articleSaysUtensil)
                {
                    proposal = new CatalogClassificationProposal(
                        EquipmentCategory.Utensil,
                        null,
                        MapUtensilTypeFromArticle(articleTokens, nameTokens),
                        null,
                        InferenceConfidence.Medium,
                        "ARTICLE_UTENSIL",
                        "Ninguna regla del nombre aplico y el articulo vinculado lo clasifica como utensilio.");
                }
                else
                {
                    proposal = new CatalogClassificationProposal(
                        EquipmentCategory.Equipment,
                        EquipmentTypeClassification.Otro,
                        null,
                        "Sin regla para el nombre: revisar",
                        InferenceConfidence.Review,
                        "NAME_UNMATCHED",
                        "No existe regla para el nombre ni articulo vinculado, requiere revision manual.");
                }
            }
            else if (match.Category == EquipmentCategory.Utensil)
            {
                proposal = new CatalogClassificationProposal(
                    match.Category,
                    null,
                    match.UtensilType,
                    match.OtherDetail,
                    match.Confidence,
                    match.Rule,
                    match.Reason);
            }
            else
            {
                proposal = ApplyArticleAdjustments(match, articleTokens, articleSaysEquipment, articleSaysUtensil, nameTokens, input.AnyUnitHasSerial);
            }

            // E. Regla dura: con numero de serie jamas se propone Utensilio.
            if (proposal.Category == EquipmentCategory.Utensil && input.AnyUnitHasSerial)
            {
                EquipmentTypeClassification? equipmentType = null;
                if (match is { Category: EquipmentCategory.Equipment } && match.TypeClassification.HasValue)
                    equipmentType = match.TypeClassification.Value;

                proposal = equipmentType.HasValue
                    ? new CatalogClassificationProposal(
                        EquipmentCategory.Equipment,
                        equipmentType,
                        null,
                        null,
                        InferenceConfidence.Review,
                        "SERIAL_UTENSIL_TO_EQUIPMENT",
                        "Tiene numero de serie por lo que no puede ser utensilio, se reclasifica como equipo para revision.")
                    : new CatalogClassificationProposal(
                        EquipmentCategory.Equipment,
                        EquipmentTypeClassification.Otro,
                        null,
                        "Tiene numero de serie: revisar clasificacion",
                        InferenceConfidence.Review,
                        "SERIAL_UTENSIL_TO_EQUIPMENT",
                        "Tiene numero de serie por lo que no puede ser utensilio, se reclasifica como equipo por revisar.");
            }

            return proposal;
        }

        /// <summary>D. Ajustes por la categoria del articulo vinculado sobre un match de equipo.</summary>
        private static CatalogClassificationProposal ApplyArticleAdjustments(
            NameMatch match,
            IReadOnlyList<string> articleTokens,
            bool articleSaysEquipment,
            bool articleSaysUtensil,
            IReadOnlyList<string> nameTokens,
            bool anyUnitHasSerial)
        {
            var type = match.TypeClassification!.Value;

            // El articulo confirma "Equipo": mantiene la clase y sube la confianza
            // (los casos marcados Review se mantienen en Review).
            if (articleSaysEquipment)
            {
                var confidence = match.Confidence == InferenceConfidence.Review
                    ? InferenceConfidence.Review
                    : InferenceConfidence.High;

                return new CatalogClassificationProposal(
                    EquipmentCategory.Equipment, type, null, match.OtherDetail, confidence, match.Rule, match.Reason);
            }

            // Maquinaria/electronica contradicha por un articulo utensilio: queda en revision.
            if (articleSaysUtensil && MachineryClasses.Contains(type))
            {
                return new CatalogClassificationProposal(
                    EquipmentCategory.Equipment, type, null, match.OtherDetail, InferenceConfidence.Review, match.Rule,
                    "El nombre indica maquinaria o equipo electrico pero el articulo vinculado dice utensilio, se mantiene como equipo para revision.");
            }

            // Mobiliario + articulo utensilio sin serie: se propone utensilio.
            if (articleSaysUtensil && type == EquipmentTypeClassification.Mobiliario && !anyUnitHasSerial)
            {
                return new CatalogClassificationProposal(
                    EquipmentCategory.Utensil, null, MapUtensilTypeFromArticle(articleTokens, nameTokens), null,
                    InferenceConfidence.Medium, "ARTICLE_UTENSIL",
                    "El nombre parece mobiliario pero el articulo vinculado lo clasifica como utensilio.");
            }

            // Mobiliario contradicho pero con numero de serie: queda como equipo en revision.
            if (articleSaysUtensil && type == EquipmentTypeClassification.Mobiliario)
            {
                return new CatalogClassificationProposal(
                    EquipmentCategory.Equipment, type, null, match.OtherDetail, InferenceConfidence.Review, match.Rule,
                    "El articulo vinculado dice utensilio pero el registro tiene numero de serie, se mantiene como equipo para revision.");
            }

            return new CatalogClassificationProposal(
                EquipmentCategory.Equipment, type, null, match.OtherDetail, match.Confidence, match.Rule, match.Reason);
        }

        /// <summary>B. Nombres compuestos que deben ganar antes que la regla generica de primera palabra.</summary>
        private static NameMatch? MatchCompoundName(IReadOnlyList<string> tokens)
        {
            if (tokens.Count == 0)
                return null;

            if ((HasPrefix(tokens, "meson") || HasPrefix(tokens, "mesa")) && HasPrefix(tokens, "refrigerad"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Frio, null, null,
                    InferenceConfidence.High, "NAME_MESON_REFRIGERADOR",
                    "Meson refrigerador: manda refrigerador antes que el mobiliario.");
            }

            if (HasPrefix(tokens, "camara"))
            {
                if (HasPrefix(tokens, "congela"))
                {
                    return new NameMatch(
                        EquipmentCategory.Equipment, EquipmentTypeClassification.Congelacion, null, null,
                        InferenceConfidence.High, "NAME_CAMARA_CONGELACION",
                        "Camara de congelacion.");
                }

                if (HasPrefix(tokens, "conservador"))
                {
                    return new NameMatch(
                        EquipmentCategory.Equipment, EquipmentTypeClassification.Frio, null, null,
                        InferenceConfidence.High, "NAME_CAMARA_CONSERVADORA",
                        "Camara conservadora: equipo de frio.");
                }

                if (HasWord(tokens, "video"))
                {
                    return new NameMatch(
                        EquipmentCategory.Equipment, EquipmentTypeClassification.Audiovisuales, null, null,
                        InferenceConfidence.High, "NAME_CAMARA_VIDEO",
                        "Camara de video: equipo audiovisual.");
                }
            }

            return null;
        }

        /// <summary>C. Tabla de palabras clave sobre el nombre normalizado (primer match gana).</summary>
        private static NameMatch? MatchKeywordName(IReadOnlyList<string> tokens, bool articleAgreesUtensil)
        {
            if (tokens.Count == 0)
                return null;

            // Mobiliario por primera palabra ("estante porta bandejas" queda aqui, antes que "porta").
            if (StartsWithAny(tokens[0], MobiliarioFirstWords))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Mobiliario, null, null,
                    InferenceConfidence.High, "NAME_MOBILIARIO",
                    "La primera palabra del nombre corresponde a mobiliario.");
            }

            if (HasPrefix(tokens, "extintor"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.SeguridadIndustrial, null, null,
                    InferenceConfidence.High, "NAME_EXTINTOR",
                    "Extintor: equipo de seguridad industrial.");
            }

            if (HasPrefix(tokens, "refrigerador") || HasPrefix(tokens, "abatidor"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Frio, null, null,
                    InferenceConfidence.High, "NAME_FRIO",
                    "El nombre corresponde a un equipo de frio.");
            }

            if (HasPrefix(tokens, "teclado") || HasWord(tokens, "cpu") || HasPrefix(tokens, "monitor")
                || HasPhrase(tokens, "all", "in", "one") || HasPrefix(tokens, "router")
                || HasPhrase(tokens, "acces", "point") || HasPhrase(tokens, "access", "point"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Informatico, null, null,
                    InferenceConfidence.High, "NAME_INFORMATICO",
                    "El nombre corresponde a un equipo informatico.");
            }

            if (HasPrefix(tokens, "televisor") || HasPrefix(tokens, "proyector"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Audiovisuales, null, null,
                    InferenceConfidence.High, "NAME_AUDIOVISUAL",
                    "El nombre corresponde a un equipo audiovisual.");
            }

            // El panel del sistema define "Equipos electronicos (balanzas)": las balanzas son Electronico.
            if (HasPrefix(tokens, "balanza"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Electronico, null, null,
                    InferenceConfidence.High, "NAME_BALANZA",
                    "Balanza: equipo electronico (el panel de equipos agrupa las balanzas como electronicos).");
            }

            if (HasPrefix(tokens, "refractometro"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Medicion, null, null,
                    InferenceConfidence.High, "NAME_MEDICION",
                    "El nombre corresponde a un instrumental de medicion.");
            }

            if (HasPrefix(tokens, "extractor") || HasPrefix(tokens, "campana") || HasPrefix(tokens, "envasadora")
                || HasPrefix(tokens, "laminadora") || HasPrefix(tokens, "fermentador")
                || (HasPrefix(tokens, "maquina") && HasPrefix(tokens, "helado")))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Electricos, null, null,
                    InferenceConfidence.High, "NAME_ELECTRICO",
                    "El nombre corresponde a un equipo electrico.");
            }

            if (HasPrefix(tokens, "bebedero"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Electricos, null, null,
                    InferenceConfidence.Review, "NAME_BEDEDERO",
                    "Bebedero: dispensador de agua con categoria incierta, revisar.");
            }

            if ((HasPrefix(tokens, "filtro") || HasPrefix(tokens, "flitro")) && HasPrefix(tokens, "ablandador"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Electricos, null, null,
                    InferenceConfidence.Review, "NAME_FILTRO_ABLANDADOR",
                    "Filtro ablandador (incluye el typo flitro): clase incierta, revisar.");
            }

            if (HasPrefix(tokens, "horno") || HasPrefix(tokens, "freidora") || HasPrefix(tokens, "microonda")
                || HasPrefix(tokens, "cafetera") || HasPhrase(tokens, "cocina", "industrial")
                || HasPhrase(tokens, "sous", "vide"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Calor, null, null,
                    InferenceConfidence.High, "NAME_CALOR",
                    "El nombre corresponde a un equipo de calor.");
            }

            if (HasPhrase(tokens, "termo", "tanque") || HasPrefix(tokens, "termotanque"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Calor, null, null,
                    InferenceConfidence.Review, "NAME_TERMO_TANQUE",
                    "Termo tanque: clase incierta, revisar.");
            }

            if (HasPrefix(tokens, "garrafa"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Calor, null, null,
                    InferenceConfidence.Review, "NAME_GARRAFA",
                    "Garrafa: recipiente de clase incierta, revisar.");
            }

            if (HasPrefix(tokens, "licuadora") || HasPrefix(tokens, "batidora") || HasPrefix(tokens, "amasadora")
                || HasWord(tokens, "mixer") || HasPrefix(tokens, "procesador") || HasPrefix(tokens, "moledora")
                || HasPrefix(tokens, "molino") || HasWord(tokens, "thermomix") || HasWord(tokens, "termomix"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.MaquinasRotativas, null, null,
                    InferenceConfidence.High, "NAME_MAQUINA_ROTATIVA",
                    "El nombre corresponde a una maquina rotativa.");
            }

            if (HasPhrase(tokens, "aparato", "telefonico"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Electronico, null, null,
                    InferenceConfidence.High, "NAME_ELECTRONICO",
                    "Aparato telefonico: equipo electronico.");
            }

            if (HasPrefix(tokens, "lavaplatos") || HasPrefix(tokens, "bacha"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Otro, null,
                    "Sanitario / hidraulico (lavado)",
                    InferenceConfidence.Medium, "NAME_SANITARIO",
                    "Lavaplatos o bacha: sanitario/hidraulico para lavado.");
            }

            if (HasPrefix(tokens, "carrito"))
            {
                return new NameMatch(
                    EquipmentCategory.Equipment, EquipmentTypeClassification.Mobiliario, null, null,
                    InferenceConfidence.Review, "NAME_CARRITO",
                    "Carrito: mobiliario movil, verificar la categoria.");
            }

            // Utensilios por nombre.
            if (tokens[0].StartsWith("porta", StringComparison.Ordinal)
                && !tokens[0].StartsWith("portatil", StringComparison.Ordinal))
            {
                return new NameMatch(
                    EquipmentCategory.Utensil, null, UtensilType.MenajeCocina, null,
                    articleAgreesUtensil ? InferenceConfidence.High : InferenceConfidence.Medium,
                    "NAME_PORTA",
                    "La primera palabra es porta, corresponde a un utensilio de menaje de cocina.");
            }

            if (HasPrefix(tokens, "pinza"))
            {
                return new NameMatch(
                    EquipmentCategory.Utensil, null, UtensilType.Metal, null,
                    InferenceConfidence.Medium, "NAME_PINZA",
                    "Pinza: utensilio de metal.");
            }

            return null;
        }

        /// <summary>D. Mapea la familia del articulo vinculado a un <see cref="UtensilType"/> valido.</summary>
        private static UtensilType MapUtensilTypeFromArticle(IReadOnlyList<string> articleTokens, IReadOnlyList<string> nameTokens)
        {
            if (HasPrefix(articleTokens, "cristaleria"))
                return UtensilType.Vidrio;

            if (HasPrefix(articleTokens, "vajilla"))
                return UtensilType.VajillaGeneral;

            if (HasPrefix(articleTokens, "cubiertos"))
                return UtensilType.Servicio;

            if (HasPrefix(articleTokens, "bartending") || HasPrefix(articleTokens, "servicio"))
                return UtensilType.BartendingBarismo;

            if (HasPrefix(articleTokens, "reposteria") || HasPrefix(articleTokens, "panaderia")
                || HasPrefix(articleTokens, "pasteleria"))
                return UtensilType.PanaderiaReposteriaPasteleria;

            if (HasPrefix(articleTokens, "preparacion") || HasPrefix(articleTokens, "coccion"))
                return UtensilType.MenajeCocina;

            // "Utensilio", "Material operativo" y cualquier otra familia: por material del nombre.
            return HasMetalToken(nameTokens) ? UtensilType.Metal : UtensilType.MenajeCocina;
        }

        private static bool HasMetalToken(IReadOnlyList<string> tokens) =>
            HasPrefix(tokens, "metal") || HasPrefix(tokens, "acero") || HasPrefix(tokens, "inox");

        /// <summary>Minusculas, sin diacriticos y con espacios colapsados.</summary>
        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            var formD = value.Trim().Normalize(NormalizationForm.FormD);
            foreach (var c in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            var lowered = builder.ToString().ToLowerInvariant();
            return Regex.Replace(lowered, @"\s+", " ").Trim();
        }

        /// <summary>Divide el texto normalizado en tokens de letras/digitos ("cod. 32516" => [cod, 32516]).</summary>
        private static List<string> Tokenize(string normalized)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            foreach (var c in normalized)
            {
                if (char.IsLetterOrDigit(c))
                {
                    current.Append(c);
                }
                else if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens;
        }

        private static bool HasWord(IReadOnlyList<string> tokens, string word) =>
            tokens.Contains(word);

        private static bool HasPrefix(IReadOnlyList<string> tokens, string prefix) =>
            tokens.Any(t => t.StartsWith(prefix, StringComparison.Ordinal));

        private static bool HasPhrase(IReadOnlyList<string> tokens, params string[] words)
        {
            if (words.Length == 0 || tokens.Count < words.Length)
                return false;

            for (var i = 0; i + words.Length <= tokens.Count; i++)
            {
                var matches = true;
                for (var j = 0; j < words.Length; j++)
                {
                    if (!string.Equals(tokens[i + j], words[j], StringComparison.Ordinal))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return true;
            }

            return false;
        }

        private static bool StartsWithAny(string token, IReadOnlyList<string> prefixes) =>
            prefixes.Any(prefix => token.StartsWith(prefix, StringComparison.Ordinal));
    }
}
