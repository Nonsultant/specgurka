using Gherkin.Ast;
using SpecGurka.GurkaSpec;
using Background = Gherkin.Ast.Background;
using Rule = Gherkin.Ast.Rule;
using Scenario = Gherkin.Ast.Scenario;

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

        var featureIgnored = gherkinDoc.Feature.Tags.Any(tag => tag.Name == "@ignore");

        if (featureIgnored)
        {
            gurkaFeature.Status = Status.NotImplemented;
        }

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
                var ignored = scenario.Tags.Any(t => t.Name == "@ignore");

                var gurkaScenario = new GurkaSpec.Scenario
                {
                    Name = scenario.Name,
                    Description = scenario.Description?.TrimStart() ?? string.Empty,
                    Status = ignored ? Status.NotImplemented : Status.Failed
                };
                foreach (var step in scenario.Steps)
                {
                    gurkaScenario.AddStep(step);
                }

                if (ignored)
                {
                    gurkaScenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
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
                        var ignored = rScenario.Tags.Any(t => t.Name == "@ignore");

                        var gurkaScenario = new GurkaSpec.Scenario
                        {
                            Name = rScenario.Name,
                            Description = rScenario.Description?.TrimStart() ?? string.Empty,
                            Status = ignored ? Status.NotImplemented : Status.Failed
                        };

                        foreach (var step in rScenario.Steps)
                        {
                            gurkaScenario.AddStep(step);
                        }

                        if (ignored)
                        {
                            gurkaScenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
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