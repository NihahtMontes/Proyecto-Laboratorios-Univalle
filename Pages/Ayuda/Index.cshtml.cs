using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages.Ayuda;

/// <summary>Ayuda para cualquier rol: cómo empezar, glosario con buscador y preguntas frecuentes (Helpers/HelpContent.cs).</summary>
[Authorize]
public class IndexModel : PageModel
{
    public IReadOnlyList<HelpContent.RoleGuide> RoleGuides => HelpContent.RoleGuides;
    public IReadOnlyList<HelpContent.Faq> Faqs => HelpContent.Faqs;

    /// <summary>Glosario agrupado por área, en el orden en que aparece en el menú.</summary>
    public IReadOnlyList<IGrouping<string, HelpContent.Term>> GlossaryByArea =>
        HelpContent.Glossary.GroupBy(t => t.Area).ToList();

    public void OnGet()
    {
    }
}
