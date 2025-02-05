using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;

namespace VizGurka.Pages.Features;

public class Index : PageModel
{
    public Feature Feature { get; set; }

    public MarkdownPipeline Pipeline { get; set; }

    public void OnGet(Guid id)
    {
        var testRun = TestrunReader.ReadLatestRun("One");
        var product = testRun.Products.FirstOrDefault();
        Feature = product.Features.FirstOrDefault(f => f.Id == id);

        Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        var trimmedInput = input.Trim();

        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }

}