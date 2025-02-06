using Microsoft.AspNetCore.Mvc.RazorPages;
using VizGurka.Helpers;

namespace VizGurka.Pages.Product;

public class ProductModel : PageModel
{
    public string ProductName { get; set; }
    public DateTime LatestRunDate { get; set; }

    public void OnGet(string productName)
    {
        ProductName = productName;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        if (latestRun != null)
        {
            LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
        }
    }
}