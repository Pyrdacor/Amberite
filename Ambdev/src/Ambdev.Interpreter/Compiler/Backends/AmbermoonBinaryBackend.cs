namespace Ambdev.Interpreter.Compiler.Backends;

using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

/// <summary>
/// Compiles Ambdev event/chain declarations back into the Ambermoon binary event-data format.
///
/// Binary layout (Motorola big-endian throughout):
///   word              chainCount  — number of top-level chains (consecutive indices 1..N, N ≤ 64)
///   chainCount × word             — 0-based binary start-event index for each chain
///   word              eventCount  — total event slots (= highest 1-based event index)
///   eventCount × 12 bytes         — event records: [type (1 byte)] [data (9 bytes)] [next-event (2 bytes)]
///
/// Event record byte layout:
///   [0]      : event type = etype index
///   [1..9]   : data bytes (etype field offsets 0–8)
///   [10..11] : next-event index (0xFFFF = none / end of chain)
///
/// Chain-linking conventions (matching MapDecoder):
///   -  (Always  / Event N) : set prev.record[10..11]  = N−1 (binary index of event N)
///   ?  (Success / Chain X) : set branch.record[10..11] = firstEventBinaryIndex(X)
///   !  (Failure / Chain Y) : set branch.record[8..9]   = firstEventBinaryIndex(Y)
///                            (= etype data offset 7, the word at data[7..8])
///
/// Only chains with index 1–64 appear in the chain table; sub-chains (65+) are wired
/// exclusively through ? / ! link steps.
/// </summary>
public sealed class AmbermoonBinaryBackend : ICompilerBackend
{
    public string Name => "Ambermoon";

    public byte[] Compile(CompilationInput input)
    {
        // ── Pass 1: allocate and fill event records ───────────────────────────

        // EventInfo.Index is 1-based; binary index = Index − 1.
        // Allocate one slot per index up to the highest declared event index.
        int eventCount = input.Events.Count == 0 ? 0 : input.Events.Max(e => e.Index);
        var records    = new byte[eventCount][];

        for (int i = 0; i < eventCount; i++)
        {
            records[i]     = new byte[12];
            records[i][10] = 0xFF; // next = 0xFFFF  (no continuation by default)
            records[i][11] = 0xFF;
        }

        foreach (var ev in input.Events)
        {
            int bi     = ev.Index - 1;
            var record = records[bi];
            record[0]  = (byte)ev.EtypeIndex; // type byte = etype index

            if (!input.Registry.Etypes.TryGetValue(ev.EtypeIndex, out var etype))
                continue;

            // 1a. Write etype-level defaults first (optional fields with non-zero defaults)
            foreach (var field in etype.Fields)
            {
                if (field.DefaultValue.HasValue)
                    WriteField(record, field.Offset, field.DefaultValue.Value,
                               FieldSize(field, input.Registry));
            }

            // 1b. Override with explicit field values from the event declaration
            foreach (var evField in ev.Fields)
            {
                var fieldInfo = etype.Fields.FirstOrDefault(f => f.Name == evField.Name);
                if (fieldInfo is not null)
                    WriteField(record, fieldInfo.Offset, evField.Value,
                               FieldSize(fieldInfo, input.Registry));
            }
        }

        // ── Pass 2: wire events together according to chain structure ─────────

        var chainLookup = input.Chains.ToDictionary(c => c.Index);

        foreach (var chain in input.Chains)
        {
            int prevBi = -1; // binary index of the last Always/Event step seen

            foreach (var step in chain.Steps)
            {
                switch (step.Prefix, step.Target)
                {
                    case (StepPrefix.Always, StepTarget.Event):
                    {
                        int bi = step.TargetIndex - 1;
                        if (prevBi >= 0)
                            WriteNext(records[prevBi], bi);
                        prevBi = bi;
                        break;
                    }

                    case (StepPrefix.IfSuccess, StepTarget.Chain):
                    {
                        if (prevBi >= 0)
                        {
                            int first = FirstEventBinaryIndex(step.TargetIndex, chainLookup);
                            if (first >= 0)
                                WriteNext(records[prevBi], first);
                        }
                        break;
                    }

                    case (StepPrefix.IfFailure, StepTarget.Chain):
                    {
                        if (prevBi >= 0)
                        {
                            int first = FirstEventBinaryIndex(step.TargetIndex, chainLookup);
                            if (first >= 0)
                                WriteFailTarget(records[prevBi], first);
                        }
                        break;
                    }
                }
            }
        }

        // ── Serialise ─────────────────────────────────────────────────────────

        // Top-level chains: indices 1–64 only.
        var topChains = input.Chains
                             .Where(c => c.Index is >= 1 and <= 64)
                             .OrderBy(c => c.Index)
                             .ToList();

        int chainTableSize = topChains.Count == 0 ? 0 : topChains[^1].Index;

        var ms = new MemoryStream();

        WriteWord(ms, (ushort)chainTableSize);
        for (int ci = 1; ci <= chainTableSize; ci++)
        {
            var chain = topChains.FirstOrDefault(c => c.Index == ci);
            if (chain is not null)
            {
                int first = FirstEventBinaryIndex(chain.Index, chainLookup);
                WriteWord(ms, first >= 0 ? (ushort)first : (ushort)0xFFFF);
            }
            else
            {
                WriteWord(ms, 0xFFFF); // gap — empty chain placeholder
            }
        }

        WriteWord(ms, (ushort)eventCount);
        foreach (var r in records)
            ms.Write(r, 0, 12);

        return ms.ToArray();
    }

