using System.Xml.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace VizGurka.Services
{
    public class LuceneIndexService
    {
        private readonly RAMDirectory _directory;
        private readonly StandardAnalyzer _analyzer;
        private readonly IndexWriterConfig _indexConfig;
        private readonly string _directoryPath;
        private readonly ILogger<LuceneIndexService> _logger;

        public LuceneIndexService(IConfiguration configuration, ILogger<LuceneIndexService> logger)
        {
            _directory = new RAMDirectory();
            _analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            _indexConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, _analyzer);
            _directoryPath = configuration["Path:directoryPath"];
            
            if (string.IsNullOrEmpty(_directoryPath))
            {
                throw new ArgumentNullException("directoryPath", "Directory path is not configured in appsettings.json.");
            }
        }

        // index all .gurka files in the configured directory
        public void IndexDirectory()
        {
            if (!System.IO.Directory.Exists(_directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {_directoryPath}");

            var gurkaFiles = System.IO.Directory.GetFiles(_directoryPath, "*.gurka");
            if (gurkaFiles.Length == 0)
            {
                Console.WriteLine("No .gurka files found in the directory.");
                return;
            }

            using var writer = new IndexWriter(_directory, _indexConfig);

            foreach (var gurkaFile in gurkaFiles)
            {
                Console.WriteLine($"Indexing file: {Path.GetFileName(gurkaFile)}");
                try
                {
                    XDocument xmlDoc;
                    using (var reader = new StreamReader(gurkaFile, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        xmlDoc = XDocument.Load(reader);
                    }

                    var testrun = xmlDoc.Element("Testrun");
                    if (testrun == null)
                    {
                        Console.WriteLine($"Skipping file: {Path.GetFileName(gurkaFile)} (Invalid XML structure)");
                        continue;
                    }

                    string name = testrun.Element("Name")?.Value ?? "Unknown";
                    string branchName = testrun.Element("BranchName")?.Value ?? "Unknown";
                    string commitId = testrun.Element("CommitId")?.Value ?? "Unknown";
                    string commitMessage = testrun.Element("CommitMessage")?.Value ?? "Unknown";
                    bool testsPassed = bool.TryParse(testrun.Element("TestsPassed")?.Value, out var result) && result;

                    var features = testrun
                        .Descendants("Feature")
                        .Select(f => new
                        {
                            Id = f.Element("Id")?.Value ?? "Unknown",
                            Name = f.Element("Name")?.Value ?? "Unknown",
                            Description = f.Element("Description")?.Value ?? "Unknown",
                            Status = f.Element("Status")?.Value ?? "Unknown",
                            Rules = f.Descendants("Rule") != null 
                                ? f.Descendants("Rule").Select(r => new
                                {
                                    Name = r.Element("Name")?.Value ?? "Unknown",
                                    Description = r.Element("Description")?.Value ?? "Unknown",
                                    Status = r.Element("Status")?.Value ?? "Unknown"
                                }).ToList()
                                : new List<object>().Select(r => new { Name = (string)null, Description = (string)null, Status = (string)null }).ToList(),
                            Steps = f.Descendants("Step") != null 
                                ? f.Descendants("Step").Select(s => new
                                {
                                    Kind = s.Element("Kind")?.Value ?? "Unknown",
                                    Text = s.Element("Text")?.Value ?? "Unknown",
                                    Status = s.Element("Status")?.Value ?? "Unknown"
                                }).ToList()
                                : new List<object>().Select(s => new { Kind = (string)null, Text = (string)null, Status = (string)null }).ToList()
                        }).ToList();

                    var scenarios = testrun
                        .Descendants("Scenario")
                        .Select(s => new
                        {
                            Name = s.Element("Name")?.Value ?? "Unknown",
                            Status = s.Element("Status")?.Value ?? "Unknown",
                            TestDuration = s.Element("TestDuration")?.Value ?? "Unknown",
                            Steps = s.Descendants("Step").Select(st => new
                            {
                                Kind = st.Element("Kind")?.Value ?? "Unknown",
                                Text = st.Element("Text")?.Value ?? "Unknown",
                                Status = st.Element("Status")?.Value ?? "Unknown"
                            }).ToList()
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
                        
                        foreach (var rule in feature.Rules)
                        {
                            featureDoc.Add(new TextField("RuleName", rule.Name, Field.Store.YES));
                            featureDoc.Add(new TextField("RuleDescription", rule.Description, Field.Store.YES));
                            featureDoc.Add(new StringField("RuleStatus", rule.Status, Field.Store.YES));
                        }
                        
                        foreach (var step in feature.Steps)
                        {
                            featureDoc.Add(new StringField("StepKind", step.Kind, Field.Store.YES));
                            featureDoc.Add(new TextField("StepText", step.Text, Field.Store.YES));
                            featureDoc.Add(new StringField("StepStatus", step.Status, Field.Store.YES));
                        }

                        writer.AddDocument(featureDoc);
                    }
                    
                    foreach (var scenario in scenarios)
                    {
                        var scenarioDoc = new Document
                        {
                            new StringField("FileName", Path.GetFileName(gurkaFile), Field.Store.YES),
                            new TextField("ScenarioName", scenario.Name, Field.Store.YES),
                            new StringField("ScenarioStatus", scenario.Status, Field.Store.YES),
                            new StringField("ScenarioTestDuration", scenario.TestDuration, Field.Store.YES)
                        };
                        
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
                    Console.WriteLine($"Error indexing file {Path.GetFileName(gurkaFile)}: {ex.Message}");
                }
            }

            writer.Flush(triggerMerge: false, applyAllDeletes: false);
        }

        // search algo
        public List<string> Search(string searchTerm)
        {
            var results = new List<string>();

            using var reader = DirectoryReader.Open(_directory);
            var searcher = new IndexSearcher(reader);

            var queryParser = new Lucene.Net.QueryParsers.Classic.QueryParser(LuceneVersion.LUCENE_48, "CommitMessage", _analyzer);
            var query = queryParser.Parse(searchTerm);

            var hits = searcher.Search(query, 10).ScoreDocs;

            foreach (var hit in hits)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                results.Add($"FileName: {foundDoc.Get("FileName")}, Name: {foundDoc.Get("Name")}, BranchName: {foundDoc.Get("BranchName")}, CommitId: {foundDoc.Get("CommitId")}");
            }

            return results;
        }
        
        // this is for debugging purposes, look in program.cs for RAMDirectory and you'll find it.
        public RAMDirectory GetIndexDirectory()
        {
            return _directory;
        }
    }
}