using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using VizGurka.Helpers;
using VizGurka.Models;

namespace VizGurka.Services;

public class SearchService
{
    private readonly LuceneIndexService _luceneIndexService;
    private readonly QueryMapperHelper _queryMapperHelper;
    private readonly ILogger<SearchService> _logger;

    public SearchService(LuceneIndexService luceneIndexService, QueryMapperHelper queryMapperHelper, ILogger<SearchService> logger)
    {
        _luceneIndexService = luceneIndexService;
        _queryMapperHelper = queryMapperHelper;
        _logger = logger;
    }

    public List<SearchResult> Search(string query, string filter, Dictionary<string, string> fieldPrefixMappings)
    {
        try
        {
            var processedQuery = _queryMapperHelper.MapQuery(query);

            // Handle tag queries starting with @
            if (query.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                processedQuery = "Tag:" + query.Substring(1);
            }

            _logger.LogInformation($"Original query: {query}");
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
                var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, fields, analyzer, boosts)
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
                // Field-specific handling
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

            // Execute the search and apply filters
            var searchResults = ExecuteSearch(searcher, mainQuery, processedQuery);
            return ApplyFilter(searchResults, filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search: {Message}", ex.Message);
            return new List<SearchResult>();
        }
    }

    private List<SearchResult> ExecuteSearch(
        IndexSearcher searcher, 
        Query query, 
        string originalQuery, 
        int maxResults = 50)
{
    var hits = searcher.Search(query, maxResults).ScoreDocs;

    return hits.Select(hit =>
    {
        var doc = searcher.Doc(hit.Doc);

        var docType = DetermineDocumentType(doc);

        var sourceField = DetermineMatchingField(doc, originalQuery);

        var result = new SearchResult
        {
            Title = doc.Get("FeatureName") ?? doc.Get("ScenarioName") ?? doc.Get("Name") ?? "No Title",
            Content = doc.Get("FeatureDescription") ??
                      doc.Get("StepText") ?? doc.Get("CommitMessage") ?? "No Content",
            Score = hit.Score,
            SourceField = sourceField, // Set the SourceField here
            DocumentType = docType,
            FileName = doc.Get("FileName") ?? "No File"
        };

        var tags = new List<string>();
        foreach (var tagField in doc.GetFields("Tag"))
        {
            if (!string.IsNullOrEmpty(tagField.GetStringValue()))
            {
                tags.Add(tagField.GetStringValue());
            }
        }

        result.Tags = tags.Distinct().ToList();

        if (docType == "Feature")
        {
            result.FeatureId = doc.Get("FeatureId") ?? string.Empty;

            var featureTags = new List<string>();
            foreach (var tagField in doc.GetFields("FeatureTag"))
            {
                if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                {
                    featureTags.Add(tagField.GetStringValue());
                }
            }

            result.TypeSpecificTags = featureTags;
        }
        else if (docType == "Scenario")
        {
            result.ParentFeatureId = doc.Get("ParentFeatureId") ?? string.Empty;
            result.ParentFeatureName = doc.Get("ParentFeatureName") ?? string.Empty;

            var scenarioTags = new List<string>();
            foreach (var tagField in doc.GetFields("ScenarioTag"))
            {
                if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                {
                    scenarioTags.Add(tagField.GetStringValue());
                }
            }

            result.TypeSpecificTags = scenarioTags;
        }

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
}

    private List<SearchResult> ApplyFilter(IEnumerable<SearchResult> results, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return results.ToList();

        return filter.ToLower() switch
        {
            "features" => results.Where(r => r.DocumentType == "Feature").ToList(),
            "scenarios" => results.Where(r => r.DocumentType == "Scenario").ToList(),
            "tags" => results.Where(r => r.DocumentType == "Tag").ToList(),
            "rules" => results.Where(r => r.DocumentType == "Rule").ToList(),
            _ => results.ToList()
        };
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

    private string DetermineMatchingField(Document doc, string originalQuery)
    {
        // Handle @-based tag queries
    if (originalQuery.StartsWith("@", StringComparison.OrdinalIgnoreCase))
    {
        var tagValue = originalQuery.Substring(1);

        foreach (var field in new[] { "Tag", "FeatureTag", "ScenarioTag", "RuleTag" })
        {
            foreach (var tagField in doc.GetFields(field))
            {
                var storedTagValue = tagField.GetStringValue();
                if (!string.IsNullOrEmpty(storedTagValue) &&
                    (storedTagValue.Equals(tagValue, StringComparison.OrdinalIgnoreCase) ||
                     storedTagValue.Equals("@" + tagValue, StringComparison.OrdinalIgnoreCase)))
                {
                    return field;
                }
            }
        }
    }

    // extract field prefix from the query
    var match = Regex.Match(originalQuery, @"^\s*([A-Za-z]+[A-Za-z0-9]*):\s*(.+)$");
    if (match.Success)
    {
        var fieldPrefix = match.Groups[1].Value.Trim();
        
        if (fieldPrefix.Equals("tag", StringComparison.OrdinalIgnoreCase) ||
            fieldPrefix.Equals("tags", StringComparison.OrdinalIgnoreCase))
        {
            return "Tag";
        }
        
        var _fieldPrefixMappings = _queryMapperHelper.GetMappings();
        
        foreach (var mapping in _fieldPrefixMappings)
        {
            if (mapping.Key.StartsWith(fieldPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fieldName = mapping.Value.TrimEnd(':');

                if (doc.Get(fieldName) != null)
                {
                    return fieldName;
                }
            }
        }
    }
    
    string[] fieldsToCheck =
    {
        "Tag", "ScenarioTag", "FeatureTag", "RuleTag",
        "ScenarioName", "FeatureName", "FeatureDescription", "StepText",
        "CommitMessage", "Name", "FileName", "BranchName", "CommitId", "RuleName", "RuleDescription", "RuleStatus"
    };

    var searchTerm = originalQuery;
    if (match.Success)
    {
        searchTerm = match.Groups[2].Value.Trim();
    }

    foreach (var field in fieldsToCheck)
    {
        if (field.EndsWith("Tag"))
        {
            foreach (var tagField in doc.GetFields(field))
            {
                var tagValue = tagField.GetStringValue();
                if (!string.IsNullOrEmpty(tagValue) &&
                    tagValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return field;
                }
            }
        }
        else
        {
            var fieldValue = doc.Get(field);
            if (!string.IsNullOrEmpty(fieldValue) &&
                fieldValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return field;
            }
        }
    }
    
    var docType = DetermineDocumentType(doc);
    return docType switch
    {
        "Feature" => "FeatureName",
        "Scenario" => "ScenarioName",
        "Testrun" => "CommitMessage",
        _ => "Unknown"
    };
    }
}