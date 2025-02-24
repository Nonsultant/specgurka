using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;

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
    public DateTime LatestRunDate { get; set; } = DateTime.MinValue;

    public Dictionary<string, object> FeatureTree { get; set; } = new();
    public void OnGet(string productName, Guid id, Guid? featureId)
    {

        ProductName = productName;
        Id = id;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        var product = latestRun?.Products.FirstOrDefault();
        if (product != null)
        {
            PopulateFeatures(product);
            PopulateFeatureIds();
            BuildFeatureTree();
        }

        if (latestRun != null)
        {
            LatestRunDate = DateTime.Parse(latestRun.RunDate, CultureInfo.InvariantCulture);
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

            // Continue if not enough parts
            if (parts.Length < 1) continue;

            // Build the tree dynamically
            var currentLevel = FeatureTree;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (i == parts.Length - 1)
                {
                    // Add the feature to the final level
                    if (!currentLevel.ContainsKey("Features"))
                    {
                        currentLevel["Features"] = new List<Feature>();
                    }
                    ((List<Feature>)currentLevel["Features"]).Add(feature);
                }
                else
                {
                    // Navigate to the next level of the tree
                    if (!currentLevel.ContainsKey(part))
                    {
                        currentLevel[part] = new Dictionary<string, object>();
                    }
                    currentLevel = (Dictionary<string, object>)currentLevel[part];
                }
            }
        }
    }

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        var trimmedInput = input.Trim();
        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }

}