using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace VizGurka.Pages.Features;

public class FeaturesModel : PageModel
{
    private readonly IStringLocalizer<FeaturesModel> _localizer;
	private readonly FeatureFileRepositorySettings _featureFileRepoSettings;
    private readonly TagPatternsSettings _tagPatternsSettings;
    private readonly ProductNameHelper _productNameHelper;

   public FeaturesModel(
    IStringLocalizer<FeaturesModel> localizer,
    IOptions<FeatureFileRepositorySettings> featureFileRepoOptions,
    IOptions<TagPatternsSettings> tagPatternsOptions,
    ProductNameHelper productNameHelper)
{
    _localizer = localizer;
    _featureFileRepoSettings = featureFileRepoOptions.Value;
    _tagPatternsSettings = tagPatternsOptions.Value;
    _productNameHelper = productNameHelper;
}
    public Guid Id { get; set; }
    public List<Feature> Features { get; set; } = new List<Feature>();
    public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public List<Guid> FeatureIds { get; set; } = new List<Guid>();
    public Feature? SelectedFeature { get; set; }
    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    public string GithubLink { get; set; } = string.Empty;
    public string CommitId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime LatestRunDateUtc { get; set; } = DateTime.MinValue;
    public string CurrentCulture { get; set; } = string.Empty;

    public int FeaturePassedCount { get; private set; }
    public int FeatureFailedCount { get; private set; }
    public int FeatureNotImplementedCount { get; private set; }
    
    public int ScenarioPassedCount { get; private set; }
    public int ScenarioFailedCount { get; private set; }
    public int ScenarioNotImplementedCount { get; private set; }

	public string BaseUrl => _featureFileRepoSettings.BaseUrl; 

	public string GithubBaseUrl => _tagPatternsSettings.Github.BaseUrl;
    public string GithubOwner => _tagPatternsSettings.Github.Owner;
	public List<RepositorySettings> GithubRepositories => _tagPatternsSettings.Github.Repositories;
    public string GetPrettyProductName(string productName) => _productNameHelper.GetPrettyProductName(productName);

	public string GetGithubRepositoryByProductName(string productName)
	{
    	var repository = GithubRepositories
        	.FirstOrDefault(repo => repo.Product.Contains(productName));
        return repository?.Name;
    }
    

	public string AzureBaseUrl => _tagPatternsSettings.Azure.BaseUrl;
    public string AzureOwner => _tagPatternsSettings.Azure.Owner;
	public List<RepositorySettings> AzureRepositories => _tagPatternsSettings.Azure.Repositories;

	public string GetAzureRepositoryByProductName(string productName)
	{
    	var repository = AzureRepositories
        	.FirstOrDefault(repo => repo.Product.Contains(productName));
        return repository?.Name;
    }

    public int RulePassedCount { get; private set; }
    public int RuleFailedCount { get; private set; }
    public int RuleNotImplementedCount { get; private set; }

    public Dictionary<string, object> FeatureTree { get; set; } = new();

    public List<(Feature Feature, TimeSpan Duration)> SlowestFeatures { get; private set; } = new();
    public List<(Scenario Scenario, string FeatureName, TimeSpan Duration)> SlowestScenarios { get; private set; } = new();
    public List<(Scenario Scenario, string SectionName, TimeSpan Duration)> SlowestScenariosInSelectedFeature { get; private set; } = new();

