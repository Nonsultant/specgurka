using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    public IndexModel() { }

    public void OnGet()
    {
        ViewData["Title"] = "Gurka";
    }
}