    // ── Linking helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the 0-based binary index of the first event in the given chain,
    /// or -1 if the chain doesn't exist or has no direct event steps.
    /// </summary>
    private static int FirstEventBinaryIndex(int chainIndex, Dictionary<int, ChainInfo> lookup)
    {
        if (!lookup.TryGetValue(chainIndex, out var chain)) return -1;
        var step = chain.Steps.FirstOrDefault(
            s => s.Prefix == StepPrefix.Always && s.Target == StepTarget.Event);
        return step is null ? -1 : step.TargetIndex - 1;
    }

    /// <summary>Sets record bytes [10..11] to the given binary event index (success / next).</summary>
    private static void WriteNext(byte[] record, int binaryIndex)
    {
        record[10] = (byte)(binaryIndex >> 8);
        record[11] = (byte)(binaryIndex & 0xFF);
    }

    /// <summary>
    /// Sets record bytes [8..9] to the given binary event index.
    /// These bytes correspond to etype data offset 7 (word) — the failure-branch target
    /// used by branching event types (Door, Chest, Condition, Dice100, QuestionPopup).
    /// </summary>
    private static void WriteFailTarget(byte[] record, int binaryIndex)
    {
        record[8] = (byte)(binaryIndex >> 8);
        record[9] = (byte)(binaryIndex & 0xFF);
    }

    // ── Field-data helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Writes <paramref name="value"/> at etype data <paramref name="offset"/> inside the record.
    /// Record layout: [0]=type, [1..9]=data (etype offsets 0–8), [10..11]=next.
    /// </summary>
    private static void WriteField(byte[] record, int offset, long value, int size)
    {
        int rOff = 1 + offset; // convert etype data offset to record byte index
        switch (size)
        {
            case 1:
                if (rOff < 10)
                    record[rOff] = (byte)(value & 0xFF);
                break;
            case 2:
                if (rOff + 1 < 10)
                {
                    record[rOff]     = (byte)((value >> 8) & 0xFF);
                    record[rOff + 1] = (byte)(value & 0xFF);
                }
                break;
            case 4:
                if (rOff + 3 < 10)
                {
                    record[rOff]     = (byte)((value >> 24) & 0xFF);
                    record[rOff + 1] = (byte)((value >> 16) & 0xFF);
                    record[rOff + 2] = (byte)((value >>  8) & 0xFF);
                    record[rOff + 3] = (byte)(value & 0xFF);
                }
                break;
        }
    }

    private static int FieldSize(FieldInfo field, AmbdevRegistry registry) => field.Type switch
    {
        PrimitiveTypeRef p => PrimSize(p.Type),
        NamedTypeRef n when registry.Enums.TryGetValue(n.TypeName, out var e)
            => PrimSize(e.BaseType),
        NamedTypeRef n when registry.Bitfields.TryGetValue(n.TypeName, out var b)
            => PrimSize(b.BaseType),
        _ => 1
    };

    private static int PrimSize(PrimType t) => t switch
    {
        PrimType.Word => 2,
        PrimType.Long => 4,
        _             => 1
    };

    private static void WriteWord(Stream s, ushort v)
    {
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)(v & 0xFF));
    }
}
