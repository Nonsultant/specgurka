using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

internal static class GherkinDocumentExtensions
{
    internal static GurkaSpec.Feature ToGurkaFeature(this GherkinDocument gherkinDoc)
    {
        var gurkaFeature = new GurkaSpec.Feature
        {
            Name = gherkinDoc.Feature.Name,
            Description = gherkinDoc.Feature.Description
        };

        foreach (var featureChild in gherkinDoc.Feature.Children)
        {
            if (featureChild is Background background)
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
                gurkaFeature.Background = gurkaBackground;
            }
            else if (featureChild is Scenario scenario)
            {
                var gurkaScenario = new GurkaSpec.Scenario
                {
                    Name = scenario.Name,
                    Description = scenario.Description?.TrimStart() ?? string.Empty
                };
                foreach (var step in scenario.Steps)
                {
                    gurkaScenario.AddStep(step);
                }
                gurkaFeature.Scenarios.Add(gurkaScenario);
            }
            else if (featureChild is Rule rule)
            {
                var gurkaRule = new GurkaSpec.Rule
                {
                    Name = rule.Name,
                    Description = rule.Description?.TrimStart() ?? string.Empty
                };
                foreach (var ruleChild in rule.Children)
                {
                    if (ruleChild is Background rBackground)
                    {
                        var gurkaBackground = new GurkaSpec.Background
                        {
                            Name = rBackground.Name,
                            Description = rBackground.Description?.TrimStart() ?? string.Empty
                        };
                        foreach (var step in rBackground.Steps)
                        {
                            gurkaBackground.AddStep(step);
                        }
                        gurkaRule.Background = gurkaBackground;
                    }
                    else if (ruleChild is Scenario rScenario)
                    {
                        var gurkaScenario = new GurkaSpec.Scenario
                        {
                            Name = rScenario.Name,
                            Description = rScenario.Description?.TrimStart() ?? string.Empty
                        };
                        foreach (var step in rScenario.Steps)
                        {
                            gurkaScenario.AddStep(step);
                        }
                        gurkaRule.Scenarios.Add(gurkaScenario);
                    }
                }
                gurkaFeature.Rules.Add(gurkaRule);
            }
        }
        return gurkaFeature;
    }

}