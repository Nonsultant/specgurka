using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;

namespace VizGurka.Pages.Product;

public class ProductModel : PageModel
{
    public List<Feature> Features { get; set; } = new List<Feature>();
    public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    public string ProductName { get; set; } = string.Empty;
    public DateTime LatestRunDate { get; set; }

    public void OnGet(string productName, Guid id)
    {
        ProductName = productName;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        var product = latestRun?.Products.FirstOrDefault();
        if (product != null)
        {
            PopulateFeatures(product);
            PopulateScenarios();
        }

        if (latestRun != null)
        {
            LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
        }
    }

    private void PopulateFeatures(SpecGurka.GurkaSpec.Product product)
    {
        Features = product.Features;
    }

    private void PopulateScenarios()
    {
        Scenarios = Features
            .SelectMany(f => f.Scenarios.Concat(f.Rules.SelectMany(r => r.Scenarios)))
            .ToList();
    }

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        var trimmedInput = input.Trim();
        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }
}