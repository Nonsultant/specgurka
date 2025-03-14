using SpecGurka.GurkaSpec;
using Background = Gherkin.Ast.Background;
using Feature = Gherkin.Ast.Feature;
using Rule = Gherkin.Ast.Rule;
using Scenario = Gherkin.Ast.Scenario;
using DataTable = Gherkin.Ast.DataTable;
using TableRow = Gherkin.Ast.TableRow;
using System.Text;

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
                Examples = scenario.Examples?.FirstOrDefault()?.ToMarkDownString() ?? string.Empty

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
            // Regular scenario handling (unchanged)
            var gurkaScenario = new GurkaSpec.Scenario
            {
                Name = scenario.Name,
                Description = scenario.Description?.TrimStart() ?? string.Empty,
            };

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

        // Get the template scenario
        var templateScenario = scenarioOutline.ToGurkaScenario();

        // Add the template scenario as the first item (this represents the outline itself)
        result.Add(templateScenario);

        // Now process each example to create concrete scenarios
        foreach (var examples in scenarioOutline.Examples)
        {
            var headers = examples.TableHeader.Cells.Select(c => c.Value).ToList();

            // Process each row in the examples table
            foreach (var row in examples.TableBody)
            {
                // Create a new scenario for this example row
                var exampleScenario = new GurkaSpec.Scenario
                {
                    Name = ReplaceParameters(scenarioOutline.Name, headers, row),
                    Description = templateScenario.Description,
                    Tags = new List<string>(templateScenario.Tags)
                };

                // Process each step, replacing parameters
                foreach (var templateStep in scenarioOutline.Steps)
                {
                    var stepText = ReplaceParameters(templateStep.Text, headers, row);

                    var step = new GurkaSpec.Step
                    {
                        Kind = templateStep.Keyword.Trim(),
                        Text = stepText,
                        Status = templateScenario.Status // Inherit status from template
                    };

                    // Handle data tables in steps if they exist
                    if (templateStep.Argument is DataTable dataTable)
                    {
                        // Replace parameters in each data table cell as well
                        var processedTable = ProcessDataTable(dataTable, headers, row);
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

    private static string ReplaceParameters(string text, List<string> headers, TableRow row)
    {
        if (text == null) return string.Empty;

        string result = text;
        var cells = row.Cells.ToList();

        for (int i = 0; i < headers.Count && i < cells.Count; i++)
        {
            var paramName = headers[i];
            var paramValue = cells[i].Value;
            result = result.Replace($"<{paramName}>", paramValue);
        }

        return result;
    }

    private static string ProcessDataTable(DataTable dataTable, List<string> headers, TableRow exampleRow)
    {
        var sb = new StringBuilder();
        var rows = dataTable.Rows.ToList();

        if (rows.Count == 0)
            return string.Empty;

        // Process header row
        var headerRow = rows[0];
        var headerCells = headerRow.Cells.ToList();

        sb.Append("|");
        foreach (var cell in headerCells)
        {
            var processedCell = ReplaceParameters(cell.Value, headers, exampleRow);
            sb.Append($" {processedCell} |");
        }
        sb.AppendLine();

        // Add separator row
        sb.Append("|");
        for (int i = 0; i < headerCells.Count; i++)
        {
            sb.Append(" --- |");
        }
        sb.AppendLine();

        // Process data rows
        for (int i = 1; i < rows.Count; i++)
        {
            var rowCells = rows[i].Cells.ToList();
            sb.Append("|");
            foreach (var cell in rowCells)
            {
                var processedCell = ReplaceParameters(cell.Value, headers, exampleRow);
                sb.Append($" {processedCell} |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
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