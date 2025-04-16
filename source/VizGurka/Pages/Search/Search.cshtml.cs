using System.Net;
using System.Text.RegularExpressions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using VizGurka.Services;

namespace VizGurka.Pages.Search;

public class SearchModel : PageModel
{
    private readonly Dictionary<string, string> _fieldPrefixMappings;
    private readonly IStringLocalizer<SearchModel> _localizer;
    private readonly ILogger<SearchModel> _logger;
    private readonly LuceneIndexService _luceneIndexService;

    public SearchModel(IStringLocalizer<SearchModel> localizer, LuceneIndexService luceneIndexService,
        ILogger<SearchModel> logger)
    {
        _localizer = localizer;
        _luceneIndexService = luceneIndexService;
        _logger = logger;

        // Initialize the mapping dictionary for field prefixes
        _fieldPrefixMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "feature:", "FeatureName:" },
            { "egenskap:", "FeatureName:" },
            { "feature name:", "FeatureName:" },
            { "feature id:", "FeatureId:" },
            { "feature description:", "FeatureDescription:" },
            { "feature status:", "FeatureStatus:" },

            { "scenario:", "ScenarioName:" },
            { "test:", "ScenarioName:" },
            { "scenario name:", "ScenarioName:" },
            { "scenario status:", "ScenarioStatus:" },
            { "scenario duration:", "ScenarioTestDuration:" },

            { "step:", "StepText:" },
            { "steg:", "StepText:" },

            { "tag:", "Tag:" },
            { "tags:", "Tag:" },
            { "@", "Tag:" },
            { "featuretag:", "FeatureTag:" },
            { "feature tag:", "FeatureTag:" },
            { "scenariotag:", "ScenarioTag:" },
            { "scenario tag:", "ScenarioTag:" },
            { "rule:", "RuleName:" },
            { "rule description:", "RuleDescription:" },
            { "rule status:", "RuleStatus:" },
            { "ruletag:", "RuleTag:" },
            { "rule tag:", "RuleTag:" },

            { "file:", "FileName:" },
            { "fil:", "FileName:" },
            { "name:", "Name:" },
            { "namn:", "Name:" },
            { "branch:", "BranchName:" },
            { "commit:", "CommitId:" },
            { "message:", "CommitMessage:" },

