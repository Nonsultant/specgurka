using SpecGurka.GurkaSpec;
using Background = Gherkin.Ast.Background;
using Feature = Gherkin.Ast.Feature;
using Rule = Gherkin.Ast.Rule;
using Scenario = Gherkin.Ast.Scenario;

namespace SpecGurka.GenGurka.Extensions;

internal static class GherkinDocumentExtensions
{
    internal static GurkaSpec.Feature ToGurkaFeature(this Feature gherkinFeature)
    {
        var gurkaFeature = new GurkaSpec.Feature
        {
            Name = gherkinFeature.Name,
            Description = gherkinFeature.Description
        };

        var featureTags = gherkinFeature.Tags?.Select(tag => tag.Name).ToList() ?? new List<string>();
        var featureIgnored = featureTags.Any(tag => tag == "@ignore");

        foreach (var featureChild in gherkinFeature.Children)
        {
            switch (featureChild)
            {
                case Background background:
                    gurkaFeature.Background = background.ToGurkaBackground();
                    break;
                case Scenario scenario:
                    var gurkaScenario = scenario.ToGurkaScenario();
                    gurkaFeature.Scenarios.Add(gurkaScenario);
                    if (gurkaScenario.Tags.Contains("@ignore"))
                    {
                        featureIgnored = true;
                    }
                    break;
                case Rule rule:
                    var gurkaRule = rule.ToGurkaRule();
                    gurkaFeature.Rules.Add(gurkaRule);
                    if (gurkaRule.Tags.Contains("@ignore") || gurkaRule.Scenarios.Any(s => s.Tags.Contains("@ignore")))
                    {
                        featureIgnored = true;
                    }
                    break;
            }
        }

        if (featureIgnored)
        {
            SetFeatureStatusToNotImplemented(gurkaFeature);
        }

        // Remove duplicate tags
        gurkaFeature.Tags = featureTags.Distinct().ToList();

        return gurkaFeature;
    }

    static GurkaSpec.Rule ToGurkaRule(this Rule rule)
    {
        var gurkaRule = new GurkaSpec.Rule
        {
            Name = rule.Name,
            Description = rule.Description?.TrimStart() ?? string.Empty
        };

        var ruleTags = rule.Tags?.Select(tag => tag.Name).ToList() ?? new List<string>();
        gurkaRule.Tags = ruleTags;

        foreach (var ruleChild in rule.Children)
        {
            if (ruleChild is Background background)
            {
                gurkaRule.Background = background.ToGurkaBackground();
            }
            else if (ruleChild is Scenario scenario)
            {
                var gurkaScenario = scenario.ToGurkaScenario();

                gurkaRule.Scenarios.Add(gurkaScenario);
            }
        }

        return gurkaRule;
    }

    static GurkaSpec.Scenario ToGurkaScenario(this Scenario scenario)
    {
        var gurkaScenario = new GurkaSpec.Scenario
        {
            Name = scenario.Name,
            Description = scenario.Description?.TrimStart() ?? string.Empty,
            Examples = scenario.Examples?.FirstOrDefault()?.ToMarkDownString() ?? string.Empty
        };
        foreach (var step in scenario.Steps)
        {
            gurkaScenario.AddStep(step);
        }

        var scenarioTags = scenario.Tags?.Select(tag => tag.Name).ToList() ?? new List<string>();
        gurkaScenario.Tags = scenarioTags;

        var ignored = scenario.Tags.Any(t => t.Name == "@ignore");
        if (ignored)
        {
            gurkaScenario.Status = Status.NotImplemented;
            gurkaScenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
        }

        return gurkaScenario;
    }

    static GurkaSpec.Background ToGurkaBackground(this Background background)
    {
        var gurkaBackground = new GurkaSpec.Background
        {
            Name = background.Name ?? string.Empty,
            Description = background.Description?.TrimStart() ?? string.Empty
        };

        foreach (var step in background.Steps)
        {
            gurkaBackground.AddStep(step);
        }

        return gurkaBackground;
    }

    static void SetFeatureStatusToNotImplemented(GurkaSpec.Feature gurkaFeature)
    {
        gurkaFeature.Status = Status.NotImplemented;

        if (gurkaFeature.Background != null)
        {
            gurkaFeature.Background.Status = Status.NotImplemented;
            gurkaFeature.Background.Steps.ForEach(step => step.Status = Status.NotImplemented);
        }

        gurkaFeature.Scenarios.ForEach(scenario =>
        {
            scenario.Status = Status.NotImplemented;
            scenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
        });

        gurkaFeature.Rules.ForEach(rule =>
        {
            if (rule.Tags.Contains("@ignore") || rule.Scenarios.Any(s => s.Tags.Contains("@ignore")))
            {
                rule.Status = Status.NotImplemented;
                if (rule.Background != null)
                {
                    rule.Background.Status = Status.NotImplemented;
                    rule.Background.Steps.ForEach(step => step.Status = Status.NotImplemented);
                }

                rule.Scenarios.ForEach(scenario =>
                {
                    scenario.Status = Status.NotImplemented;
                    scenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
                });
            }
        });
    }
}