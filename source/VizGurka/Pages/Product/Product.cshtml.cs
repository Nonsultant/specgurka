using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
namespace VizGurka.Pages.Product;

public class ProductModel : PageModel
{
    public Feature Feature { get; set; }
    public MarkdownPipeline Pipeline { get; set; }
    public string ProductName { get; set; }
    public DateTime LatestRunDate { get; set; }

    public void OnGet(string productName, Guid id)
    {
        ProductName = productName;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        var product = latestRun.Products.FirstOrDefault();
        Feature = product.Features.FirstOrDefault(f => f.Id == id);

        if (latestRun != null)
        {
            LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
        }

        Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }
    public IHtmlContent MarkdownStringToHtml(string input)
    {
        var trimmedInput = input.Trim();

        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }
}