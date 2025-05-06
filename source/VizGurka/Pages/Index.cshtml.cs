using Microsoft.AspNetCore.Mvc.RazorPages;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using SpecGurka.GurkaSpec;
using Microsoft.AspNetCore.Mvc;
using VizGurka.Services;


namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    private readonly IStringLocalizer<IndexModel> _localizer;
    private readonly GitHubActionFetcher _githubActionFetcher;
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly LuceneIndexService _luceneIndexService;

    public IndexModel(
        IStringLocalizer<IndexModel> localizer,
        GitHubActionFetcher githubActionFetcher,
        ILogger<IndexModel> logger,
        IConfiguration configuration,
        LuceneIndexService luceneIndexService)
    {
        _localizer = localizer;
        _githubActionFetcher = githubActionFetcher;
        _logger = logger;
        _configuration = configuration;
        _luceneIndexService = luceneIndexService;
        _logger.LogInformation("IndexModel initialized.");
    }

    public async Task<IActionResult> OnPostRunScript()
    {
        try
        {
            _logger.LogInformation("Running GitHub artifact fetch from user request");

            // Execute the GitHub artifact fetch using the correct method
            await _githubActionFetcher.RunAsync();

            _logger.LogInformation("GitHub artifact fetch completed successfully");
            // Reinitialize data after script completes
            TestrunReader.Initialize(_configuration);
            TempData["RefreshMessage"] = "Refresh completed successfully!";
            TempData["RefreshSuccess"] = true;

            try
            {
                _logger.LogInformation("New Lucene indexing started");
                _luceneIndexService.IndexDirectory();
                _logger.LogInformation("Lucene indexing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lucene indexing failed");
                TempData["RefreshMessage"] = "Lucene indexing failed. Check logs for details.";
                TempData["RefreshSuccess"] = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running GitHub artifact fetch");
            TempData["RefreshMessage"] = "An error occurred during refresh. Check logs for details.";
            TempData["RefreshSuccess"] = false;
        }

        // Redirect back to the same page to see updated results
        return RedirectToPage();
    }


    public List<(string ProductName, DateTime LatestRunDate, Guid Id)> UniqueProductNamesWithDatesAndId { get; set; } = new List<(string ProductName, DateTime LatestRunDate, Guid Id)>();
    public List<ProductInfo> UniqueProducts { get; set; } = new List<ProductInfo>();
    public string CurrentCulture { get; set; }

    public void OnGet()
    {
        // this is just for logging purposes
        var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture;
        CurrentCulture = requestCulture?.Culture.Name ?? "en-GB";

        Console.WriteLine($"Current Culture: {CurrentCulture}");

        var uniqueProductNames = TestrunReader.GetUniqueProductNames();
        var productInfos = new Dictionary<string, ProductInfo>();

        foreach (var productName in uniqueProductNames)
        {
            var latestRun = TestrunReader.ReadLatestRun(productName);
            if (latestRun == null) continue;

            var testRunDateTimeUtc = DateTime.Parse(
                latestRun.RunDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                );

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
                    Culture = CurrentCulture,
                    BranchName = latestRun.BranchName,
                    CommitAuthor = latestRun.CommitAuthor
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