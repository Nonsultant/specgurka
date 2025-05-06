using System.Text;
using System.Xml.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = System.IO.Directory;
using SpecGurka.GurkaSpec;

namespace VizGurka.Services;

public class LuceneIndexService
{
    private readonly StandardAnalyzer _analyzer;
    private readonly RAMDirectory _directory;
    private readonly string _directoryPath;
    private readonly IndexWriterConfig _indexConfig;
    private readonly ILogger<LuceneIndexService> _logger;

    public LuceneIndexService(IConfiguration configuration, ILogger<LuceneIndexService> logger)
    {
        _directory = new RAMDirectory();
        _analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        _indexConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, _analyzer);
        _directoryPath = configuration["Path:directoryPath"] ?? string.Empty;
        _logger = logger;

        if (string.IsNullOrEmpty(_directoryPath))
            throw new ArgumentNullException("directoryPath", "Directory path is not configured in appsettings.json.");
    }

    // index all .gurka files in the configured directory
    public void IndexDirectory()
    {
        if (!Directory.Exists(_directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {_directoryPath}");

        var gurkaFiles = Directory.GetFiles(_directoryPath, "*.gurka");
        if (gurkaFiles.Length == 0)
        {
            _logger.LogInformation("No .gurka files found in the directory.");
            return;
        }

        // Create a new IndexWriterConfig for each IndexWriter
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var indexConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

        // Use the fresh config for the IndexWriter
        using var writer = new IndexWriter(_directory, indexConfig);

        foreach (var gurkaFile in gurkaFiles)
        {
            _logger.LogInformation($"Indexing file: {Path.GetFileName(gurkaFile)}");
            try
            {
                XDocument xmlDoc;
                using (var reader = new StreamReader(gurkaFile, Encoding.UTF8, true))
                {
                    xmlDoc = XDocument.Load(reader);
                }

                var testrun = xmlDoc.Element("Testrun");
                if (testrun == null)
                {
                    _logger.LogWarning($"Skipping file: {Path.GetFileName(gurkaFile)} (Invalid XML structure)");
                    continue;
                }

                var name = testrun.Element("Name")?.Value ?? "Unknown";
                var branchName = testrun.Element("BranchName")?.Value ?? "Unknown";
                var commitId = testrun.Element("CommitId")?.Value ?? "Unknown";
                var commitMessage = testrun.Element("CommitMessage")?.Value ?? "Unknown";
                var testsPassed = bool.TryParse(testrun.Element("TestsPassed")?.Value, out var result) && result;

                // Analyze the structure to determine the proper parent-child relationship
                // Looking at the XML to identify the feature-scenario relationship
                
                var features = testrun
                    .Descendants("Feature")
                    .Select(f => new
                    {
                        Id = f.Element("Id")?.Value ?? "Unknown",
                        Name = f.Element("Name")?.Value ?? "Unknown",
                        Description = f.Element("Description")?.Value ?? "Unknown",
                        Status = f.Element("Status")?.Value ?? "Unknown",
                        Tags = GetTagsFromElement(f.Element("Tags")!),
                        Rules = f.Descendants("Rule") != null
                            ? f.Descendants("Rule").Select(r => new
                            {
                                Name = r.Element("Name")?.Value ?? "Unknown",
                                Description = r.Element("Description")?.Value ?? "Unknown",
                                Status = r.Element("Status")?.Value ?? "Unknown",
                                Tags = GetTagsFromElement(r.Element("Tags")!)
                            }).ToList()
                            : new List<object>().Select(r => new
                                { 
                                    Name = (string)null!, 
                                    Description = (string)null!, 
                                    Status = (string)null!,
                                    Tags = new List<string>()
                                }).ToList(),
                        Steps = f.Descendants("Step") != null
                            ? f.Descendants("Step").Select(s => new
                            {
                                Kind = s.Element("Kind")?.Value ?? "Unknown",
                                Text = s.Element("Text")?.Value ?? "Unknown",
                                Status = s.Element("Status")?.Value ?? "Unknown"
                            }).ToList()
                            : new List<object>().Select(s => new
                                { Kind = (string)null!, Text = (string)null!, Status = (string)null! }).ToList(),
                        NestedScenarios = f.Descendants("Scenario").Select(s => new
                        {
                            Name = s.Element("Name")?.Value ?? "Unknown",
                            Status = s.Element("Status")?.Value ?? "Unknown",
                            TestDuration = s.Element("TestDuration")?.Value ?? "Unknown",
                            Tags = GetTagsFromElement(s.Element("Tags")!),
                            Steps = s.Descendants("Step").Select(st => new
                            {
                                Kind = st.Element("Kind")?.Value ?? "Unknown",
                                Text = st.Element("Text")?.Value ?? "Unknown",
                                Status = st.Element("Status")?.Value ?? "Unknown"
                            }).ToList()
                        }).ToList()
                    }).ToList();
                
                var topLevelScenarios = testrun
                    .Elements("Scenario")
                    .Select(s => new
                    {
                        Name = s.Element("Name")?.Value ?? "Unknown",
                        Status = s.Element("Status")?.Value ?? "Unknown",
                        TestDuration = s.Element("TestDuration")?.Value ?? "Unknown",
                        Tags = GetTagsFromElement(s.Element("Tags")!),
                        Steps = s.Descendants("Step").Select(st => new
                        {
                            Kind = st.Element("Kind")?.Value ?? "Unknown",
                            Text = st.Element("Text")?.Value ?? "Unknown",
                            Status = st.Element("Status")?.Value ?? "Unknown"
                        }).ToList(),
                        FeatureId = s.Element("FeatureId")?.Value ?? s.Attribute("FeatureId")?.Value
                    }).ToList();

                // index testrun data
                var testrunDoc = new Document
                {
                    new StringField("FileName", Path.GetFileName(gurkaFile), Field.Store.YES),
                    new StringField("Name", name, Field.Store.YES),
                    new StringField("BranchName", branchName, Field.Store.YES),
                    new StringField("CommitId", commitId, Field.Store.YES),
                    new TextField("CommitMessage", commitMessage, Field.Store.YES),
                    new StringField("TestsPassed", testsPassed.ToString(), Field.Store.YES)
                };
                writer.AddDocument(testrunDoc);
                
                var featureNameToIdMap = features.ToDictionary(f => f.Name, f => f.Id);
                
                foreach (var feature in features)
                {
                    var featureDoc = new Document
                    {
                        new StringField("FileName", Path.GetFileName(gurkaFile), Field.Store.YES),
                        new StringField("FeatureId", feature.Id, Field.Store.YES),
                        new TextField("FeatureName", feature.Name, Field.Store.YES),
                        new TextField("FeatureDescription", feature.Description, Field.Store.YES),
                        new StringField("FeatureStatus", feature.Status, Field.Store.YES)
                    };
                    
                    foreach (var tag in feature.Tags)
                    {
                        featureDoc.Add(new TextField("FeatureTag", tag, Field.Store.YES));
                        featureDoc.Add(new TextField("Tag", tag, Field.Store.YES));
                    }

                    foreach (var rule in feature.Rules)
                    {
                        featureDoc.Add(new TextField("RuleName", rule.Name, Field.Store.YES));
                        featureDoc.Add(new TextField("RuleDescription", rule.Description, Field.Store.YES));
                        featureDoc.Add(new StringField("RuleStatus", rule.Status, Field.Store.YES));
                        
                        foreach (var tag in rule.Tags)
                        {
                            featureDoc.Add(new TextField("RuleTag", tag, Field.Store.YES));
                            featureDoc.Add(new TextField("Tag", tag, Field.Store.YES));
                        }
                    }

                    foreach (var step in feature.Steps)
                    {
                        featureDoc.Add(new StringField("StepKind", step.Kind, Field.Store.YES));
                        featureDoc.Add(new TextField("StepText", step.Text, Field.Store.YES));
                        featureDoc.Add(new StringField("StepStatus", step.Status, Field.Store.YES));
                    }

                    writer.AddDocument(featureDoc);
                    
                    foreach (var scenario in feature.NestedScenarios)
                    {
                        var scenarioDoc = new Document
                        {
                            new StringField("FileName", Path.GetFileName(gurkaFile), Field.Store.YES),
                            new TextField("ScenarioName", scenario.Name, Field.Store.YES),
                            new StringField("ScenarioStatus", scenario.Status, Field.Store.YES),
                            new StringField("ScenarioTestDuration", scenario.TestDuration, Field.Store.YES),
                            new StringField("ParentFeatureId", feature.Id, Field.Store.YES),
                            new StringField("ParentFeatureName", feature.Name, Field.Store.YES)
                        };
                        
                        foreach (var tag in scenario.Tags)
                        {
                            scenarioDoc.Add(new TextField("ScenarioTag", tag, Field.Store.YES));
                            scenarioDoc.Add(new TextField("Tag", tag, Field.Store.YES));
                        }

                        foreach (var step in scenario.Steps)
                        {
                            scenarioDoc.Add(new StringField("StepKind", step.Kind, Field.Store.YES));
                            scenarioDoc.Add(new TextField("StepText", step.Text, Field.Store.YES));
                            scenarioDoc.Add(new StringField("StepStatus", step.Status, Field.Store.YES));
                        }

                        writer.AddDocument(scenarioDoc);
                    }
                }
                
                foreach (var scenario in topLevelScenarios)
                {
                    string parentFeatureId = "Unknown";
                    string parentFeatureName = "Unknown";
                    
                    if (!string.IsNullOrEmpty(scenario.FeatureId))
                    {
                        parentFeatureId = scenario.FeatureId;
                        var matchingFeature = features.FirstOrDefault(f => f.Id == scenario.FeatureId);
                        if (matchingFeature != null)
                        {
                            parentFeatureName = matchingFeature.Name;
                        }
                    }
                    // If there's only one feature, assume it's the parent
                    else if (features.Count == 1)
                    {
                        parentFeatureId = features[0].Id;
                        parentFeatureName = features[0].Name;
                    }
                    
                    else
                    {
                        foreach (var feature in features)
                        {
                            if (scenario.Name.Contains(feature.Name, StringComparison.OrdinalIgnoreCase) ||
                                feature.Name.Contains(scenario.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                parentFeatureId = feature.Id;
                                parentFeatureName = feature.Name;
                                break;
                            }
                            
                            bool stepReferencesFeature = scenario.Steps.Any(step =>
                                step.Text.Contains(feature.Name, StringComparison.OrdinalIgnoreCase));
                            
                            if (stepReferencesFeature)
                            {
                                parentFeatureId = feature.Id;
                                parentFeatureName = feature.Name;
                                break;
                            }
                            
                            bool hasMatchingTags = scenario.Tags.Any(tag => 
                                feature.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
                                
                            if (hasMatchingTags)
                            {
                                parentFeatureId = feature.Id;
                                parentFeatureName = feature.Name;
                                break;
                            }
                        }
                    }

                    var scenarioDoc = new Document
                    {
                        new StringField("FileName", Path.GetFileName(gurkaFile), Field.Store.YES),
                        new TextField("ScenarioName", scenario.Name, Field.Store.YES),
                        new StringField("ScenarioStatus", scenario.Status, Field.Store.YES),
                        new StringField("ScenarioTestDuration", scenario.TestDuration, Field.Store.YES),
                        new StringField("ParentFeatureId", parentFeatureId, Field.Store.YES),
                        new StringField("ParentFeatureName", parentFeatureName, Field.Store.YES)
                    };
                    
                    foreach (var tag in scenario.Tags)
                    {
                        scenarioDoc.Add(new StringField("ScenarioTag", tag, Field.Store.YES));
                        scenarioDoc.Add(new StringField("Tag", tag, Field.Store.YES));
                    }

                    foreach (var step in scenario.Steps)
                    {
                        scenarioDoc.Add(new StringField("StepKind", step.Kind, Field.Store.YES));
                        scenarioDoc.Add(new TextField("StepText", step.Text, Field.Store.YES));
                        scenarioDoc.Add(new StringField("StepStatus", step.Status, Field.Store.YES));
                    }

                    writer.AddDocument(scenarioDoc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error indexing file {Path.GetFileName(gurkaFile)}: {ex.Message}");
            }
        }

        writer.Flush(false, false);
    }

    // search algo
    public List<string> Search(string searchTerm)
    {
        var results = new List<string>();

        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

        var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "CommitMessage", _analyzer);
        var query = queryParser.Parse(searchTerm);

        var hits = searcher.Search(query, 10).ScoreDocs;

        foreach (var hit in hits)
        {
            var foundDoc = searcher.Doc(hit.Doc);
            results.Add(
                $"FileName: {foundDoc.Get("FileName")}, Name: {foundDoc.Get("Name")}, BranchName: {foundDoc.Get("BranchName")}, CommitId: {foundDoc.Get("CommitId")}");
        }

        return results;
    }

    // Enhanced search method that includes parent-child relationships
    public List<SearchResult> SearchWithRelationships(string searchTerm, int maxResults = 10)
    {
        var results = new List<SearchResult>();

        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);
        
        var multiFieldQuery = new BooleanQuery();
        
        string[] fieldsToSearch = {
            "CommitMessage", "Name", "BranchName", 
            "FeatureName", "FeatureDescription", 
            "ScenarioName", "StepText",
            "Tag", "FeatureTag", "ScenarioTag", "RuleTag"
        };

        foreach (var field in fieldsToSearch)
        {
            var parser = new QueryParser(LuceneVersion.LUCENE_48, field, _analyzer);
            try
            {
                var fieldQuery = parser.Parse(searchTerm);
                multiFieldQuery.Add(fieldQuery, Occur.SHOULD);
            }
            catch (Exception)
            {
                // Skip invalid queries
            }
        }

        var hits = searcher.Search(multiFieldQuery, maxResults).ScoreDocs;

        foreach (var hit in hits)
        {
            var doc = searcher.Doc(hit.Doc);
            
            string docType = "Unknown";
            if (doc.Get("ScenarioName") != null) docType = "Scenario";
            else if (doc.Get("FeatureName") != null) docType = "Feature";
            else docType = "Testrun";

            var result = new SearchResult
            {
                DocType = docType,
                FileName = doc.Get("FileName"),
                Score = hit.Score
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
            
            if (docType == "Scenario")
            {
                result.Title = doc.Get("ScenarioName");
                result.ParentFeatureId = doc.Get("ParentFeatureId");
                result.ParentFeatureName = doc.Get("ParentFeatureName");
                result.Status = doc.Get("ScenarioStatus");
                result.Duration = doc.Get("ScenarioTestDuration");
                
                var scenarioTags = new List<string>();
                foreach (var tagField in doc.GetFields("ScenarioTag"))
                {
                    if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                    {
                        scenarioTags.Add(tagField.GetStringValue());
                    }
                }
                result.TypeSpecificTags = scenarioTags.Distinct().ToList();
            }
            else if (docType == "Feature")
            {
                result.Title = doc.Get("FeatureName");
                result.Id = doc.Get("FeatureId");
                result.Description = doc.Get("FeatureDescription");
                result.Status = doc.Get("FeatureStatus");
                
                var featureTags = new List<string>();
                foreach (var tagField in doc.GetFields("FeatureTag"))
                {
                    if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                    {
                        featureTags.Add(tagField.GetStringValue());
                    }
                }
                
                var ruleTags = new List<string>();
                foreach (var tagField in doc.GetFields("RuleTag"))
                {
                    if (!string.IsNullOrEmpty(tagField.GetStringValue()))
                    {
                        ruleTags.Add(tagField.GetStringValue());
                    }
                }
                
                // Combine feature and rule tags
                result.TypeSpecificTags = featureTags.Concat(ruleTags).Distinct().ToList();
            }
            else
            {
                result.Title = doc.Get("Name");
                result.BranchName = doc.Get("BranchName");
                result.CommitId = doc.Get("CommitId");
                result.CommitMessage = doc.Get("CommitMessage");
                result.TestsPassed = doc.Get("TestsPassed");
            }

            results.Add(result);
        }

        return results;
    }

    public StandardAnalyzer GetAnalyzer()
    {
        return _analyzer;
    }

    // this is for debugging purposes, look in program.cs for RAMDirectory and you'll find it.
    public RAMDirectory GetIndexDirectory()
    {
        return _directory;
    }
    
    private List<string> GetTagsFromElement(XElement tagsElement)
    {
        if (tagsElement == null)
            return new List<string>();

        var tagElements = tagsElement.Elements("Tag").Select(t => t.Value).ToList();

        if (!tagElements.Any())
        {
            tagElements = tagsElement.Elements("string").Select(t => t.Value).ToList();
        }

        if (!tagElements.Any() && !string.IsNullOrWhiteSpace(tagsElement.Value))
        {
            if (tagsElement.Value.Contains(',') || tagsElement.Value.Contains(' '))
            {
                tagElements = tagsElement.Value
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            else
            {
                tagElements.Add(tagsElement.Value);
            }
        }
        return tagElements
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToList();
    }
}