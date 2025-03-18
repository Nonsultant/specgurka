using SpecGurka.GurkaSpec;
using Background = Gherkin.Ast.Background;
using Feature = Gherkin.Ast.Feature;
using Rule = Gherkin.Ast.Rule;
using Scenario = Gherkin.Ast.Scenario;
using DataTable = Gherkin.Ast.DataTable;
using TableRow = Gherkin.Ast.TableRow;
using System.Text;

namespace SpecGurka.GenGurka.Extensions;

public static class ScenarioOutlineDataTableHelper
{

    public static string ProcessDataTable(DataTable dataTable, List<string> headers, TableRow exampleRow)
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

    public static string ReplaceParameters(string text, List<string> headers, TableRow row)
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
}