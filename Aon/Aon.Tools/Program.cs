using Ambermoon.Aon.Tools;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: aon-tools chars <source-dir> <output-dir>");
    return 1;
}

if (args[0] != "chars")
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    return 1;
}

var sourceDir = args[1];
var outputDir = args.Length >= 3 ? args[2] : args[1];

if (!Directory.Exists(sourceDir))
{
    Console.Error.WriteLine($"Source directory not found: {sourceDir}");
    return 1;
}

Directory.CreateDirectory(outputDir);

var files = Directory.GetFiles(sourceDir)
    .OrderBy(f => f)
    .ToArray();

int ok = 0, errors = 0;

foreach (var filePath in files)
{
    var fileName = Path.GetFileName(filePath);
    var outPath  = Path.Combine(outputDir, fileName + ".aon");

    try
    {
        var data = File.ReadAllBytes(filePath);
        if (data.Length < 0x0122)
        {
            Console.Error.WriteLine($"  SKIP  {fileName}  (too small: {data.Length} bytes)");
            continue;
        }

        var charData = CharacterReader.Read(data);
        var aon      = CharacterAonWriter.Write(charData, fileName);
        File.WriteAllText(outPath, aon, System.Text.Encoding.UTF8);

        Console.WriteLine($"  OK    {fileName}  →  {Path.GetFileName(outPath)}  ({charData.Name})");
        ok++;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR {fileName}: {ex.Message}");
        errors++;
    }
}

Console.WriteLine();
Console.WriteLine($"Done: {ok} converted, {errors} errors.");
return errors > 0 ? 1 : 0;