    public void OnGet(string productName, Guid id, Guid? featureId)
    {
        var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture;
        CurrentCulture = requestCulture?.Culture.Name ?? "en-GB";

        ProductName = productName;
        Id = id;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        var product = latestRun?.Products.FirstOrDefault();
        if (product != null)
        {
            PopulateFeatures(product);
            PopulateFeatureIds();
            BuildFeatureTree();

            CountFeaturesByStatus();

            CalculateSlowestFeatures();
            CalculateSlowestScenarios();
            CalculateSlowestScenariosForSelectedFeature();

            Id = product.Features.FirstOrDefault()?.Id ?? Guid.Empty;

            if (featureId.HasValue)
            {
                SelectedFeature = Features.FirstOrDefault(f => f.Id == featureId.Value);
                if (SelectedFeature != null)
                {
                    PopulateScenarios(SelectedFeature);
                    CalculateSlowestScenariosForSelectedFeature();
                    CountRulesByStatus();
                    CountScenariosByStatus();
                }
            }
        }

        if (latestRun != null)
        {
            LatestRunDateUtc = DateTime.Parse(latestRun.RunDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        if (featureId.HasValue)
        {
            SelectedFeature = Features.FirstOrDefault(f => f.Id == featureId.Value);
            if (SelectedFeature != null)
            {
                PopulateScenarios(SelectedFeature);
            }
        }

        if (latestRun != null)
        {
            GithubLink = latestRun.BaseUrl;
        }

        if (latestRun != null)
        {
            CommitId = latestRun.CommitId;
        }
    }

    public string GetFormattedLatestRunDate()
    {
        return DateTimeHelper.FormatDateTimeForCulture(LatestRunDateUtc, CurrentCulture);
    }

    private void CountFeaturesByStatus()
    {
        FeaturePassedCount = Features.Count(f => f.Status.ToString() == "Passed");
        FeatureFailedCount = Features.Count(f => f.Status.ToString() == "Failed");
        FeatureNotImplementedCount = Features.Count(f => f.Status.ToString() == "NotImplemented");
    }

    private void CountRulesByStatus()
    {
        if (SelectedFeature == null)
        {
            RulePassedCount = 0;
            RuleFailedCount = 0;
            RuleNotImplementedCount = 0;
            return;
        }

        RulePassedCount = SelectedFeature.Rules.Count(r => r.Status.ToString() == "Passed");
        RuleFailedCount = SelectedFeature.Rules.Count(r => r.Status.ToString() == "Failed");
        RuleNotImplementedCount = SelectedFeature.Rules.Count(r => r.Status.ToString() == "NotImplemented");
    }
    
    private void CountScenariosByStatus()
    {
        if (SelectedFeature == null)
        {
            ScenarioPassedCount = 0;
            ScenarioFailedCount = 0;
            ScenarioNotImplementedCount = 0;
            return;
        }
        
        var allScenarios = SelectedFeature.Scenarios.ToList();
        
        foreach (var rule in SelectedFeature.Rules)
        {
            allScenarios.AddRange(rule.Scenarios);
        }

        ScenarioPassedCount = allScenarios.Count(s => s.Status.ToString() == "Passed");
        ScenarioFailedCount = allScenarios.Count(s => s.Status.ToString() == "Failed");
        ScenarioNotImplementedCount = allScenarios.Count(s => s.Status.ToString() == "NotImplemented");
    }
    
    public int GetTotalScenarioCount(List<Feature> features)
    {
        return features.Sum(feature =>
            (feature.Scenarios?.Count ?? 0) +
            (feature.Rules?.Sum(rule => rule.Scenarios?.Count ?? 0) ?? 0)
        );
    }

    private void PopulateFeatures(Product product)
    {
        Features = product.Features.Select(f => new Feature
        {
            Id = f.Id,
            Name = f.Name,
            Tags = f.Tags,
            Status = f.Status,
            Background = f.Background,
            Scenarios = f.Scenarios,
            Rules = f.Rules,
            Description = f.Description,
            FilePath = f.FilePath
        }).ToList();
    }

    private void PopulateScenarios(Feature selectedFeature)
    {
        Scenarios = selectedFeature.Scenarios.Concat(selectedFeature.Rules.SelectMany(r => r.Scenarios)).ToList();
    }

    private void PopulateFeatureIds()
    {
        FeatureIds = Features.Select(f => f.Id).ToList();
    }

    private string[] NormalizeAndSplitFilePath(string filePath)
    {

        var normalizedPath = filePath.Replace("\\", "/");

        // Remove the relative path trash (../../)
        while (normalizedPath.StartsWith("../"))
        {
            normalizedPath = normalizedPath.Substring(3);
        }

        var parts = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        return parts;
    }

    private void BuildFeatureTree()
    {
        foreach (var feature in Features)
        {
            var parts = NormalizeAndSplitFilePath(feature.FilePath);
            if (parts.Length == 0) continue;
            
            int featuresIndex = Array.FindIndex(parts, p => p.Equals("features", StringComparison.OrdinalIgnoreCase));
            if (featuresIndex == -1 || featuresIndex == parts.Length - 1)
                continue;
            
            var currentLevel = FeatureTree;
            for (int i = featuresIndex + 1; i < parts.Length - 1; i++)
            {
                var part = parts[i];

                if (!currentLevel.ContainsKey(part))
                {
                    currentLevel[part] = new Dictionary<string, object>();
                }

                currentLevel = (Dictionary<string, object>)currentLevel[part];
            }
            
            if (!currentLevel.ContainsKey("Features"))
            {
                currentLevel["Features"] = new List<Feature>();
            }

            ((List<Feature>)currentLevel["Features"]).Add(feature);
        }
    }



    private void CalculateSlowestFeatures()
    {
        SlowestFeatures = Features
            .Where(f => !string.IsNullOrEmpty(f.TestDuration))
            .Select(f => (
                Feature: f,
                Duration: ParseDuration(f.TestDuration)
            ))
            .OrderByDescending(item => item.Duration)
            .Take(5)
            .ToList();
    }

    private void CalculateSlowestScenarios()
    {
        var allScenarios = new List<(Scenario Scenario, string FeatureName, TimeSpan Duration)>();

        foreach (var feature in Features)
        {
            allScenarios.AddRange(feature.Scenarios
                .Where(s => !string.IsNullOrEmpty(s.TestDuration))
                .Select(s => (Scenario: s, FeatureName: feature.Name, Duration: ParseDuration(s.TestDuration))));

            foreach (var rule in feature.Rules)
            {
                allScenarios.AddRange(rule.Scenarios
                    .Where(s => !string.IsNullOrEmpty(s.TestDuration))
                    .Select(s => (Scenario: s, FeatureName: $"{feature.Name} ({rule.Name})", Duration: ParseDuration(s.TestDuration))));
            }
        }

        SlowestScenarios = allScenarios
            .OrderByDescending(item => item.Duration)
            .Take(5)
            .ToList();
    }

    private TimeSpan ParseDuration(string duration)
    {
        // Handle the format "hh:mm:ss.fffffff"
        if (string.IsNullOrWhiteSpace(duration))
            return TimeSpan.Zero;

        if (TimeSpan.TryParse(duration, out TimeSpan result))
        {
            return result;
        }

        // Fallback to the previous parsing logic for other formats
        result = TimeSpan.Zero;

        // Handle minutes if present
        if (duration.Contains("m"))
        {
            var parts = duration.Split('m');
            if (double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double minutes))
            {
                result = result.Add(TimeSpan.FromMinutes(minutes));
            }
            duration = parts.Length > 1 ? parts[1].Trim() : "";
        }

        // Handle seconds
        if (duration.Contains("s"))
        {
            duration = duration.Replace("s", "").Trim();
            if (double.TryParse(duration, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds))
            {
                result = result.Add(TimeSpan.FromSeconds(seconds));
            }
        }

        return result;
    }
    private void CalculateSlowestScenariosForSelectedFeature()
    {
        if (SelectedFeature == null)
        {
            SlowestScenariosInSelectedFeature = new List<(Scenario, string, TimeSpan)>();
            return;
        }

        var scenariosInFeature = new List<(Scenario Scenario, string SectionName, TimeSpan Duration)>();

        // Add scenarios directly under the feature
        scenariosInFeature.AddRange(SelectedFeature.Scenarios
            .Where(s => !string.IsNullOrEmpty(s.TestDuration))
            .Select(s => (Scenario: s, SectionName: "Feature", Duration: ParseDuration(s.TestDuration))));

        // Add scenarios under rules
        foreach (var rule in SelectedFeature.Rules)
        {
            scenariosInFeature.AddRange(rule.Scenarios
                .Where(s => !string.IsNullOrEmpty(s.TestDuration))
                .Select(s => (Scenario: s, SectionName: rule.Name, Duration: ParseDuration(s.TestDuration))));
        }

        SlowestScenariosInSelectedFeature = scenariosInFeature
            .OrderByDescending(item => item.Duration)
            .Take(10) // Show 10 slowest scenarios
            .ToList();
    }

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return HtmlString.Empty;
        }

