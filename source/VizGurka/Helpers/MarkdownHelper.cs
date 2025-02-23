using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Microsoft.AspNetCore.Html;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.ListExtras;


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
            .DisableHtml()
            .UseListExtras()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
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
            
            // Fix list formatting, To comply with CommonMark
            cleaned = Regex.Replace(cleaned,
                @"^[ \t]*([*\-+])\s+",
                m => $"{m.Groups[1].Value} ",
                RegexOptions.Multiline);

            // Ensure blank line before lists
            cleaned = Regex.Replace(cleaned,
                @"(\S.*?)(\r?\n)([*\-+])",
                "$1$2\n$3",
                RegexOptions.Multiline);

            // Clean up table formatting
            cleaned = Regex.Replace(cleaned,
                @"^\s*(\|.*\|)\s*$",
                m => m.Groups[1].Value,
                RegexOptions.Multiline);

            return string.Join("\n", cleaned.Split('\n')
                .Select(line =>
                {
                    var trimmed = line.TrimStart();
                    // Handle images by trimming entire line
                    if (trimmed.StartsWith("!["))
                        return trimmed;

                    // Check if current line is part of a list or table without altering its original spacing
                    bool isList = Regex.IsMatch(line, @"^\s*([*\-+]|\d+\.)\s+");
                    bool isTableRow = Regex.IsMatch(line, @"^\s*\|");

                    return (isList || isTableRow) ? line : line.TrimStart();
                }));
        }

    }

}
