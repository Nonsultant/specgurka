using Microsoft.AspNetCore.Mvc.RazorPages;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    private readonly IStringLocalizer<IndexModel> _localizer;
    public IndexModel(IStringLocalizer<IndexModel> localizer)
    {
        _localizer = localizer;
    }
    public List<ProductInfo> UniqueProducts { get; set; } = new List<ProductInfo>();
    public string CurrentCulture { get; set; }

    public void OnGet(string culture = "sv-SE")
    {
        CurrentCulture = culture;
        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        Console.WriteLine($"Current Culture: {culture}");

        var uniqueProductNames = TestrunReader.GetUniqueProductNames();
        var productInfos = new Dictionary<string, ProductInfo>();

        foreach (var productName in uniqueProductNames)
        {
            var latestRun = TestrunReader.ReadLatestRun(productName);
            if (latestRun == null) continue;

            var testRunDateTimeUtc = DateTime.Parse(latestRun.RunDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var product = latestRun.Products.FirstOrDefault(p => p.Name == productName);
            if (product == null) continue;

            var feature = product.Features.FirstOrDefault();
            if (feature == null) continue;

            if (!productInfos.ContainsKey(productName))
            {
                productInfos[productName] = new ProductInfo
                {
                    ProductName = productName,
                    LatestRunDateUtc = testRunDateTimeUtc,
                    Id = feature.Id,
                    Culture = culture
                };
            }
            else if (testRunDateTimeUtc > productInfos[productName].LatestRunDateUtc)
            {
                productInfos[productName].LatestRunDateUtc = testRunDateTimeUtc;
                productInfos[productName].Id = feature.Id;
            }
        }

        UniqueProducts = productInfos.Values.OrderByDescending(p => p.LatestRunDateUtc).ToList();
    }
}

public class ProductInfo
{
    public string ProductName { get; set; }
    public DateTime LatestRunDateUtc { get; set; }
    public Guid Id { get; set; }
    public string Culture { get; set; }

    public string GetFormattedDateTime()
    {
        return DateTimeHelper.FormatDateTimeForCulture(LatestRunDateUtc, Culture);
    }
}