            { "parent:", "ParentFeatureId:" },
            { "parent feature:", "ParentFeatureId:" },
            { "parent name:", "ParentFeatureName:" }
        };
    }

    public string ProductName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public List<SearchResult> SearchResults { get; set; } = new();
    public Guid FirstFeatureId { get; set; } = Guid.Empty;

    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public string Filter { get; set; } = string.Empty;


    public int TotalFeaturesCount { get; set; }
    public int TotalScenariosCount { get; set; }
    public int TotalStepsCount { get; set; }
    public int TotalTagsCount { get; set; }
    public int TotalRulesCount { get; set; }
    public int TotalOtherCount { get; set; }

    public int FilteredFeaturesCount { get; set; }
    public int FilteredScenariosCount { get; set; }
    public int FilteredStepsCount { get; set; }
    public int FilteredTagsCount { get; set; }
    public int FilteredRulesCount { get; set; }
    public int FilteredOtherCount { get; set; }
    
    public List<SearchResult> AllResults { get; set; } = new();
    public List<SearchResult> FilteredResults { get; set; } = new();

    public void OnGet(string productName, string query, string filter)
    {
        ProductName = productName;
        Query = WebUtility.UrlDecode(query);
        Filter = filter;

        if (!string.IsNullOrEmpty(Query)) PerformLuceneSearch();

        UpdateFilteredCounts();
    }

    private void UpdateFilteredCounts()
    {
        // Apply the filter to the SearchResults
        var filteredResults = ApplyFilter(SearchResults, Filter).ToList();

        // Calculate Filtered counts based on the filtered results
        FilteredFeaturesCount = filteredResults.Count(r => r.DocumentType == "Feature");
        FilteredScenariosCount = filteredResults.Count(r => r.DocumentType == "Scenario");
        FilteredStepsCount = filteredResults.Count(r => r.DocumentType == "Step");
        FilteredTagsCount = filteredResults.Count(r => r.DocumentType == "Tag");
        FilteredRulesCount = filteredResults.Count(r => r.DocumentType == "Rule");
        FilteredOtherCount = filteredResults.Count(r => 
            r.DocumentType != "Feature" && 
            r.DocumentType != "Scenario" && 
            r.DocumentType != "Step" && 
            r.DocumentType != "Tag" && 
            r.DocumentType != "Rule");

        // Update the SearchResults to only include filtered results
        SearchResults = filteredResults;
    }

    private IEnumerable<SearchResult> ApplyFilter(IEnumerable<SearchResult> results, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return results; // No filter applied, return all results

        return filter.ToLower() switch
        {
            "features" => results.Where(r => r.DocumentType == "Feature"),
            "scenarios" => results.Where(r => r.DocumentType == "Scenario"),
            "tags" => results.Where(r => r.DocumentType == "Tag"),
            "rules" => results.Where(r => r.DocumentType == "Rule"),
            _ => results // Unknown filter, return all results
        };
    }

    private void PerformLuceneSearch()
    {
        try
        {
            var processedQuery = ApplyFieldPrefixMappings(Query);

            // Handle tag queries starting with @
            if (Query.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                processedQuery = "Tag:" + Query.Substring(1);
                processedQuery = ApplyFieldPrefixMappings(processedQuery);
            }

            _logger.LogInformation($"Original query: {Query}");
            _logger.LogInformation($"Processed query: {processedQuery}");

            var directory = _luceneIndexService.GetIndexDirectory();
            using var reader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(reader);
            var analyzer = _luceneIndexService.GetAnalyzer();

            // Define fields and boosts
            string[] fields =
            {
                "FeatureName", "ScenarioName", "StepText", "Tag",
                "FeatureDescription", "RuleName", "FileName", "CommitMessage"
            };

            var boosts = new Dictionary<string, float>
            {
                { "FeatureName", 3.0f },
                { "ScenarioName", 2.5f },
                { "Tag", 2.5f },
                { "StepText", 2.0f }
            };

            Query mainQuery;

            if (!HasExplicitFieldPrefix(processedQuery))
            {
                // Enhanced multi-field search with query expansion
                var parser = new MultiFieldQueryParser(
                    LuceneVersion.LUCENE_48,
                    fields,
                    analyzer,
                    boosts
                )
                {
                    DefaultOperator = Operator.OR,
                    AllowLeadingWildcard = true
                };

                // Add wildcard and fuzzy matches
                var enhancedQuery = processedQuery.Split()
                    .Select(term => $"{term}* {term}~")
                    .Aggregate((a, b) => $"{a} {b}");

                try
                {
                    mainQuery = parser.Parse(enhancedQuery);
                }
                catch (ParseException)
                {
                    // Fallback to simple match
                    mainQuery = parser.Parse(QueryParser.Escape(processedQuery));
                }
            }
            else
            {
                // Existing field-specific handling
                var parts = processedQuery.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var fieldName = parts[0].Trim();
                    var searchValue = parts[1].Trim();
                    var queryParser = new QueryParser(LuceneVersion.LUCENE_48, fieldName, analyzer);
                    mainQuery = queryParser.Parse(searchValue);
                }
                else
                {
                    var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "FeatureName", analyzer);
                    mainQuery = queryParser.Parse(processedQuery);
                }
            }

            // Apply filters
            if (!string.IsNullOrEmpty(Filter))
            {
                var filterField = GetFilterField(Filter);
                if (!string.IsNullOrEmpty(filterField))
                {
                    var filter = new FieldValueFilter(filterField);
                    mainQuery = new FilteredQuery(mainQuery, filter);
                }
            }

            // Search with increased hit count
            ExecuteSearch(searcher, mainQuery);
            
            AllResults = SearchResults;

            CalculateTotalCounts();
            
            UpdateFilteredCounts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search: {Message}", ex.Message);
            SearchResults = new List<SearchResult>();
        }
    }
    
    private void CalculateTotalCounts()
    {
        TotalFeaturesCount = AllResults.Count(r => r.DocumentType == "Feature");
        TotalScenariosCount = AllResults.Count(r => r.DocumentType == "Scenario");
        TotalStepsCount = AllResults.Count(r => r.DocumentType == "Step");
        TotalTagsCount = AllResults.Count(r => r.DocumentType == "Tag");
        TotalRulesCount = AllResults.Count(r => r.DocumentType == "Rule");
        TotalOtherCount = AllResults.Count(r => 
            r.DocumentType != "Feature" && 
            r.DocumentType != "Scenario" && 
            r.DocumentType != "Step" && 
            r.DocumentType != "Tag" && 
            r.DocumentType != "Rule");
    }


    private string GetFilterField(string filter)
    {
        return filter?.ToLower() switch
        {
            "features" => "FeatureName",
            "scenarios" => "ScenarioName",
            "tags" => "Tag",
            "rules" => "RuleName",
            _ => null
        };
    }

    private void ExecuteSearch(IndexSearcher searcher, Query query, int maxResults = 50)
    {
        var hits = searcher.Search(query, maxResults).ScoreDocs;

        SearchResults = hits.Select(hit =>
        {
            var doc = searcher.Doc(hit.Doc);

            var docType = DetermineDocumentType(doc);

            var sourceField = DetermineMatchingField(doc, Query);

            var result = new SearchResult
            {
                Title = doc.Get("FeatureName") ?? doc.Get("ScenarioName") ?? doc.Get("Name") ?? "No Title",
                Content = doc.Get("FeatureDescription") ??
                          doc.Get("StepText") ?? doc.Get("CommitMessage") ?? "No Content",
                Score = hit.Score,
                SourceField = sourceField,
                DocumentType = docType,
                FileName = doc.Get("FileName") ?? "No File"
            };

            var tags = new List<string>();
            foreach (var tagField in doc.GetFields("Tag"))
                if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                    tags.Add(tagField.GetStringValue());

            result.Tags = tags.Distinct().ToList();

            if (docType == "Feature")
            {
                result.FeatureId = doc.Get("FeatureId") ?? string.Empty;

                var featureTags = new List<string>();
                foreach (var tagField in doc.GetFields("FeatureTag"))
                    if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                        featureTags.Add(tagField.GetStringValue());

                result.TypeSpecificTags = featureTags;
            }
            else if (docType == "Scenario")
            {
                result.ParentFeatureId = doc.Get("ParentFeatureId") ?? string.Empty;
                result.ParentFeatureName = doc.Get("ParentFeatureName") ?? string.Empty;

                if (!string.IsNullOrEmpty(result.ParentFeatureId) &&
                    Guid.TryParse(result.ParentFeatureId, out var featureId))
                    if (FirstFeatureId == Guid.Empty)
                        FirstFeatureId = featureId;

                var scenarioTags = new List<string>();
                foreach (var tagField in doc.GetFields("ScenarioTag"))
                    if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                        scenarioTags.Add(tagField.GetStringValue());

                result.TypeSpecificTags = scenarioTags;
            }

            if (docType == "Feature" || docType == "Scenario")
                result.Status = doc.Get(docType + "Status") ?? string.Empty;

            if (docType == "Scenario") result.Duration = doc.Get("ScenarioTestDuration") ?? string.Empty;

            return result;
        }).ToList();

        if (FirstFeatureId == Guid.Empty && SearchResults.Any())
        {
            var firstWithFeatureId = SearchResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.FeatureId));
            if (firstWithFeatureId != null && Guid.TryParse(firstWithFeatureId.FeatureId, out var featureId))
            {
                FirstFeatureId = featureId;
            }
            else
            {
                var firstWithParentId = SearchResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.ParentFeatureId));
                if (firstWithParentId != null && Guid.TryParse(firstWithParentId.ParentFeatureId, out var parentId))
                    FirstFeatureId = parentId;
            }
        }
    }

    private string ApplyFieldPrefixMappings(string query)
    {
        if (string.IsNullOrEmpty(query))
            return query;

        foreach (var mapping in _fieldPrefixMappings)
            if (query.StartsWith(mapping.Key, StringComparison.OrdinalIgnoreCase))
                return mapping.Value + query.Substring(mapping.Key.Length);

        return query;
    }

    private bool HasExplicitFieldPrefix(string query)
    {
        if (string.IsNullOrEmpty(query))
            return false;

        var match = Regex.Match(query, @"^\s*([A-Za-z]+[A-Za-z0-9]*):\s*");
        return match.Success;
    }

    private string DetermineDocumentType(Document doc)
    {
        if (doc.Get("ScenarioName") != null) return "Scenario";

        if (doc.Get("FeatureName") != null) return "Feature";

        if (doc.Get("CommitMessage") != null) return "Testrun";
        return "Unknown";
    }

    private string DetermineMatchingField(Document doc, string query)
    {
        if (query.StartsWith("@", StringComparison.OrdinalIgnoreCase))
        {
            var tagValue = query.Substring(1);

            foreach (var field in new[] { "Tag", "FeatureTag", "ScenarioTag", "RuleTag" })
            foreach (var tagField in doc.GetFields(field))
            {
                var storedTagValue = tagField.GetStringValue();
                if (!string.IsNullOrEmpty(storedTagValue) &&
                    (storedTagValue.Equals(tagValue, StringComparison.OrdinalIgnoreCase) ||
                     storedTagValue.Equals("@" + tagValue, StringComparison.OrdinalIgnoreCase)))
                    return field;
            }
        }

        var match = Regex.Match(query, @"^\s*([A-Za-z]+[A-Za-z0-9]*):\s*(.+)$");
        if (match.Success)
        {
            var fieldPrefix = match.Groups[1].Value.Trim();

            if (fieldPrefix.Equals("tag", StringComparison.OrdinalIgnoreCase) ||
                fieldPrefix.Equals("tags", StringComparison.OrdinalIgnoreCase))
                return "Tag";

            foreach (var mapping in _fieldPrefixMappings)
                if (mapping.Key.StartsWith(fieldPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var fieldName = mapping.Value.TrimEnd(':');

                    if (doc.Get(fieldName) != null) return fieldName;
                }
        }

        string[] fieldsToCheck =
        {
            "Tag", "ScenarioTag", "FeatureTag", "RuleTag",
            "ScenarioName", "FeatureName", "FeatureDescription", "StepText",
            "CommitMessage", "Name", "FileName", "BranchName", "CommitId", "RuleName", "RuleDescription", "RuleStatus"
        };

        var searchTerm = query;
        if (match.Success) searchTerm = match.Groups[2].Value.Trim();


        foreach (var field in fieldsToCheck)
            if (field.EndsWith("Tag"))
            {
                foreach (var tagField in doc.GetFields(field))
                {
                    var tagValue = tagField.GetStringValue();
                    if (!string.IsNullOrEmpty(tagValue) &&
                        tagValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        return field;
                }
            }
            else
            {
                var fieldValue = doc.Get(field);
                if (!string.IsNullOrEmpty(fieldValue) &&
                    fieldValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    return field;
            }

        var queryWords = searchTerm.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);

        if (queryWords.Length > 0)
        {
            var fieldMatches = new Dictionary<string, int>();

            foreach (var field in fieldsToCheck)
                if (field.EndsWith("Tag"))
                {
                    var matchCount = 0;
                    foreach (var tagField in doc.GetFields(field))
                    {
                        var tagValue = tagField.GetStringValue();
                        if (!string.IsNullOrEmpty(tagValue))
                            foreach (var word in queryWords)
                                if (word.Length > 2 &&
                                    tagValue.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                                    matchCount++;
                    }

                    if (matchCount > 0) fieldMatches[field] = matchCount;
                }
                else
                {
                    var fieldValue = doc.Get(field);
                    if (!string.IsNullOrEmpty(fieldValue))
                    {
                        var matchCount = 0;
                        foreach (var word in queryWords)
                            if (word.Length > 2 &&
                                fieldValue.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                                matchCount++;

                        if (matchCount > 0) fieldMatches[field] = matchCount;
                    }
                }

            if (fieldMatches.Count > 0) return fieldMatches.OrderByDescending(kv => kv.Value).First().Key;
        }

        var docType = DetermineDocumentType(doc);
        switch (docType)
        {
            case "Feature":
                return "FeatureName";
            case "Scenario":
                return "ScenarioName";
            case "Testrun":
                return "CommitMessage";
            default:
                return "Unknown";
        }
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

        public List<string> Tags { get; set; } = new();
        public List<string> TypeSpecificTags { get; set; } = new();

        public string FeatureId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string ParentFeatureId { get; set; } = string.Empty;
        public string ParentFeatureName { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string GetRelevantFeatureId()
        {
            if (DocumentType == "Feature")
                return FeatureId;
            if (DocumentType == "Scenario")
                return ParentFeatureId;
            return string.Empty;
        }
    }
}