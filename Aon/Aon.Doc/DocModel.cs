namespace Ambermoon.Aon.Doc;

// A single table cell — may be plain text or "code" (renders in monospace/code block).
internal sealed record Cell(string Text, bool IsCode = false);

// A document table with optional title, column headers, and rows.
internal sealed record DocTable(
    string? Title,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<Cell>> Rows);

// Top-level document — a title and a sequence of tables.
internal sealed record AonDoc(string Title, IReadOnlyList<DocTable> Tables);
