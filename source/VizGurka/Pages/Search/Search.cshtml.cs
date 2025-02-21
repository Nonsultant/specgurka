using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace VizGurka.Pages.Search
{
    public class SearchModel : PageModel
    {
        private readonly IStringLocalizer<SearchModel> _localizer;
        public SearchModel(IStringLocalizer<SearchModel> localizer)
        {
            _localizer = localizer;
        }

        public List<Feature> Features { get; set; } = new List<Feature>();
        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
        public Guid Id { get; set; } = Guid.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public List<Feature> FeatureSearchResults { get; set; } = new List<Feature>();
        public List<ScenarioWithFeatureId> ScenarioSearchResults { get; set; } = new List<ScenarioWithFeatureId>();
        public List<RuleWithFeatureId> RuleSearchResults { get; set; } = new List<RuleWithFeatureId>();
        public List<TagWithFeatureId> TagSearchResults { get; set; } = new List<TagWithFeatureId>();
        public DateTime LatestRunDate { get; set; } = DateTime.MinValue;
        public Guid FirstFeatureId { get; set; } = Guid.Empty;

        public int FeatureResultCount { get; set; } = 0;
        public int RuleResultCount { get; set; } = 0;
        public int ScenarioResultCount { get; set; } = 0;
        public int TagsResultCount { get; set; } = 0;

        public class RuleWithFeatureId
        {
            public Guid FeatureId { get; set; } = Guid.Empty;
            public Rule? Rule { get; set; }
            public string Description { get; set; } = string.Empty;
        }
        public class ScenarioWithFeatureId
        {
            public Guid FeatureId { get; set; } = Guid.Empty;
            public Scenario? Scenario { get; set; }
            public List<Step> Steps { get; set; } = new List<Step>();
        }
        public class TagWithFeatureId
        {
            public Guid FeatureId { get; set; } = Guid.Empty;
            public string Tag { get; set; } = string.Empty;
            public string FeatureName { get; set; } = string.Empty;
            public string? ScenarioName { get; set; } = string.Empty;
        }

        public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public void OnGet(string productName, string query)
        {
            ProductName = productName;
            Query = query;
            var latestRun = TestrunReader.ReadLatestRun(productName);
            var product = latestRun?.Products.FirstOrDefault();
            if (product != null)
            {
                PopulateFeatures(product);
                PopulateScenarios();
                if (Features.Any())
                {
                    FirstFeatureId = Features.First().Id;
                }
            }

            if (latestRun != null)
            {

                LatestRunDate = DateTime.Parse(latestRun.RunDate);

            }

            if (!string.IsNullOrEmpty(Query) && product != null)
            {
                // Include features that match the query by name, have scenarios that match the query, or have rules that match the query
                FeatureSearch(product);

                // Include scenarios that match the query
                ScenarioSearch(product);

                // Include rules that match the query
                RuleSearch(product);

                // Include tags that match the query
                TagSearch(product);

                SearchResultCounter(product);
            }
        }

        public IHtmlContent MarkdownStringToHtml(string input)
        {
            var trimmedInput = input.Trim();
            return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
        }

        private void PopulateFeatures(SpecGurka.GurkaSpec.Product product)
        {
            Features = product.Features.Select(f => new Feature
            {
                Id = f.Id,
                Name = f.Name,
                Status = f.Status,
                Scenarios = f.Scenarios,
                Rules = f.Rules,
                Description = f.Description
            }).ToList();
        }

        private void PopulateScenarios()
        {
            Scenarios = Features.SelectMany(f => f.Scenarios.Concat(f.Rules.SelectMany(r => r.Scenarios))).ToList();
        }

        private void FeatureSearch(SpecGurka.GurkaSpec.Product product)
        {
            FeatureSearchResults = product.Features
                        .Where(f => f.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                    f.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                                    f.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                         s.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase))) ||
                                    f.Rules.Any(r => r.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                     r.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                                                     r.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                                          s.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)))))
                        .ToList();
        }

        private void ScenarioSearch(SpecGurka.GurkaSpec.Product product)
        {
            ScenarioSearchResults = product.Features
                        .SelectMany(f => f.Scenarios
                            .Where(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                        s.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)))
                            .Select(s => new ScenarioWithFeatureId
                            {
                                FeatureId = f.Id,
                                Scenario = s,
                                Steps = s.Steps
                            })
                            .Concat(f.Rules.SelectMany(r => r.Scenarios
                                .Where(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                            s.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)))
                                .Select(s => new ScenarioWithFeatureId
                                {
                                    FeatureId = f.Id,
                                    Scenario = s,
                                    Steps = s.Steps
                                }))))
                        .ToList();
        }

        private void RuleSearch(SpecGurka.GurkaSpec.Product product)
        {
            RuleSearchResults = product.Features
                       .SelectMany(f => f.Rules
                           .Where(r => r.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                       r.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)))
                           .Select(r => new RuleWithFeatureId
                           {
                               FeatureId = f.Id,
                               Rule = r,
                               Description = r.Description ?? string.Empty
                           }))
                       .ToList();
        }

        private void TagSearch(SpecGurka.GurkaSpec.Product product)
        {
            TagSearchResults = product.Features
                .SelectMany(f => f.Tags
                    .Where(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase))
                    .Select(t => new TagWithFeatureId
                    {
                        FeatureId = f.Id,
                        Tag = t,
                        FeatureName = f.Name
                    })
                    .Concat(f.Scenarios.SelectMany(s => s.Tags
                        .Where(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase))
                        .Select(t => new TagWithFeatureId
                        {
                            FeatureId = f.Id,
                            Tag = t,
                            FeatureName = f.Name,
                            ScenarioName = s.Name
                        })))
                    .Concat(f.Rules.SelectMany(r => r.Tags
                        .Where(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase))
                        .Select(t => new TagWithFeatureId
                        {
                            FeatureId = f.Id,
                            Tag = t,
                            FeatureName = f.Name
                        }))
                        .Concat(f.Rules.SelectMany(r => r.Scenarios.SelectMany(s => s.Tags
                            .Where(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase))
                            .Select(t => new TagWithFeatureId
                            {
                                FeatureId = f.Id,
                                Tag = t,
                                FeatureName = f.Name,
                                ScenarioName = s.Name
                            })))
                        ))
                )
                .ToList();
        }

        private void SearchResultCounter(SpecGurka.GurkaSpec.Product product)
        {
            FeatureResultCount = product.Features
                        .Count(f => f.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                    f.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                                    f.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                         s.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase))) ||
                                    f.Rules.Any(r => r.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                     r.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                                                     r.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                                          s.Tags.Any(t => t.Contains(Query, StringComparison.OrdinalIgnoreCase)))));

            ScenarioResultCount = ScenarioSearchResults.Count;
            RuleResultCount = RuleSearchResults.Count;
            TagsResultCount = TagSearchResults.Count;
        }
    }
}