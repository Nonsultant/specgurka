using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;

namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    private readonly string? _gurkaFilePath;
    public Testrun TestRun { get; set; }

    public IndexModel(IConfiguration config)
    {
        _gurkaFilePath = config.GetValue<string>("GurkaFilePath");
    }

    public void OnGet()
    {
        ViewData["Title"] = "Gurka";
        TestRun = Gurka.ReadGurkaFile(_gurkaFilePath);
    }
}