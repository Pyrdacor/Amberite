using System.Net;
using System.Text;

namespace Ambermoon.Aon.Doc;

internal static class HtmlWriter
{
    public static string Render(AonDoc doc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine($"<meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{Esc(doc.Title)}</title>");
        sb.AppendLine(Style);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{Esc(doc.Title)}</h1>");

        foreach (var table in doc.Tables)
        {
            if (table.Title != null)
                sb.AppendLine($"<h2>{Esc(table.Title)}</h2>");

            RenderTable(sb, table);
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void RenderTable(StringBuilder sb, DocTable table)
    {
        sb.AppendLine("<div class=\"tbl-wrap\">");
        sb.AppendLine("<table>");

        // Header
        sb.AppendLine("<thead><tr>");
        foreach (var h in table.Headers)
            sb.AppendLine($"<th>{Esc(h)}</th>");
        sb.AppendLine("</tr></thead>");

        // Body
        sb.AppendLine("<tbody>");
        bool odd = false;
        foreach (var row in table.Rows)
        {
            sb.AppendLine(odd ? "<tr class=\"odd\">" : "<tr>");
            odd = !odd;
            for (int c = 0; c < table.Headers.Count; c++)
            {
                var cell = c < row.Count ? row[c] : new Cell("");
                var text = Esc(cell.Text);
                sb.AppendLine(cell.IsCode ? $"<td><code>{text}</code></td>" : $"<td>{text}</td>");
            }
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
    }

    private static string Esc(string s) => WebUtility.HtmlEncode(s);

    private const string Style = """
        <style>
          body { font-family: system-ui, sans-serif; margin: 2rem; color: #111; background: #fafafa; }
          h1 { font-size: 1.6rem; border-bottom: 2px solid #ccc; padding-bottom: .4rem; }
          h2 { font-size: 1.1rem; margin-top: 2rem; color: #444; }
          .tbl-wrap { overflow-x: auto; margin-bottom: 2rem; }
          table { border-collapse: collapse; font-size: .85rem; white-space: nowrap; }
          th { background: #2c3e50; color: #fff; padding: .4rem .8rem; text-align: left; }
          td { padding: .3rem .8rem; border-bottom: 1px solid #e0e0e0; }
          tr.odd td { background: #f5f5f5; }
          code { font-family: ui-monospace, monospace; color: #c0392b; }
          td:first-child { font-weight: 500; }
        </style>
        """;
}
