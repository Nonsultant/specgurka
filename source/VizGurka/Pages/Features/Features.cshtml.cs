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
    public string select_feature { get; set; }
    public Guid Id { get; set; }
    public List<Feature> Features { get; set; } = new List<Feature>();
    public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public List<Guid> FeatureIds { get; set; } = new List<Guid>();
    public Feature? SelectedFeature { get; set; }
    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public string ProductName { get; set; } = string.Empty;
    public DateTime LatestRunDate { get; set; }

    public void OnGet(string productName, Guid id, Guid? featureId)
    {
        select_feature = _localizer["select_feature"];

        ProductName = productName;
        Id = id;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        var product = latestRun?.Products.FirstOrDefault();
        if (product != null)
        {
            PopulateFeatures(product);
            PopulateFeatureIds();
        }

        if (latestRun != null)
        {
            LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
        }

        if (featureId.HasValue)
        {
            SelectedFeature = Features.FirstOrDefault(f => f.Id == featureId.Value);
            if (SelectedFeature != null)
            {
                PopulateScenarios(SelectedFeature);
            }
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
            Description = f.Description
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

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        var trimmedInput = input.Trim();
        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }

}