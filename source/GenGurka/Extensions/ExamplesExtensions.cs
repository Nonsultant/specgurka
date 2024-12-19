using System.Text;
using Gherkin.Ast;

namespace SpecGurka.GenGurka.Extensions;

public static class ExamplesExtensions
{
    public static string ToMarkDownString(this Examples example)
    {
        var sb = new StringBuilder();

        foreach (var cell in example.TableHeader.Cells)
        {
            sb.Append("| ").Append(cell.Value).Append(" ");
        }
        sb.AppendLine("|");

        foreach (var cell in example.TableHeader.Cells)
        {
            sb.Append("| --- ");
        }
        sb.AppendLine("|");

        foreach (var row in example.TableBody)
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