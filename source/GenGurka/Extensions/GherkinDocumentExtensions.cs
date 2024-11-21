using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

public static class GherkinDocumentExtensions
{

    public static GurkaSpec.Feature ToGurkaFeature(this GherkinDocument gherkinDoc)
    {
        var gurkaFeature = new GurkaSpec.Feature
        {
            Name = gherkinDoc.Feature.Name,
            Description = gherkinDoc.Feature.Description
        };
        foreach (var featureChild in gherkinDoc.Feature.Children)
        {
            if (featureChild is Scenario scenario)
            {
                var gurkaScenario = new GurkaSpec.Scenario
                {
                    Name = scenario.Name
                };
                foreach (var step in scenario.Steps)
                {
                    gurkaScenario.Steps.Add(new GurkaSpec.Step
                    {
                        Text = step.Text,
                        Kind = step.Keyword.Trim()
                    });
                }
                gurkaFeature.Scenarios.Add(gurkaScenario);
            }
            else if (featureChild is Rule rule)
            {
                var gurkaRule = new GurkaSpec.Rule { Name = rule.Name };
                foreach (var ruleChild in rule.Children)
                {
                    if (ruleChild is Gherkin.Ast.Scenario rScenario)
                    {
                        var gurkaScenario = new GurkaSpec.Scenario { Name = rScenario.Name };
                        foreach (var step in rScenario.Steps)
                        {
                            gurkaScenario.Steps.Add(new GurkaSpec.Step
                            {
                                Text = step.Text,
                                Kind = step.Keyword.Trim()
                            });
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