namespace MapEventReader;

using System.Text;
using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

record BinaryEvent(int Index, byte Type, byte[] Data, ushort NextEventIndex);

record ChainEntry(int Index, string Name, List<ChainStepEntry> Steps);

record ChainStepEntry(char Prefix, string Target, int TargetIndex);

class MapDecoder(AmbdevRegistry registry, IReadOnlyDictionary<int, string> mapNames)
{
    // Event types that carry an alternative-branch index at etype data offset 7 (word).
    static readonly HashSet<int> BranchingTypes = [2, 3, 13, 15, 19];

    readonly AmbdevRegistry _registry = registry;
    readonly IReadOnlyDictionary<int, string> _mapNames = mapNames;
    readonly List<ChainEntry> _chains = [];
    BinaryEvent[] _events = [];
    int _nextSubIdx = 65;

    public (List<BinaryEvent> Events, List<ChainEntry> Chains) Decode(DataReader reader)
    {
        int chainCount = reader.ReadWord();
        var starts = new int[chainCount];
        for (int i = 0; i < chainCount; i++)
            starts[i] = reader.ReadWord();

        int evCount = reader.ReadWord();
        _events = new BinaryEvent[evCount];
        for (int i = 0; i < evCount; i++)
        {
            byte type = reader.ReadByte();
            byte[] data = reader.ReadBytes(9);
            ushort next = reader.ReadWord();
            _events[i] = new BinaryEvent(i, type, data, next);
        }

        for (int i = 0; i < chainCount; i++)
        {
            int chainIdx = i + 1;
            var steps = BuildSteps(starts[i], []);
            _chains.Add(new ChainEntry(chainIdx, ChainName(chainIdx, steps), steps));
        }

        return (_events.ToList(), _chains);
    }

    List<ChainStepEntry> BuildSteps(int start, HashSet<int> visited)
    {
        var steps = new List<ChainStepEntry>();
        int cur = start;

        while (cur != 0xffff && cur < _events.Length && visited.Add(cur))
        {
            var ev = _events[cur];
            steps.Add(new ChainStepEntry('-', "event", cur + 1)); // events are 1-based in .aev

            if (BranchingTypes.Contains(ev.Type))
            {
                int failIdx    = (ev.Data[7] << 8) | ev.Data[8];
                int successIdx = ev.NextEventIndex;

                // Success path: normal continuation (bytes 10-11 of 12-byte record)
                if (successIdx != 0xffff)
                {
                    int si = _nextSubIdx++;
                    var subSteps = BuildSteps(successIdx, []);
                    _chains.Add(new ChainEntry(si, ChainName(si, subSteps), subSteps));
                    steps.Add(new ChainStepEntry('?', "chain", si));
                }

                // Failure path: alternative branch (etype offset 7, data bytes 7-8)
                if (failIdx != 0xffff)
                {
                    int fi = _nextSubIdx++;
                    var failSteps = BuildSteps(failIdx, []);
                    _chains.Add(new ChainEntry(fi, ChainName(fi, failSteps), failSteps));
                    steps.Add(new ChainStepEntry('!', "chain", fi));
                }

                break;
            }

            cur = ev.NextEventIndex;
        }

        return steps;
    }

    // ── Naming ────────────────────────────────────────────────────────────────────

    // If the chain contains exactly one always-event step that has a semantic name, use it.
    string ChainName(int chainIdx, List<ChainStepEntry> steps)
    {
        if (steps.Count == 1 && steps[0].Prefix == '-' && steps[0].Target == "event")
        {
            int binaryIdx = steps[0].TargetIndex - 1;
            if (binaryIdx >= 0 && binaryIdx < _events.Length)
            {
                var semantic = SemanticEventName(_events[binaryIdx]);
                if (semantic is not null) return semantic;
            }
        }
        return chainIdx <= 64 ? $"Chain_{chainIdx}" : $"SubChain_{chainIdx}";
    }

    // Returns a semantic name for events that reference a map; null for everything else.
    string? SemanticEventName(BinaryEvent ev) => ev.Type switch
    {
        1  => TeleportName(ev),
        10 => ChangeTileName(ev),
        22 => SpawnName(ev),
        _  => null
    };

    string EventName(BinaryEvent ev) =>
        SemanticEventName(ev) ?? $"Event_{ev.Index + 1}";

    string TeleportName(BinaryEvent ev)
    {
        int transitionType = ev.Data[4];
        int mapIdx         = (ev.Data[5] << 8) | ev.Data[6];

        string prefix = transitionType switch
        {
            0 => "Map_change_to",
            1 => "Teleporter_to",
            2 => "WindGate_to",
            3 => "Levitate_up_to",
            4 => "Outro_to",
            5 => "Falling_down_to",
            _ => "Teleport_to"
        };

        return $"{prefix}_{MapIdent(mapIdx)}";
    }

    string ChangeTileName(BinaryEvent ev)
    {
        int mapIdx = (ev.Data[7] << 8) | ev.Data[8];
        return $"Change_tile_on_{MapIdent(mapIdx)}";
    }

