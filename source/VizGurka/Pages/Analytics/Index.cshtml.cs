using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;

namespace VizGurka.Pages.Analytics;

public class Index : PageModel
{
    public List<(Guid FeatureId, Scenario Scenario)> SlowestScenarios { get; set; }

    public void OnGet()
    {
        var testRun = TestrunReader.ReadLatestRun("One");
        var product = testRun.Products.FirstOrDefault();

        SlowestScenarios = product.Features
            .SelectMany(f => f.Scenarios.Select(s => (f.Id, s)))
            .OrderByDescending(s => s.s.TestDuration)
            .Take(5)
            .ToList();
    }
}