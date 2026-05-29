using Ambermoon.Aon;
using Ambermoon.Aon.Doc;

// ── argument parsing ─────────────────────────────────────────────────────────

string? aodPath   = null;
string? outBase   = null;
var format        = OutputFormat.Both;
var aonPaths      = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--aod":    aodPath = args[++i]; break;
        case "--out":    outBase = args[++i]; break;
        case "--format":
            format = (i + 1 < args.Length ? args[++i] : "") switch
            {
                "md"   => OutputFormat.Md,
                "html" => OutputFormat.Html,
                _      => OutputFormat.Both,
            };
            break;
        default:
            if (!args[i].StartsWith('-'))
                aonPaths.AddRange(ExpandGlob(args[i]));
            else
                Console.Error.WriteLine($"Unknown option: {args[i]}");
            break;
    }
}

if (aonPaths.Count == 0)
{
    Console.Error.WriteLine("""
        Usage: aon-doc [--aod <file.aod>] [--out <base>] [--format md|html|both] <file1.aon> [...]

          --aod <file.aod>   AOD schema: adds Offset/Type columns and enum/bitfield tables
          --out <base>       Output base name (default: first AON filename or "output")
          --format md|html   Output format (default: both)

        Modes:
          AOD given OR single AON  →  one row per property
          No AOD + multiple AONs   →  one row per entity, one column per property
        """);
    return 1;
}

// ── parse AOD ────────────────────────────────────────────────────────────────

AodFile? aodFile = null;
if (aodPath != null)
{
    var result = AodFileParser.ParseFile(aodPath);
    foreach (var d in result.Diagnostics)
        Console.Error.WriteLine($"[{aodPath}] {d}");
    if (!result.Success) return 1;
    aodFile = result.File;
}

// ── parse AON files ───────────────────────────────────────────────────────────

var inputs = new List<(string FileName, AonDocument Doc)>();
foreach (var path in aonPaths)
{
    var result = AonDocumentParser.ParseFile(path);
    foreach (var d in result.Diagnostics)
        Console.Error.WriteLine($"[{path}] {d}");
    if (!result.Success) return 1;
    inputs.Add((Path.GetFileNameWithoutExtension(path), result.Document!));
}

// ── determine output base name ────────────────────────────────────────────────

outBase ??= aonPaths.Count == 1
    ? Path.GetFileNameWithoutExtension(aonPaths[0])
    : "output";

// ── generate ─────────────────────────────────────────────────────────────────

var doc = new DocGenerator(aodFile, inputs).Build();

if (format != OutputFormat.Html)
{
    var path = outBase + ".md";
    File.WriteAllText(path, MarkdownWriter.Render(doc), System.Text.Encoding.UTF8);
    Console.WriteLine($"  OK  {path}");
}
if (format != OutputFormat.Md)
{
    var path = outBase + ".html";
    File.WriteAllText(path, HtmlWriter.Render(doc), System.Text.Encoding.UTF8);
    Console.WriteLine($"  OK  {path}");
}

return 0;

// ─────────────────────────────────────────────────────────────────────────────

// Expand a path that may contain * or ? wildcards into matching file paths,
// sorted. Returns a single-element list for plain paths (no wildcards).
static IEnumerable<string> ExpandGlob(string pattern)
{
    if (!pattern.Contains('*') && !pattern.Contains('?'))
        return [pattern];

    var dir     = Path.GetDirectoryName(pattern);
    var fileGlob = Path.GetFileName(pattern);
    var searchIn = string.IsNullOrEmpty(dir) ? "." : dir;
    return Directory.GetFiles(searchIn, fileGlob).OrderBy(f => f);
}

enum OutputFormat { Both, Md, Html }
