using System.Net;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using VizGurka.Services;

namespace VizGurka.Pages.Search;

public class SearchModel : PageModel
{
    private readonly IStringLocalizer<SearchModel> _localizer;
    private readonly LuceneIndexService _luceneIndexService;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(IStringLocalizer<SearchModel> localizer, LuceneIndexService luceneIndexService, ILogger<SearchModel> logger)
    {
        _localizer = localizer;
        _luceneIndexService = luceneIndexService;
        _logger = logger;
    }

    public string ProductName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public List<SearchResult> SearchResults { get; set; } = new();
    public Guid FirstFeatureId { get; set; } = Guid.Empty;

    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public void OnGet(string productName, string query)
    {
        ProductName = productName;
        Query = WebUtility.UrlDecode(query);

        if (!string.IsNullOrEmpty(Query)) PerformLuceneSearch();
    }

    private void PerformLuceneSearch()
    {
        try
        {
            var directory = _luceneIndexService.GetIndexDirectory();
            using var reader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(reader);

            var analyzer = _luceneIndexService.GetAnalyzer();

            // Create a query for multiple fields
            var queries = new Dictionary<string, Query>
            {
                { "FileName", new QueryParser(LuceneVersion.LUCENE_48, "FileName", analyzer).Parse(Query) },
                { "Name", new QueryParser(LuceneVersion.LUCENE_48, "Name", analyzer).Parse(Query) },
                { "BranchName", new QueryParser(LuceneVersion.LUCENE_48, "BranchName", analyzer).Parse(Query) },
                { "CommitId", new QueryParser(LuceneVersion.LUCENE_48, "CommitId", analyzer).Parse(Query) },
                { "CommitMessage", new QueryParser(LuceneVersion.LUCENE_48, "CommitMessage", analyzer).Parse(Query) },
                { "FeatureName", new QueryParser(LuceneVersion.LUCENE_48, "FeatureName", analyzer).Parse(Query) },
                { "FeatureDescription", new QueryParser(LuceneVersion.LUCENE_48, "FeatureDescription", analyzer).Parse(Query) },
                { "FeatureStatus", new QueryParser(LuceneVersion.LUCENE_48, "FeatureStatus", analyzer).Parse(Query) },
                { "FeatureId", new QueryParser(LuceneVersion.LUCENE_48, "FeatureId", analyzer).Parse(Query) },
                { "ScenarioName", new QueryParser(LuceneVersion.LUCENE_48, "ScenarioName", analyzer).Parse(Query) },
                { "ScenarioStatus", new QueryParser(LuceneVersion.LUCENE_48, "ScenarioStatus", analyzer).Parse(Query) },
                { "ScenarioTestDuration", new QueryParser(LuceneVersion.LUCENE_48, "ScenarioTestDuration", analyzer).Parse(Query) },
                { "ParentFeatureId", new QueryParser(LuceneVersion.LUCENE_48, "ParentFeatureId", analyzer).Parse(Query) },
                { "ParentFeatureName", new QueryParser(LuceneVersion.LUCENE_48, "ParentFeatureName", analyzer).Parse(Query) },
                { "StepText", new QueryParser(LuceneVersion.LUCENE_48, "StepText", analyzer).Parse(Query) }
            };

            // Combine all field queries into a BooleanQuery
            var booleanQuery = new BooleanQuery();
            foreach (var query in queries.Values) booleanQuery.Add(query, Occur.SHOULD);

            // Execute the search
            var hits = searcher.Search(booleanQuery, 10).ScoreDocs;

            // Process search results
            SearchResults = hits.Select(hit =>
            {
                var doc = searcher.Doc(hit.Doc);

                // Get explanation to find the source field with the highest contribution
                var explanation = searcher.Explain(booleanQuery, hit.Doc);
                var sourceField = GetMatchingField(explanation, queries.Keys);

                // Determine the document type
                string docType = DetermineDocumentType(doc);
                
                // Create the search result with improved parent-child relationship handling
                var result = new SearchResult
                {
                    Title = doc.Get("FeatureName") ?? doc.Get("ScenarioName") ?? doc.Get("Name") ?? "No Title",
                    Content = doc.Get("FeatureDescription") ?? doc.Get("StepText") ?? doc.Get("CommitMessage") ?? "No Content",
                    Score = hit.Score,
                    SourceField = sourceField,
                    DocumentType = docType
                };

                // Handle feature ID retrieval 
                if (docType == "Feature")
                {
                    result.FeatureId = doc.Get("FeatureId") ?? string.Empty;
                }
                else if (docType == "Scenario")
                {
                    // For scenarios, get the parent feature ID
                    result.ParentFeatureId = doc.Get("ParentFeatureId") ?? string.Empty;
                    result.ParentFeatureName = doc.Get("ParentFeatureName") ?? string.Empty;
                    
                    // Try to parse feature ID to Guid if it's available and valid
                    if (!string.IsNullOrEmpty(result.ParentFeatureId) && 
                        Guid.TryParse(result.ParentFeatureId, out Guid featureId))
                    {
                        // Set FirstFeatureId if it's still empty
                        if (FirstFeatureId == Guid.Empty)
                        {
                            FirstFeatureId = featureId;
                        }
                    }
                }

                // Add additional fields based on document type
                if (docType == "Feature" || docType == "Scenario")
                {
                    result.Status = doc.Get(docType + "Status") ?? string.Empty;
                }
                
                if (docType == "Scenario")
                {
                    result.Duration = doc.Get("ScenarioTestDuration") ?? string.Empty;
                }

                return result;
            }).ToList();

            // If we have no feature ID yet but have results, try to find one
            if (FirstFeatureId == Guid.Empty && SearchResults.Any())
            {
                // Look for a result with a feature ID or parent feature ID
                var firstWithFeatureId = SearchResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.FeatureId));
                if (firstWithFeatureId != null && Guid.TryParse(firstWithFeatureId.FeatureId, out Guid featureId))
                {
                    FirstFeatureId = featureId;
                }
                else
                {
                    var firstWithParentId = SearchResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.ParentFeatureId));
                    if (firstWithParentId != null && Guid.TryParse(firstWithParentId.ParentFeatureId, out Guid parentId))
                    {
                        FirstFeatureId = parentId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search: {Message}", ex.Message);
        }
    }

    private string DetermineDocumentType(Lucene.Net.Documents.Document doc)
    {
        // Determine the type of document based on available fields
        if (doc.Get("ScenarioName") != null)
        {
            return "Scenario";
        }
        else if (doc.Get("FeatureName") != null)
        {
            return "Feature";
        }
        else if (doc.Get("CommitMessage") != null)
        {
            return "Testrun";
        }
        return "Unknown";
    }

    private string GetMatchingField(Explanation explanation, IEnumerable<string> fields)
    {
        // Inspect explanation details and determine which field contributed to the match
        foreach (var field in fields)
        {
            if (explanation.Description.Contains(field, StringComparison.OrdinalIgnoreCase)) return field;

            // Check nested details if the field isn't directly in the top-level explanation
            foreach (var detail in explanation.GetDetails())
                if (detail.Description.Contains(field, StringComparison.OrdinalIgnoreCase))
                    return field;
        }

        return "Unknown"; // Default if no field matches
    }

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new HtmlString(string.Empty);
            
        var trimmedInput = input.Trim();
        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }

    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float Score { get; set; }
        public string SourceField { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        
        // Feature-specific properties
        public string FeatureId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        
        // Scenario-specific properties
        public string ParentFeatureId { get; set; } = string.Empty;
        public string ParentFeatureName { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        
        // Helper property to get the relevant feature ID regardless of document type
        public string GetRelevantFeatureId()
        {
            if (DocumentType == "Feature")
                return FeatureId;
            else if (DocumentType == "Scenario")
                return ParentFeatureId;
            return string.Empty;
        }
    }
}