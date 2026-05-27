using Ambdev.Interpreter.Compiler;
using Ambdev.Interpreter.Compiler.Backends;
using Ambdev.Interpreter.Runtime;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ambdevc <events.aed> <map.aev> [<output.bin>]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  <events.aed>   Type definitions (etypes, enums, bitfields).");
    Console.Error.WriteLine("  <map.aev>      Event and chain declarations for one map.");
    Console.Error.WriteLine("  <output.bin>   Binary output file (default: <map>.bin).");
    return 1;
}

var aedPath = args[0];
var aevPath = args[1];
var outPath = args.Length >= 3
    ? args[2]
    : Path.ChangeExtension(Path.GetFullPath(aevPath), ".bin");

if (!File.Exists(aedPath))
{
    Console.Error.WriteLine($"File not found: {aedPath}");
    return 1;
}
if (!File.Exists(aevPath))
{
    Console.Error.WriteLine($"File not found: {aevPath}");
    return 1;
}

try
{
    var aedSource = File.ReadAllText(aedPath, System.Text.Encoding.UTF8);
    var aevSource = File.ReadAllText(aevPath, System.Text.Encoding.UTF8);

    // Only the Ambermoon backend exists for now; additional backends will be
    // selectable via a future --backend flag once they are implemented.
    ICompilerBackend backend = new AmbermoonBinaryBackend();

    var bytes = AmbdevCompiler.Compile(aedSource, aevSource, backend, aedPath, aevPath);

    File.WriteAllBytes(outPath, bytes);
    Console.WriteLine($"Compiled  {Path.GetFileName(aevPath)}");
    Console.WriteLine($"       →  {outPath}  ({bytes.Length} bytes)");
    return 0;
}
catch (AmbdevParseException ex)
{
    Console.Error.WriteLine($"Parse error: {ex.Message}");
    return 1;
}
catch (AmbdevRuntimeException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    return 1;
}
