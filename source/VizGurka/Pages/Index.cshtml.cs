using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;


namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    public List<string> UniqueProductNames { get; set; }

    public void OnGet()
    {
        UniqueProductNames = TestrunReader.GetUniqueProductNames();
    }
}