        var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimStart();
        }
        
        var trimmedInput = string.Join(Environment.NewLine, lines);

        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }

    public Scenario FindMatchingChild(List<Scenario> children, string[] headerCells, string[] rowCells)
    {
        var paramValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Math.Min(headerCells?.Length ?? 0, rowCells.Length); i++)
        {
            paramValues[headerCells?[i] ?? throw new InvalidOperationException()] = rowCells[i];
        }

        foreach (var child in children)
        {
            if (child.Examples != null)
            {
                bool allParamsMatch = true;
                foreach (var param in paramValues)
                {
                    var paramPattern = $"{param.Key}={param.Value}";
                    if (!child.Examples.Contains(paramPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        allParamsMatch = false;
                        break;
                    }
                }

                if (allParamsMatch && paramValues.Count > 0)
                {
                    return child;
                }
            }
        }

        foreach (var child in children)
        {
            bool allParamsInSteps = true;
            foreach (var param in paramValues)
            {
                bool paramFoundInSteps = false;
                foreach (var step in child.Steps)
                {
                    if (step.Text.Contains(param.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        paramFoundInSteps = true;
                        break;
                    }
                }

                if (!paramFoundInSteps)
                {
                    allParamsInSteps = false;
                    break;
                }
            }

            if (allParamsInSteps && paramValues.Count > 0)
            {
                return child;
            }
        }

        foreach (var child in children)
        {
            int matchCount = 0;
            foreach (var cell in rowCells)
            {
                foreach (var step in child.Steps)
                {
                    if (step.Text.Contains(cell, StringComparison.OrdinalIgnoreCase))
                    {
                        matchCount++;
                        break;
                    }
                }
            }

            if (matchCount >= rowCells.Length / 2)
            {
                return child;
            }
        }

        int rowIndex = 0;
        foreach (var child in children)
        {
            if (rowIndex == Array.IndexOf(children.ToArray(), child))
            {
                return child;
            }
            rowIndex++;
        }

        return children.FirstOrDefault() ?? throw new InvalidOperationException();
    }
}