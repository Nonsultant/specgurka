using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

public static class ScenarioExtensions
{
    public static void AddStep(this GurkaSpec.Scenario scenario, Step step)
    {
        scenario.Steps.Add(new GurkaSpec.Step
        {
            Kind = step.Keyword.Trim(),
            Text = step.Text,
            Table = step.Argument is DataTable dataTable
                ? dataTable.ToMarkdownString()
                : null,
        });
    }
}