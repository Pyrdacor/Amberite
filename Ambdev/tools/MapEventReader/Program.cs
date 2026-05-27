using Ambdev.Interpreter.Runtime;
using MapEventReader;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: mapevreader <mapfile> <events.aed>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  mapfile    Binary map file (e.g. 000)");
    Console.Error.WriteLine("  events.aed Ambermoon event definition file");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Output: map_events.aev in the current directory");
    return 1;
}

var mapPath = args[0];
var aedPath = args[1];

if (!File.Exists(mapPath)) { Console.Error.WriteLine($"Map file not found: {mapPath}"); return 1; }
if (!File.Exists(aedPath)) { Console.Error.WriteLine($"AED file not found: {aedPath}"); return 1; }

try
{
    var mapData = File.ReadAllBytes(mapPath);
    var aedSource = File.ReadAllText(aedPath);

    var registry = AmbdevRunner.Execute(aedSource, aedPath).Registry;

    // Load map names: look alongside the .aed file, next to the executable, then current directory
    var mapNamesPath =
        new[]
        {
            Path.Combine(Path.GetDirectoryName(Path.GetFullPath(aedPath))!, "map_names.txt"),
            Path.Combine(AppContext.BaseDirectory, "map_names.txt"),
            "map_names.txt"
        }
        .FirstOrDefault(File.Exists) ?? "map_names.txt";
    var mapNames = new Dictionary<int, string>();
    if (File.Exists(mapNamesPath))
    {
        foreach (var line in File.ReadLines(mapNamesPath))
        {
            var tab = line.IndexOf('\t');
            if (tab > 0 && int.TryParse(line.AsSpan(0, tab), out int id))
                mapNames[id] = line[(tab + 1)..];
        }
        Console.WriteLine($"Loaded {mapNames.Count} map names from {mapNamesPath}");
    }

    var reader = new DataReader(mapData);
    reader.Position = 2;
    bool is3D = reader.ReadByte() == 1;
    reader.Position++;
    int mapWidth = reader.ReadByte();
    int mapHeight = reader.ReadByte();
    int bytesPerTile = is3D ? 2 : 4;
    reader.Position = 12 + 320 + mapWidth * mapHeight * bytesPerTile;

    var decoder = new MapDecoder(registry, mapNames);
    var (events, chains) = decoder.Decode(reader);

    var output = decoder.GenerateAev(events, chains);
    const string outPath = "map_events.aev";
    File.WriteAllText(outPath, output);

    int mainChains = chains.Count(c => c.Index <= 64);
    int subChains  = chains.Count(c => c.Index > 64);
    Console.WriteLine($"Written {outPath}: {events.Count} events, {mainChains} chains, {subChains} sub-chains");
    return 0;
}
catch (AmbdevParseException ex) { Console.Error.WriteLine($"AED parse error: {ex.Message}"); return 1; }
catch (AmbdevRuntimeException ex) { Console.Error.WriteLine($"AED error: {ex.Message}"); return 1; }
catch (IndexOutOfRangeException) { Console.Error.WriteLine("Error: unexpected end of map data — check offset"); return 1; }
catch (Exception ex) { Console.Error.WriteLine($"Error: {ex.Message}"); return 1; }
