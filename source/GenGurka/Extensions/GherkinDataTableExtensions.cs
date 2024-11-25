using System.Text;
using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

public static class GherkinDataTableExtensions
{
    public static string ToMarkdownString(this DataTable dataTable)
    {
        var sb = new StringBuilder();

        var headers = dataTable.Rows.First().Cells;
        foreach (var cell in headers)
        {
            sb.Append("| ").Append(cell.Value).Append(" ");
        }
        sb.AppendLine("|");

        foreach (var row in dataTable.Rows.Skip(1))
        {
            foreach (var cell in row.Cells)
            {
                sb.Append("| ").Append(cell.Value).Append(" ");
            }
            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}