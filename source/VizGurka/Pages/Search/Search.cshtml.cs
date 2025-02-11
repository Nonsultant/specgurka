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

        public string ProductName { get; set; }
        public string Query { get; set; }
        public List<Feature> SearchResults { get; set; } = new List<Feature>();

        public void OnGet(string productName, string query)
        {
            ProductName = productName;
            Query = query;

            if (!string.IsNullOrEmpty(Query))
            {
                // Perform the search logic here
                var latestRun = TestrunReader.ReadLatestRun(productName);
                var product = latestRun?.Products.FirstOrDefault();
                if (product != null)
                {
                    SearchResults = product.Features
                        .Where(f => f.Name.Contains(Query, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
        }
    }
}