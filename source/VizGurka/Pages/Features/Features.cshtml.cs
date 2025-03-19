using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace VizGurka.Pages.Features;

public class FeaturesModel : PageModel
{
    private readonly IStringLocalizer<FeaturesModel> _localizer;
    public FeaturesModel(IStringLocalizer<FeaturesModel> localizer)
    {
        _localizer = localizer;
    }
    public Guid Id { get; set; }
    public List<Feature> Features { get; set; } = new List<Feature>();
    public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public List<Guid> FeatureIds { get; set; } = new List<Guid>();
    public Feature? SelectedFeature { get; set; } = null;
    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    public string GithubLink { get; set; } = string.Empty;
    public string CommitId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime LatestRunDateUtc { get; set; } = DateTime.MinValue;
    public string CurrentCulture { get; set; }

    public int FeaturePassedCount { get; private set; } = 0;
    public int FeatureFailedCount { get; private set; } = 0;
    public int FeatureNotImplementedCount { get; private set; } = 0;

    public int RulePassedCount { get; private set; } = 0;
    public int RuleFailedCount { get; private set; } = 0;
    public int RuleNotImplementedCount { get; private set; } = 0;

    public Dictionary<string, object> FeatureTree { get; set; } = new();

    public List<(Feature Feature, TimeSpan Duration)> SlowestFeatures { get; private set; } = new();
    public List<(Scenario Scenario, string FeatureName, TimeSpan Duration)> SlowestScenarios { get; private set; } = new();
    public List<(Scenario Scenario, string SectionName, TimeSpan Duration)> SlowestScenariosInSelectedFeature { get; private set; } = new();

    public void OnGet(string productName, Guid id, Guid? featureId)
    {
        var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture;
        CurrentCulture = requestCulture.Culture.Name;

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

        if (latestRun != null && latestRun.BaseUrl != null)
        {
            GithubLink = latestRun.BaseUrl;
        }

        if (latestRun != null && latestRun.CommitId != null)
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

    private void PopulateFeatures(SpecGurka.GurkaSpec.Product product)
    {
        Features = product.Features.Select(f => new Feature
        {
            Id = f.Id,
            Name = f.Name,
            Tags = f.Tags,
            Status = f.Status,
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

            if (parts.Length < 1) continue;

            string directoryName = parts.Length > 1 ? parts[parts.Length - 2] : "Root";

            if (directoryName.Equals("Features", StringComparison.OrdinalIgnoreCase))
            {
                if (!FeatureTree.ContainsKey("Features"))
                {
                    FeatureTree["Features"] = new List<Feature>();
                }

                ((List<Feature>)FeatureTree["Features"]).Add(feature);
            }
            else
            {
                var currentLevel = FeatureTree;

                if (!currentLevel.ContainsKey(directoryName))
                {
                    currentLevel[directoryName] = new Dictionary<string, object>();
                }

                var directoryLevel = (Dictionary<string, object>)currentLevel[directoryName];

                if (!directoryLevel.ContainsKey("Features"))
                {
                    directoryLevel["Features"] = new List<Feature>();
                }

                ((List<Feature>)directoryLevel["Features"]).Add(feature);
            }
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
            paramValues[headerCells[i]] = rowCells[i];
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

        return children.FirstOrDefault();
    }
}