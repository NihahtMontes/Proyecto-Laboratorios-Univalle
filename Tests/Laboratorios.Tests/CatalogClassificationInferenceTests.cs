using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Laboratorios.Tests;

/// <summary>
/// Pruebas del clasificador puro de catalogo: decide Equipo/Utensilio/Otro a partir del nombre,
/// el hint del Excel (Tipoutensilio) y la categoria del articulo vinculado.
/// </summary>
public class CatalogClassificationInferenceTests
{
    private static CatalogClassificationProposal Infer(
        string name,
        bool anyUnitHasSerial = false,
        string? sourceTypeHint = null,
        string? linkedArticleCategory = null) =>
        CatalogClassificationInference.Infer(
            new CatalogClassificationInput("CAT-TEST", name, anyUnitHasSerial, sourceTypeHint, linkedArticleCategory));

    private static void AssertValidCombination(CatalogClassificationProposal proposal)
    {
        Assert.True(
            EquipmentClassificationRules.IsConfirmedCombinationValid(
                proposal.Category, proposal.TypeClassification, proposal.UtensilType, proposal.OtherDetail),
            $"Combinacion invalida ({proposal.Rule}): {proposal.Category}/{proposal.TypeClassification}/{proposal.UtensilType}/{proposal.OtherDetail}");
    }

