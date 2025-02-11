using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using SpecGurka.GurkaSpec;
using System.Collections.Generic;
using System.Linq;
using VizGurka.Helpers;

namespace VizGurka.Pages.Search
{
    public class SearchModel : PageModel
    {
        private readonly IStringLocalizer<SearchModel> _localizer;
        public List<Feature> Features { get; set; } = new List<Feature>();
        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
        public string ProductName { get; set; }
        public string Query { get; set; }
        public List<Feature> SearchResults { get; set; } = new List<Feature>();
        public DateTime LatestRunDate { get; set; }

        public void OnGet(string productName, string query)
        {
            ProductName = productName;
            Query = query;
            var latestRun = TestrunReader.ReadLatestRun(productName);
            var product = latestRun?.Products.FirstOrDefault();
            if (product != null)
            {
                PopulateFeatures(product);
            }

            if (latestRun != null)
            {
                LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
            }

            if (!string.IsNullOrEmpty(Query))
            {
                if (product != null)
                {
                    SearchResults = product.Features
                        .Where(f => f.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
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
    }
}