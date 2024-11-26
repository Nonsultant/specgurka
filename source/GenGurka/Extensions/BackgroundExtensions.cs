using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

public static class BackgroundExtensions
{
    public static void AddStep(this GurkaSpec.Background scenario, Step step)
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