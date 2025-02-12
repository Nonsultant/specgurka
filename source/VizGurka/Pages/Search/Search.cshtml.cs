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
        public string home_button { get; set; } = string.Empty;
        public string enter_search { get; set; } = string.Empty;
        public string search_result { get; set; } = string.Empty;
        public string scenario_message { get; set; } = string.Empty;
        public string rule_message { get; set; } = string.Empty;
        public List<Feature> Features { get; set; } = new List<Feature>();
        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
        public Guid Id { get; set; } = Guid.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public List<Feature> FeatureSearchResults { get; set; } = new List<Feature>();
        public List<ScenarioWithFeatureId> ScenarioSearchResults { get; set; } = new List<ScenarioWithFeatureId>();
        public List<RuleWithFeatureId> RuleSearchResults { get; set; } = new List<RuleWithFeatureId>();
        public DateTime LatestRunDate { get; set; } = DateTime.MinValue;
        public Guid FirstFeatureId { get; set; } = Guid.Empty;
        public int FeatureResultCount { get; set; } = 0;
        public int RuleResultCount { get; set; } = 0;
        public int ScenarioResultCount { get; set; } = 0;

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

        public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public void OnGet(string productName, string query)
        {
            home_button = _localizer["home_button"];
            enter_search = _localizer["enter_search"];
            search_result = _localizer["search_result"];
            scenario_message = _localizer["scenario_message"];
            rule_message = _localizer["rule_message"];

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
                LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
            }

            if (!string.IsNullOrEmpty(Query))
            {
                // The logic for searching for features and scenarios is inside this if statement
                if (product != null)
                {
                    // Count features that match the query by name, have scenarios that match the query, or have rules that match the query
                    FeatureResultCount = product.Features
                        .Count(f => f.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                    f.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                                    f.Rules.Any(r => r.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                     r.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))));

                    // Include features that match the query by name, have scenarios that match the query, or have rules that match the query
                    FeatureSearchResults = product.Features
                        .Where(f => f.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                    f.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                                    f.Rules.Any(r => r.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                                                     r.Scenarios.Any(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))))
                        .ToList();

                    // Include scenarios that match the query
                    ScenarioSearchResults = product.Features
                        .SelectMany(f => f.Scenarios
                            .Where(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))
                            .Select(s => new ScenarioWithFeatureId
                            {
                                FeatureId = f.Id,
                                Scenario = s,
                                Steps = s.Steps
                            })
                            .Concat(f.Rules.SelectMany(r => r.Scenarios
                                .Where(s => s.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))
                                .Select(s => new ScenarioWithFeatureId
                                {
                                    FeatureId = f.Id,
                                    Scenario = s,
                                    Steps = s.Steps
                                }))))
                        .ToList();

                    // Include rules that match the query
                    RuleSearchResults = product.Features
                        .SelectMany(f => f.Rules
                            .Where(r => r.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))
                            .Select(r => new RuleWithFeatureId
                            {
                                FeatureId = f.Id,
                                Rule = r,
                                Description = r.Description ?? string.Empty
                            }))
                        .ToList();

                    ScenarioResultCount = ScenarioSearchResults.Count;
                    RuleResultCount = RuleSearchResults.Count;
                }
            }
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

        public IHtmlContent MarkdownStringToHtml(string input)
        {
            var trimmedInput = input.Trim();
            return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
        }
    }
}