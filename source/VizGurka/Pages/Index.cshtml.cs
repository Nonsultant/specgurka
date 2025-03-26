using Microsoft.AspNetCore.Mvc.RazorPages;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using SpecGurka.GurkaSpec;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using VizGurka.Services;

namespace VizGurka.Pages;

public class IndexModel : PageModel
{
    private readonly IStringLocalizer<IndexModel> _localizer;
    private readonly PowerShellService _powerShellService;
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;

    public IndexModel(
        IStringLocalizer<IndexModel> localizer,
        PowerShellService powerShellService,
        ILogger<IndexModel> logger,
        IConfiguration configuration)
    {
        _localizer = localizer;
        _powerShellService = powerShellService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnPostRunScript()
    {
        try
        {
            _logger.LogInformation("Running GitHub artifact fetch from user request");

            // Execute PowerShell script
            var result = await _powerShellService.RunScriptAsync();

            if (result.Success)
            {
                _logger.LogInformation("GitHub artifact fetch completed successfully");
                // Reinitialize data after script completes
                TestrunReader.Initialize(_configuration);
                TempData["RefreshMessage"] = "Refresh completed successfully!";
                TempData["RefreshSuccess"] = true;
            }
            else
            {
                _logger.LogWarning("GitHub artifact fetch completed with issues: {Error}", result.Error);
                TempData["RefreshMessage"] = "Refresh completed with issues. Check logs for details.";
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
        var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture;
        CurrentCulture = requestCulture.Culture.Name;

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