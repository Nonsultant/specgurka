using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

public static class BackgroundExtensions
{
    public static void AddStep(this GurkaSpec.Background scenario, Step step)
    {
        scenario.Steps.Add(new GurkaSpec.Step
        {
            Text = step.Argument is DataTable dataTable
                ? $"{step.Text} {dataTable.ToMarkdownString()}"
                : step.Text,
            Kind = step.Keyword.Trim()
        });
    }
}