    [Theory]
    [InlineData("Maquinas para helados", EquipmentTypeClassification.Electricos)]
    [InlineData("Termotanque", EquipmentTypeClassification.Calor)]
    [InlineData("Termomix", EquipmentTypeClassification.MaquinasRotativas)]
    [InlineData("Mesa refrigerada", EquipmentTypeClassification.Frio)]
    [InlineData("Meson refrigerado", EquipmentTypeClassification.Frio)]
    public void VariantesDeNombre_ClasificanComoEquipo(string name, EquipmentTypeClassification expected)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(expected, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void PistaEquiposDeCalorEnPlural_SeMarcaParaRevision()
    {
        var proposal = Infer("Garrafa", sourceTypeHint: "Equipos de calor");
        Assert.Equal(EquipmentTypeClassification.Calor, proposal.TypeClassification);
        Assert.Equal(InferenceConfidence.Review, proposal.Confidence);
    }

    [Fact]
    public void Extintor_EsSeguridadIndustrial()
    {
        var proposal = Infer("Extintor PQS 6 kg");
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.SeguridadIndustrial, proposal.TypeClassification);
        Assert.Equal(InferenceConfidence.High, proposal.Confidence);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Mesa de trabajo inoxidable")]
    [InlineData("Silla plastica")]
    [InlineData("Escritorio ejecutivo")]
    [InlineData("Mueble para computadora")]
    public void Mobiliario_PorPrimeraPalabra_IncluyeMuebleParaComputadora(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Mobiliario, proposal.TypeClassification);
        Assert.Equal(InferenceConfidence.High, proposal.Confidence);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Teclado inalambrico")]
    [InlineData("Monitor Samsung 24")]
    [InlineData("All in one HP")]
    public void Informatico_PorNombre(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Informatico, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Televisor LED 50")]
    [InlineData("Proyector Epson")]
    [InlineData("Camara de video")]
    public void Audiovisuales_PorNombre(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Audiovisuales, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Balanza digital")]
    [InlineData("Balanza electronica")]
    [InlineData("Balanza")]
    public void Balanza_EsElectronico(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Electronico, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Refractometro")]
    public void Medicion_PorNombre(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Medicion, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Horno convector")]
    [InlineData("Microonda")]
    [InlineData("Cafetera ind")]
    public void Calor_PorNombre(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Calor, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Refrigerador combi")]
    [InlineData("Meson refrigerador")]
    [InlineData("Camara conservadora")]
    public void Frio_PorNombre_MesonRefrigeradorAntesQueMobiliario(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Frio, proposal.TypeClassification);
        Assert.NotEqual(EquipmentTypeClassification.Mobiliario, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Camara congeladora")]
    [InlineData("Camara congelacion")]
    [InlineData("Camara congeladora de mariscos")]
    public void CamaraCongeladora_EsCongelacion(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Congelacion, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Licuadora industrial")]
    [InlineData("Batidora planetaria")]
    [InlineData("Thermomix TM5")]
    public void MaquinasRotativas_PorNombre(string name)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.MaquinasRotativas, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void Extractor_ConHintVentilacion_EsElectricosAlta()
    {
        var proposal = Infer("Extractor de humos y grasas", sourceTypeHint: "Ventilación");
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Electricos, proposal.TypeClassification);
        Assert.Equal(InferenceConfidence.High, proposal.Confidence);
        Assert.Equal("HINT_VENTILACION", proposal.Rule);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void Garrafa_ConYSinHint_MismaCategoriaTipoYReview()
    {
        var sinHint = Infer("Garrafa");
        var conHint = Infer("Garrafa", sourceTypeHint: "Equipo de calor");

        // Ambas filas "Garrafa" deben caer en la misma clase, marcadas para revision.
        Assert.Equal(EquipmentCategory.Equipment, sinHint.Category);
        Assert.Equal(EquipmentCategory.Equipment, conHint.Category);
        Assert.Equal(sinHint.TypeClassification, conHint.TypeClassification);
        Assert.Equal(EquipmentTypeClassification.Calor, sinHint.TypeClassification);
        Assert.Equal(InferenceConfidence.Review, sinHint.Confidence);
        Assert.Equal(InferenceConfidence.Review, conHint.Confidence);
        Assert.Equal("NAME_GARRAFA", sinHint.Rule);
        Assert.Equal("HINT_CALOR", conHint.Rule);
        AssertValidCombination(sinHint);
        AssertValidCombination(conHint);
    }

    [Fact]
    public void PinzaKelly_EsUtensilioMetal()
    {
        var proposal = Infer("Pinza kelly");
        Assert.Equal(EquipmentCategory.Utensil, proposal.Category);
        Assert.Equal(UtensilType.Metal, proposal.UtensilType);
        Assert.Null(proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void PortaTablas_EsUtensilioMenajeCocina()
    {
        var proposal = Infer("Porta tablas");
        Assert.Equal(EquipmentCategory.Utensil, proposal.Category);
        Assert.Equal(UtensilType.MenajeCocina, proposal.UtensilType);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void PortaBandejas_ConArticuloUtensilio_EsAltaConfianza()
    {
        var proposal = Infer("Porta bandejas", linkedArticleCategory: "Utensilio");
        Assert.Equal(EquipmentCategory.Utensil, proposal.Category);
        Assert.Equal(UtensilType.MenajeCocina, proposal.UtensilType);
        Assert.Equal(InferenceConfidence.High, proposal.Confidence);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void TabureteMetalico_ArticuloUtensilioSinSerie_EsUtensilioMetal()
    {
        var proposal = Infer("Taburete metalico", linkedArticleCategory: "Utensilio");
        Assert.Equal(EquipmentCategory.Utensil, proposal.Category);
        Assert.Equal(UtensilType.Metal, proposal.UtensilType);
        Assert.Equal("ARTICLE_UTENSIL", proposal.Rule);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void TabureteMetalico_ArticuloUtensilioConSerie_NuncaEsUtensilio()
    {
        var proposal = Infer("Taburete metalico", anyUnitHasSerial: true, linkedArticleCategory: "Utensilio");
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.NotEqual(EquipmentCategory.Utensil, proposal.Category);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void LaminadoraDeMasas_ArticuloUtensilio_EquipoElectricosReview()
    {
        var proposal = Infer("Laminadora de masas cod. 32516", linkedArticleCategory: "Utensilio");
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Electricos, proposal.TypeClassification);
        Assert.Equal(InferenceConfidence.Review, proposal.Confidence);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void Lavaplatos_EsEquipoOtroConDetalle()
    {
        var proposal = Infer("Lavaplatos de acero inoxidable");
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Otro, proposal.TypeClassification);
        Assert.False(string.IsNullOrWhiteSpace(proposal.OtherDetail));
        AssertValidCombination(proposal);
    }

    [Fact]
    public void NombreSinReglaNiArticulo_FallbackReview()
    {
        var proposal = Infer("Bandeja de presentacion X-99");
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(EquipmentTypeClassification.Otro, proposal.TypeClassification);
        Assert.Equal(InferenceConfidence.Review, proposal.Confidence);
        Assert.Equal("NAME_UNMATCHED", proposal.Rule);
        Assert.False(string.IsNullOrWhiteSpace(proposal.OtherDetail));
        AssertValidCombination(proposal);
    }

    [Theory]
    [InlineData("Flitro ablandador", EquipmentTypeClassification.Electricos)]   // typo del cliente
    [InlineData("Acces point Ubiquiti", EquipmentTypeClassification.Informatico)] // typo del cliente
    [InlineData("Microonda", EquipmentTypeClassification.Calor)]
    [InlineData("Molino de coffie", EquipmentTypeClassification.MaquinasRotativas)] // typo del cliente
    [InlineData("Cafetera ind", EquipmentTypeClassification.Calor)]
    [InlineData("Máquinas para helado", EquipmentTypeClassification.Electricos)]   // con acento
    public void TyposYAcentos_SigueClasificando(string name, EquipmentTypeClassification expected)
    {
        var proposal = Infer(name);
        Assert.Equal(EquipmentCategory.Equipment, proposal.Category);
        Assert.Equal(expected, proposal.TypeClassification);
        AssertValidCombination(proposal);
    }

    [Fact]
    public void Infer_EsDeterminista_MismoInputMismoResultado()
    {
        var input = new CatalogClassificationInput("CAT-1", "Cafetera ind", false, null, null);
        Assert.Equal(CatalogClassificationInference.Infer(input), CatalogClassificationInference.Infer(input));
    }

    [Theory]
    [InlineData("Extintor PQS 6 kg", false, null, null)]
    [InlineData("Mesa de trabajo inoxidable", false, null, null)]
    [InlineData("Sillas apilables", false, null, null)]
    [InlineData("Escritorio para computadora", false, null, null)]
    [InlineData("Mueble para computadora", false, null, null)]
    [InlineData("Estante porta bandejas", false, null, null)]
    [InlineData("Carrito de servicio", false, null, null)]
    [InlineData("Taburete alto", false, null, null)]
    [InlineData("Teclado inalambrico", false, null, null)]
    [InlineData("CPU Dell Optiplex", false, null, null)]
    [InlineData("Monitor Samsung 24", false, null, null)]
    [InlineData("All in one HP", false, null, null)]
    [InlineData("Router wifi dual", false, null, null)]
    [InlineData("Access point TP-Link", false, null, null)]
    [InlineData("Televisor LED 50 pulgadas", false, null, null)]
    [InlineData("Proyector Viewsonic", false, null, null)]
    [InlineData("Camara de video", false, null, null)]
    [InlineData("Balanza digital", false, null, null)]
    [InlineData("Refractómetro", false, null, null)]
    [InlineData("Horno convector", false, null, null)]
    [InlineData("Freidora doble", false, null, null)]
    [InlineData("Microondas grill", false, null, null)]
    [InlineData("Sous vide", false, null, null)]
    [InlineData("Cocina industrial 6 quemadores", false, null, null)]
    [InlineData("Termo tanque 20 L", false, null, null)]
    [InlineData("Garrafa", false, null, null)]
    [InlineData("Garrafa", false, "Equipo de calor", null)]
    [InlineData("Refrigerador combi", false, null, null)]
    [InlineData("Mesón refrigerador", false, null, null)]
    [InlineData("Cámara conservadora", false, null, null)]
    [InlineData("Cámara congeladora", false, null, null)]
    [InlineData("Abatidor de temperatura", false, null, null)]
    [InlineData("Licuadora industrial", false, null, null)]
    [InlineData("Batidora planetaria", false, null, null)]
    [InlineData("Amasadora de brazos", false, null, null)]
    [InlineData("Mixer", false, null, null)]
    [InlineData("Procesador de alimentos", false, null, null)]
    [InlineData("Moledora de cafe", false, null, null)]
    [InlineData("Molino de coffie", false, null, null)]
    [InlineData("Thermomix TM5", false, null, null)]
    [InlineData("Aparato telefónico", false, null, null)]
    [InlineData("Lavaplatos de acero inoxidable", false, null, null)]
    [InlineData("Bacha doble", false, null, null)]
    [InlineData("Extractor de humos y grasas", false, null, null)]
    [InlineData("Campana de extracción", false, null, null)]
    [InlineData("Envasadora al vacío", false, null, null)]
    [InlineData("Laminadora de masas cod. 32516", false, null, null)]
    [InlineData("Máquinas para helado", false, null, null)]
    [InlineData("Fermentador", false, null, null)]
    [InlineData("Bebedero", false, null, null)]
    [InlineData("Filtro ablandador", false, null, null)]
    [InlineData("Flitro ablandador", false, null, null)]
    [InlineData("Aparato de ventilación", false, "Ventilación", null)]
    [InlineData("Cocina rusa", false, "Aclimatización", null)]
    [InlineData("Porta utensilios", false, null, null)]
    [InlineData("Porta bandejas", true, null, null)]
    [InlineData("Pinza kelly", false, null, null)]
    [InlineData("Pinza para asar", true, null, null)]
    [InlineData("Taburete metálico", false, null, "Utensilio")]
    [InlineData("Taburete metálico", true, null, "Utensilio")]
    [InlineData("Bandeja gastronorm", false, null, "Vajilla")]
    [InlineData("Copa de vino", false, null, "Cristalería")]
    [InlineData("Cuchillo chef", false, null, "Cubiertos")]
    [InlineData("Bowl de acero", false, null, "Preparación")]
    [InlineData("Tabla de picar", false, null, "Bartending")]
    [InlineData("Molde de budín", false, null, "Repostería")]
    [InlineData("Cucharón de servicio", false, null, "Material operativo")]
    [InlineData("Bandeja de presentacion", false, null, "Equipo")]
    [InlineData("Objeto sin regla xyz", false, null, null)]
    public void Infer_NombresRealistas_GeneranCombinacionesValidas(
        string name, bool anyUnitHasSerial, string? sourceTypeHint, string? linkedArticleCategory)
    {
        var proposal = Infer(name, anyUnitHasSerial, sourceTypeHint, linkedArticleCategory);
        AssertValidCombination(proposal);
    }
}
