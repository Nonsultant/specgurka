using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;

namespace VizGurka.Pages.Features;

public class Index : PageModel
{
    public Feature Feature { get; set; }

    public void OnGet(Guid id)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GurkaFiles/DemoProject_2024-11-28T13_21_37.gurka");

        var file = Gurka.ReadGurkaFile(filePath).Products.FirstOrDefault();

        Feature = file.Features.FirstOrDefault(f => f.Id == id);
    }
}