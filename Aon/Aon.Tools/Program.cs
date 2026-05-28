using Ambermoon.Aon;
using Ambermoon.Aon.Tools;

if (args.Length == 0)
{
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  chars   <source-dir> <output-dir>          Convert binary party_char files to AON");
    Console.Error.WriteLine("  compile <schema.aod> <data.aon> <out-dir>  Compile AON to binary files");
    return 1;
}

return args[0] switch
{
    "chars"   => RunChars(args[1..]),
    "compile" => RunCompile(args[1..]),
    var cmd   => Error($"Unknown command: {cmd}"),
};

// ── chars ────────────────────────────────────────────────────────────────────

static int RunChars(string[] args)
{
    if (args.Length < 1) return Error("Usage: chars <source-dir> [output-dir]");

    var sourceDir = args[0];
    var outputDir = args.Length >= 2 ? args[1] : args[0];

    if (!Directory.Exists(sourceDir)) return Error($"Source directory not found: {sourceDir}");
    Directory.CreateDirectory(outputDir);

    int ok = 0, errors = 0;
    foreach (var filePath in Directory.GetFiles(sourceDir).OrderBy(f => f))
    {
        var fileName = Path.GetFileName(filePath);
        var outPath  = Path.Combine(outputDir, fileName + ".aon");
        try
        {
            var data = File.ReadAllBytes(filePath);
            if (data.Length < 0x0122) { Console.Error.WriteLine($"  SKIP  {fileName}  (too small: {data.Length} bytes)"); continue; }
            var charData = CharacterReader.Read(data);
            File.WriteAllText(outPath, CharacterAonWriter.Write(charData, fileName), System.Text.Encoding.UTF8);
            Console.WriteLine($"  OK    {fileName}  →  {Path.GetFileName(outPath)}  ({charData.Name})");
            ok++;
        }
        catch (Exception ex) { Console.Error.WriteLine($"  ERROR {fileName}: {ex.Message}"); errors++; }
    }

    Console.WriteLine($"\nDone: {ok} converted, {errors} errors.");
    return errors > 0 ? 1 : 0;
}

// ── compile ──────────────────────────────────────────────────────────────────

static int RunCompile(string[] args)
{
    if (args.Length < 3) return Error("Usage: compile <schema.aod> <data.aon> <out-dir>");

    var aodPath  = args[0];
    var aonPath  = args[1];
    var outDir   = args[2];

    if (!File.Exists(aodPath)) return Error($"AOD file not found: {aodPath}");
    if (!File.Exists(aonPath)) return Error($"AON file not found: {aonPath}");

    var aodResult = AodFileParser.ParseFile(aodPath);
    PrintDiagnostics(aodResult.Diagnostics, aodPath);
    if (!aodResult.Success) return 1;

    var aonResult = AonDocumentParser.ParseFile(aonPath);
    PrintDiagnostics(aonResult.Diagnostics, aonPath);
    if (!aonResult.Success) return 1;

    var backend       = new AmbermoonBinaryBackend();
    var compileResult = backend.Compile(aodResult.File!, aonResult.Document!);

    foreach (var d in compileResult.Diagnostics)
        (d.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out).WriteLine(d);

    if (!compileResult.Success) return 1;

    Directory.CreateDirectory(outDir);
    foreach (var (name, bytes) in compileResult.Outputs!)
    {
        var outPath = Path.Combine(outDir, name + ".bin");
        File.WriteAllBytes(outPath, bytes);
        Console.WriteLine($"  OK  {name}  →  {Path.GetFileName(outPath)}  ({bytes.Length} bytes)");
    }

    return 0;
}

// ── helpers ──────────────────────────────────────────────────────────────────

static void PrintDiagnostics(IReadOnlyList<ParseDiagnostic> diags, string source)
{
    foreach (var d in diags)
        (d.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out)
            .WriteLine($"[{source}] {d}");
}

static int Error(string msg) { Console.Error.WriteLine(msg); return 1; }
