using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VizGurka.Pages.Features;

public class Index : PageModel
{
    public Guid Id { get; set; }

    public void OnGet(Guid id)
    {
        Id = id;
    }
}