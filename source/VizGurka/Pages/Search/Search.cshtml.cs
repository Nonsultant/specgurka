using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using VizGurka.Services;
using VizGurka.Helpers;
using VizGurka.Models;

namespace VizGurka.Pages.Search
{
    public class SearchModel : PageModel
    {
        private readonly IStringLocalizer<SearchModel> _localizer;
        private readonly ILogger<SearchModel> _logger;
        private readonly SearchService _searchService;
        private readonly QueryMapperHelper _queryMapper;
        private readonly MarkdownHelper _markdownHelper;

        public SearchModel(
            IStringLocalizer<SearchModel> localizer,
            SearchService searchService,
            QueryMapperHelper queryMapper,
            MarkdownHelper markdownHelper,
            ILogger<SearchModel> logger)
        {
            _localizer = localizer;
            _searchService = searchService;
            _queryMapper = queryMapper;
            _markdownHelper = markdownHelper;
            _logger = logger;

            // Initialize default values for properties
            SearchResults = new List<SearchResult>();
        }

        // Properties required for Razor view
        public string ProductName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public Guid FirstFeatureId { get; set; } = Guid.Empty;
        public List<SearchResult> SearchResults { get; set; }

        public void OnGet(string productName, string query, string filter)
        {
            ProductName = productName;
            Query = query;
            Filter = filter;

            if (!string.IsNullOrEmpty(Query))
            {
                var mappedQuery = _queryMapper.MapQuery(Query);
                SearchResults = _searchService.Search(mappedQuery, Filter, _queryMapper.GetMappings());
            }
        }

        public IHtmlContent RenderMarkdown(string input) => _markdownHelper.ConvertToHtml(input);
    }
}