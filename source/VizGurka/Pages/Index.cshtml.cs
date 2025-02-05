using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;


namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    public List<(string ProductName, DateTime LatestRunDate)> UniqueProductNamesWithDates { get; set; }

    public void OnGet()
    {
        var uniqueProductNames = TestrunReader.GetUniqueProductNames();
        UniqueProductNamesWithDates = new List<(string ProductName, DateTime LatestRunDate)>();

        foreach (var productName in uniqueProductNames)
        {
            var latestRun = TestrunReader.ReadLatestRun(productName);
            if (latestRun != null)
            {
                var testRunDateTime = DateTime.Parse(latestRun.DateAndTime);
                UniqueProductNamesWithDates.Add((productName, testRunDateTime));
            }
        }
    }
}