using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Microsoft.AspNetCore.Html;

namespace VizGurka.Helpers
{
    public static class MarkdownHelper
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables(new PipeTableOptions
            {
                RequireHeaderSeparator = false,
                UseHeaderForColumnCount = true
            })
            .UseAutoIdentifiers()
            .UseMediaLinks()
            .UseListExtras()
            .UseAdvancedExtensions()
            .Build();

        public static IHtmlContent ConvertToHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return HtmlString.Empty;

            var adjusted = NormalizeMarkdown(input);
            return new HtmlString(Markdig.Markdown.ToHtml(adjusted, Pipeline));
        }

        private static string NormalizeMarkdown(string input)
        {
            var cleaned = input.Replace("~/", "/");

            //lists
            cleaned = Regex.Replace(cleaned,
                @"^(\s*)([-*+])(\s*)",
                m => $"{m.Groups[1].Value}{m.Groups[2].Value} ",
                RegexOptions.Multiline);

            //links
            cleaned = Regex.Replace(cleaned,
                @"(\S)\s*($$[^$$]+$$$$[^)]+$$)\s*",
                "$1 $2",
                RegexOptions.Multiline);

            //tables
            cleaned = Regex.Replace(cleaned,
                @"^\s*(\|.*\|)\s*$",
                m => m.Groups[1].Value,
                RegexOptions.Multiline);

            //Remove ALL leading/trailing whitespace from images
            return string.Join("\n", cleaned.Split('\n')
                .Select(line =>
                {
                    var trimmed = line.TrimStart();
                    return trimmed.StartsWith("![")
                        ? trimmed
                        : line;
                }));
        }

    }

}
