using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpecGurka.GurkaSpec;
using VizGurka.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VizGurka.Pages.Product;

public class ProductModel : PageModel
{
    public Guid Id { get; set; }
    public List<Feature> Features { get; set; } = new List<Feature>();
    public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public List<Guid> FeatureIds { get; set; } = new List<Guid>(); // New list to store feature IDs
    public Feature? SelectedFeature { get; set; }
    public MarkdownPipeline Pipeline { get; set; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    public string ProductName { get; set; } = string.Empty;
    public DateTime LatestRunDate { get; set; }

    public void OnGet(string productName, Guid id, Guid? featureId)
    {
        ProductName = productName;
        Id = id;
        var latestRun = TestrunReader.ReadLatestRun(productName);
        var product = latestRun?.Products.FirstOrDefault();
        if (product != null)
        {
            PopulateFeatures(product);
            PopulateScenarios();
            PopulateFeatureIds(); // Populate the feature IDs list
        }

        if (latestRun != null)
        {
            LatestRunDate = DateTime.Parse(latestRun.DateAndTime);
        }

        Console.WriteLine($"1 Received featureId: {featureId}");
        Console.WriteLine($"2 Total features: {Features.Count}");
        foreach (var feature in Features)
        {
            Console.WriteLine($"3 Feature ID: {feature.Id}, Name: {feature.Name}");
        }

        if (featureId.HasValue)
        {
            SelectedFeature = Features.FirstOrDefault(f => f.Id == featureId.Value);
            Console.WriteLine($"4 SelectedFeature ID: {SelectedFeature?.Id}");
        }
        else
        {
            Console.WriteLine("5 No featureId provided");
        }

        // Print the list of feature IDs after assignment
        Console.WriteLine("6 Feature IDs:");
        foreach (var ids in FeatureIds)
        {
            Console.WriteLine(ids);
        }
    }

    private void PopulateFeatures(SpecGurka.GurkaSpec.Product product)
    {
        Features = product.Features.Select(f => new Feature
        {
            Id = f.Id,
            Name = f.Name,
            Status = f.Status,
            Scenarios = f.Scenarios,
            Rules = f.Rules
        }).ToList();
    }

    private void PopulateScenarios()
    {
        Scenarios = Features
            .SelectMany(f => f.Scenarios.Concat(f.Rules.SelectMany(r => r.Scenarios)))
            .ToList();
    }

    private void PopulateFeatureIds()
    {
        FeatureIds = Features.Select(f => f.Id).ToList();
        Console.WriteLine("7 Feature IDs:");
        foreach (var id in FeatureIds)
        {
            Console.WriteLine(id);
        }
    }

    public IHtmlContent MarkdownStringToHtml(string input)
    {
        var trimmedInput = input.Trim();
        return new HtmlString(Markdown.ToHtml(trimmedInput, Pipeline));
    }
}