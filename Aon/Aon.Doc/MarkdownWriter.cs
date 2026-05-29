using System.Text;

namespace Ambermoon.Aon.Doc;

internal static class MarkdownWriter
{
    public static string Render(AonDoc doc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {doc.Title}");
        sb.AppendLine();

        foreach (var table in doc.Tables)
        {
            if (table.Title != null)
            {
                sb.AppendLine($"## {table.Title}");
                sb.AppendLine();
            }

            RenderTable(sb, table);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void RenderTable(StringBuilder sb, DocTable table)
    {
        if (table.Rows.Count == 0) return;

        // Compute widths from raw text only (backtick wrapping is extra decoration)
        var widths = table.Headers.Select(h => h.Length).ToArray();
        foreach (var row in table.Rows)
            for (int c = 0; c < row.Count && c < widths.Length; c++)
                widths[c] = Math.Max(widths[c], row[c].Text.Length);

        // Header row
        sb.Append('|');
        for (int c = 0; c < table.Headers.Count; c++)
            sb.Append($" {table.Headers[c].PadRight(widths[c])} |");
        sb.AppendLine();

        // Separator
        sb.Append('|');
        foreach (var w in widths)
            sb.Append($" {new string('-', w)} |");
        sb.AppendLine();

        // Data rows
        foreach (var row in table.Rows)
        {
            sb.Append('|');
            for (int c = 0; c < table.Headers.Count; c++)
            {
                var cell = c < row.Count ? row[c] : new Cell("");
                var text = EscapeMarkdown(cell.Text);
                // Code cells: wrap in backticks without padding (renderers align correctly)
                // Text cells: pad for readable raw source
                sb.Append(cell.IsCode ? $" `{text}` |" : $" {text.PadRight(widths[c])} |");
            }
            sb.AppendLine();
        }
    }

    private static string EscapeMarkdown(string s) =>
        s.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
}