    string SpawnName(BinaryEvent ev)
    {
        int mapIdx = (ev.Data[5] << 8) | ev.Data[6];
        return $"Spawn_on_{MapIdent(mapIdx)}";
    }

    string MapIdent(int mapIdx)
    {
        if (mapIdx == 0) return "same_map";
        return _mapNames.TryGetValue(mapIdx, out var name) ? ToIdent(name) : $"Map_{mapIdx}";
    }

    static string ToIdent(string name)
    {
        // Transliterate German special characters before stripping non-ASCII
        name = name
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
            .Replace("Ä", "AE").Replace("Ö", "OE").Replace("Ü", "UE")
            .Replace("ß", "ss");

        var sb = new StringBuilder();
        bool lastWasUnderscore = false;
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore && sb.Length > 0)
            {
                sb.Append('_');
                lastWasUnderscore = true;
            }
        }
        while (sb.Length > 0 && sb[^1] == '_') sb.Length--;

        var result = sb.ToString();
        if (result.Length > 0 && char.IsDigit(result[0])) result = "_" + result;
        return result.Length > 0 ? result : "unknown";
    }

    // ── Output generation ─────────────────────────────────────────────────────────

    public string GenerateAev(List<BinaryEvent> events, List<ChainEntry> chains)
    {
        var sb = new StringBuilder();

        foreach (var ev in events)
        {
            int evNum = ev.Index + 1;
            if (_registry.Etypes.TryGetValue(ev.Type, out var etype))
            {
                sb.Append($"event[{evNum}, {EventName(ev)}] = etype[{ev.Type}]");
                if (etype.Fields.Count > 0)
                {
                    sb.AppendLine();
                    foreach (var field in etype.Fields)
                    {
                        long val = ReadField(ev.Data, field);
                        sb.AppendLine($"    - {field.Name}: {FormatValue(val, field)}");
                    }
                }
                else
                {
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine($"// event at binary index {ev.Index}: unknown type {ev.Type}");
                sb.AppendLine($"// raw data: {string.Join(" ", ev.Data.Select(b => $"{b:X2}"))}  next=0x{ev.NextEventIndex:X4}");
            }
            sb.AppendLine();
        }

        foreach (var chain in chains.OrderBy(c => c.Index))
        {
            sb.AppendLine($"chain[{chain.Index}, {chain.Name}]");
            foreach (var step in chain.Steps)
                sb.AppendLine($"    {step.Prefix} {step.Target} {step.TargetIndex}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    long ReadField(byte[] data, FieldInfo field)
    {
        int off  = field.Offset;
        int size = FieldSize(field);
        if (off >= data.Length) return 0;
        return size switch
        {
            2 => off + 1 < data.Length ? (long)((data[off] << 8) | data[off + 1]) : data[off],
            4 => off + 3 < data.Length
                     ? ((long)data[off] << 24) | ((long)data[off+1] << 16) | ((long)data[off+2] << 8) | data[off+3]
                     : data[off],
            _ => data[off]
        };
    }

    int FieldSize(FieldInfo field) => field.Type switch
    {
        PrimitiveTypeRef p => PrimSize(p.Type),
        NamedTypeRef n =>
            _registry.Enums.TryGetValue(n.TypeName, out var e) ? PrimSize(e.BaseType) :
            _registry.Bitfields.TryGetValue(n.TypeName, out var b) ? PrimSize(b.BaseType) : 1,
        _ => 1
    };

    static int PrimSize(PrimType t) => t switch { PrimType.Word => 2, PrimType.Long => 4, _ => 1 };

    string FormatValue(long value, FieldInfo field)
    {
        if (field.Type is NamedTypeRef named)
        {
            if (_registry.Enums.TryGetValue(named.TypeName, out var e))
            {
                var m = e.Members.FirstOrDefault(x => x.Value == value);
                if (m.Name is not null) return $"{named.TypeName}.{m.Name}";
            }
            else if (_registry.Bitfields.TryGetValue(named.TypeName, out var b))
            {
                if (value == 0)
                {
                    var none = b.Members.FirstOrDefault(x => x.Value == 0);
                    return none.Name is not null ? $"{named.TypeName}.{none.Name}" : "0";
                }
                var parts = new List<string>();
                long remaining = value;
                foreach (var m in b.Members.Where(m => m.Value != 0).OrderByDescending(m => m.Value))
                {
                    if ((remaining & m.Value) == m.Value)
                    {
                        parts.Add($"{named.TypeName}.{m.Name}");
                        remaining &= ~m.Value;
                    }
                }
                if (remaining != 0) parts.Add(remaining.ToString());
                if (parts.Count > 0) return string.Join(" | ", parts);
            }
        }

        return value switch
        {
            0xff       => "0xff",
            0xffff     => "0xffff",
            0xffffffff => "0xffffffff",
            _          => value.ToString()
        };
    }
}
