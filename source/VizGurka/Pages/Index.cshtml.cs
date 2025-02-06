using Microsoft.AspNetCore.Mvc.RazorPages;
using VizGurka.Helpers;


namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    public List<(string ProductName, DateTime LatestRunDate, Guid Id)> UniqueProductNamesWithDatesAndId { get; set; } = new List<(string ProductName, DateTime LatestRunDate, Guid Id)>();

    public void OnGet()
    {
        var uniqueProductNames = TestrunReader.GetUniqueProductNames();

        foreach (var productName in uniqueProductNames)
        {
            var latestRun = TestrunReader.ReadLatestRun(productName);
            if (latestRun == null) continue;

            var testRunDateTime = DateTime.Parse(latestRun.DateAndTime);
            var product = latestRun.Products.FirstOrDefault(p => p.Name == productName);
            if (product == null) continue;

            var feature = product.Features.FirstOrDefault();
            if (feature == null) continue;

            UniqueProductNamesWithDatesAndId.Add((productName, testRunDateTime, feature.Id));
        }

        UniqueProductNamesWithDatesAndId = UniqueProductNamesWithDatesAndId
            .OrderByDescending(item => item.LatestRunDate)
            .ToList();
    }
}