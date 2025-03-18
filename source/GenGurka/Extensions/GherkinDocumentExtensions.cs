using SpecGurka.GurkaSpec;
using Background = Gherkin.Ast.Background;
using Feature = Gherkin.Ast.Feature;
using Rule = Gherkin.Ast.Rule;
using Scenario = Gherkin.Ast.Scenario;
using DataTable = Gherkin.Ast.DataTable;
using TableRow = Gherkin.Ast.TableRow;
using System.Text;
using SpecGurka.GenGurka.Helpers;

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
                    if (scenario.Examples?.Any() == true) // this is a scenario outline
                    {
                        var expandedScenarios = ExpandScenarioOutline(scenario);
                        foreach (var expandedScenario in expandedScenarios)
                        {
                            gurkaFeature.Scenarios.Add(expandedScenario);
                            if (expandedScenario.Tags.Contains("@ignore"))
                            {
                                featureIgnored = true;
                            }
                        }
                    }
                    else
                    {
                        var gurkaScenario = scenario.ToGurkaScenario();
                        gurkaFeature.Scenarios.Add(gurkaScenario);
                        if (gurkaScenario.Tags.Contains("@ignore"))
                        {
                            featureIgnored = true;
                        }
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
            StatusPropagationHelper.MarkIgnoredItemsStatus(gurkaFeature);
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
                if (scenario.Examples?.Any() == true)
                {
                    // This is a scenario outline - expand it into individual scenarios
                    var expandedScenarios = ExpandScenarioOutline(scenario);
                    foreach (var expandedScenario in expandedScenarios)
                    {
                        gurkaRule.Scenarios.Add(expandedScenario);
                    }
                }
                else
                {
                    // Regular scenario
                    var gurkaScenario = scenario.ToGurkaScenario();
                    gurkaRule.Scenarios.Add(gurkaScenario);
                }
            }
        }

        return gurkaRule;
    }

    static GurkaSpec.Scenario ToGurkaScenario(this Scenario scenario)
    {
        // If this is a scenario outline (has Examples)
        if (scenario.Examples?.Any() == true)
        {
            // Create the template scenario
            var templateScenario = new GurkaSpec.Scenario
            {
                Name = scenario.Name,
                Description = scenario.Description?.TrimStart() ?? string.Empty,
                Examples = scenario.Examples?.FirstOrDefault()?.ToMarkDownString() ?? string.Empty,
                IsOutline = true
            };

            foreach (var step in scenario.Steps)
            {
                templateScenario.AddStep(step);
            }

            var scenarioTags = scenario.Tags?.Select(tag => tag.Name).ToList() ?? new List<string>();
            templateScenario.Tags = scenarioTags;

            var ignored = scenario.Tags?.Any(t => t.Name == "@ignore") ?? false;
            if (ignored)
            {
                templateScenario.Status = Status.NotImplemented;
                templateScenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
            }

            return templateScenario;
        }
        else
        {
            // Regular scenario handling
            var gurkaScenario = new GurkaSpec.Scenario
            {
                Name = scenario.Name,
                Description = scenario.Description?.TrimStart() ?? string.Empty,
            };

            if (gurkaScenario.IsOutline && scenario.Examples?.FirstOrDefault() != null)
            {
                gurkaScenario.Examples = scenario.Examples.FirstOrDefault().ToMarkDownString();
            }

            foreach (var step in scenario.Steps)
            {
                gurkaScenario.AddStep(step);
            }

            var scenarioTags = scenario.Tags?.Select(tag => tag.Name).ToList() ?? new List<string>();
            gurkaScenario.Tags = scenarioTags;

            var ignored = scenario.Tags?.Any(t => t.Name == "@ignore") ?? false;
            if (ignored)
            {
                gurkaScenario.Status = Status.NotImplemented;
                gurkaScenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
            }

            return gurkaScenario;
        }
    }

    private static List<GurkaSpec.Scenario> ExpandScenarioOutline(Scenario scenarioOutline)
    {
        var result = new List<GurkaSpec.Scenario>();

        var templateScenario = scenarioOutline.ToGurkaScenario();
        result.Add(templateScenario);

        //process each example to create scenarios
        foreach (var examples in scenarioOutline.Examples)
        {
            var headers = examples.TableHeader.Cells.Select(c => c.Value).ToList();

            foreach (var row in examples.TableBody)
            {
                var exampleScenario = new GurkaSpec.Scenario
                {
                    Name = ScenarioOutlineDataTableHelper.ReplaceParameters(scenarioOutline.Name, headers, row),
                    Description = templateScenario.Description,
                    Tags = new List<string>(templateScenario.Tags),
                    IsOutlineChild = true
                };

                foreach (var templateStep in scenarioOutline.Steps)
                {
                    var stepText = ScenarioOutlineDataTableHelper.ReplaceParameters(templateStep.Text, headers, row);

                    var step = new GurkaSpec.Step
                    {
                        Kind = templateStep.Keyword.Trim(),
                        Text = stepText,
                        Status = templateScenario.Status
                    };

                    if (templateStep.Argument is DataTable dataTable)
                    {
                        var processedTable = ScenarioOutlineDataTableHelper.ProcessDataTable(dataTable, headers, row);
                        step.Table = processedTable;
                    }

                    exampleScenario.Steps.Add(step);
                }

                // Add examples information to help identify this is from an outline
                exampleScenario.Examples = $"Example: {string.Join(", ", row.Cells.Select((c, i) => $"{headers[i]}=\"{c.Value}\""))}";

                result.Add(exampleScenario);
            }
        }

        return result;
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
}