using Markdig;
using Microsoft.AspNetCore.Html;

namespace VizGurka.Helpers;

public class MarkdownHelper
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownHelper()
    {
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }

    public IHtmlContent ConvertToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return HtmlString.Empty;
        }

        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimStart();
        }

        var trimmedInput = string.Join(Environment.NewLine, lines);

        return new HtmlString(Markdown.ToHtml(trimmedInput, _pipeline));
    }
}