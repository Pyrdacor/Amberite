using Ambermoon.Aon;

namespace Ambermoon.Aon.Doc;

internal sealed class DocGenerator
{
    private readonly AodFile? _aod;
    private readonly IReadOnlyList<(string FileName, AonDocument Doc)> _inputs;

    public DocGenerator(AodFile? aod, IReadOnlyList<(string FileName, AonDocument Doc)> inputs)
    {
        _aod    = aod;
        _inputs = inputs;
    }

    public AonDoc Build()
    {
        var allInstances = _inputs
            .SelectMany(i => i.Doc.Instances.Select(inst => (i.FileName, inst)))
            .ToList();

        var tables = new List<DocTable>();

        // ── main data table ─────────────────────────────────────────────────
        bool propertyRows = _aod != null || allInstances.Count <= 1;
        tables.Add(propertyRows
            ? BuildPropertyRowTable(allInstances)
            : BuildEntityRowTable(allInstances));

        // ── enum / bitfield reference tables (only when AOD is supplied) ────
        if (_aod != null)
        {
            foreach (var def in _aod.Definitions.OfType<EnumDef>())
                tables.Add(BuildEnumTable(def));

            foreach (var def in _aod.Definitions.OfType<BitfieldDef>())
                tables.Add(BuildBitfieldTable(def));
        }

        var title = BuildTitle(allInstances);
        return new AonDoc(title, tables);
    }

    // ── property-per-row table ───────────────────────────────────────────────

    private DocTable BuildPropertyRowTable(IReadOnlyList<(string FileName, AonInstance inst)> instances)
    {
        // Determine ordered field names and optional AOD flat layout
        List<FlatField>? flatFields = null;
        if (_aod != null && instances.Count > 0)
        {
            var typeName  = instances[0].inst.TypeName;
            var structDef = _aod.Get<StructDef>(typeName);
            if (structDef != null)
                flatFields = StructFlattener.Flatten(structDef, _aod).ToList();
        }

        // Build column headers
        var headers = new List<string>();
        if (_aod != null) { headers.Add("Offset"); headers.Add("Type"); }
        headers.Add("Property");
        if (instances.Count == 1)
            headers.Add("Value");
        else
            headers.AddRange(instances.Select(i => string.IsNullOrEmpty(i.inst.Name) ? i.FileName : i.inst.Name));

        // Build rows ordered by flat field layout if available, otherwise by first instance
        var rows = new List<IReadOnlyList<Cell>>();

        if (flatFields != null)
        {
            // AOD-ordered fields (with offset + type)
            foreach (var ff in flatFields)
            {
                if (ff.FixedValue != null) continue;   // skip compile-time constants (e.g. Type = Monster)

                var row = new List<Cell>();
                row.Add(new Cell($"0x{ff.Offset:X4}", IsCode: true));
                row.Add(new Cell(ValueFormatter.FormatType(ff.Type, ff.IsOptional), IsCode: true));
                row.Add(new Cell(ff.Name));
                foreach (var (_, inst) in instances)
                {
                    var field = inst.Fields.FirstOrDefault(f => f.Name == ff.Name);
                    var text  = field != null ? ValueFormatter.Format(field.Value) : "-";
                    row.Add(new Cell(text));
                }
                rows.Add(row);
            }

            // Any instance fields not covered by flat layout (extra/unknown)
            var knownNames = flatFields.Select(f => f.Name).ToHashSet();
            var extraNames = instances
                .SelectMany(i => i.inst.Fields.Select(f => f.Name))
                .Distinct()
                .Where(n => !knownNames.Contains(n))
                .ToList();
            foreach (var name in extraNames)
                rows.Add(BuildPropertyRow(name, instances, withAod: true));
        }
        else
        {
            // No AOD or struct not found — use field order from first (or union of all) instances
            var orderedNames = FieldUnion(instances.Select(i => i.inst));
            foreach (var name in orderedNames)
                rows.Add(BuildPropertyRow(name, instances, withAod: false));
        }

        return new DocTable(
            instances.Count == 1 ? instances[0].inst.Name : null,
            headers,
            rows);
    }

    private IReadOnlyList<Cell> BuildPropertyRow(
        string name,
        IReadOnlyList<(string, AonInstance inst)> instances,
        bool withAod)
    {
        var row = new List<Cell>();
        if (withAod) { row.Add(new Cell("-")); row.Add(new Cell("-")); }
        row.Add(new Cell(name));
        foreach (var (_, inst) in instances)
        {
            var field = inst.Fields.FirstOrDefault(f => f.Name == name);
            row.Add(new Cell(field != null ? ValueFormatter.Format(field.Value) : "-"));
        }
        return row;
    }

    // ── entity-per-row table ─────────────────────────────────────────────────

    private DocTable BuildEntityRowTable(IReadOnlyList<(string FileName, AonInstance inst)> instances)
    {
        var fieldNames = FieldUnion(instances.Select(i => i.inst));

        // Use the "Name" field as the display label and remove it from the property columns
        // to avoid a redundant duplicate column.
        const string nameField = "Name";
        var propNames = fieldNames.Where(n => n != nameField).ToList();

        var headers = new List<string> { "Name" };
        headers.AddRange(propNames);

        var rows = instances.Select(pair =>
        {
            var (fileName, inst) = pair;

            // Prefer the actual "Name" field value for the label (preserves original chars)
            var nameVal = inst.Fields.FirstOrDefault(f => f.Name == nameField)?.Value;
            var label   = nameVal != null ? ValueFormatter.Format(nameVal) : (string.IsNullOrEmpty(inst.Name) ? fileName : inst.Name);

            var row = new List<Cell> { new Cell(label) };
            foreach (var name in propNames)
            {
                var field = inst.Fields.FirstOrDefault(f => f.Name == name);
                row.Add(new Cell(field != null ? ValueFormatter.Format(field.Value) : "-"));
            }
            return (IReadOnlyList<Cell>)row;
        }).ToList();

        return new DocTable(null, headers, rows);
    }

    // ── enum / bitfield reference tables ────────────────────────────────────

    private static DocTable BuildEnumTable(EnumDef def)
    {
        int next = 0;
        var rows = def.Members.Select(m =>
        {
            int v = m.ExplicitValue ?? next;
            next = v + 1;
            return (IReadOnlyList<Cell>)new List<Cell>
            {
                new Cell($"0x{v:X2}", IsCode: true),
                new Cell(v.ToString(), IsCode: true),
                new Cell(m.Name),
            };
        }).ToList();

        return new DocTable(
            $"enum {def.Name} : {ValueFormatter.FormatType(new PrimitiveTypeRef(def.UnderlyingType, null), false)}",
            ["Hex", "Dec", "Name"],
            rows);
    }

    private static DocTable BuildBitfieldTable(BitfieldDef def)
    {
        var rows = def.Flags.Select((flag, i) =>
            (IReadOnlyList<Cell>)new List<Cell>
            {
                new Cell($"0x{1 << i:X2}", IsCode: true),
                new Cell(i.ToString(), IsCode: true),
                new Cell(flag),
            }).ToList();

        return new DocTable(
            $"bitfield {def.Name} : {ValueFormatter.FormatType(new PrimitiveTypeRef(def.UnderlyingType, null), false)}",
            ["Value", "Bit", "Name"],
            rows);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    // Union of all field names in declaration order (first seen wins for ordering).
    private static List<string> FieldUnion(IEnumerable<AonInstance> instances)
    {
        var seen  = new HashSet<string>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var inst in instances)
            foreach (var field in inst.Fields)
                if (seen.Add(field.Name))
                    order.Add(field.Name);
        return order;
    }

    private static string BuildTitle(IReadOnlyList<(string FileName, AonInstance inst)> instances)
    {
        if (instances.Count == 0) return "AON Data";
        var typeName = instances[0].inst.TypeName;
        return instances.Count == 1
            ? $"{typeName}: {instances[0].inst.Name}"
            : $"{typeName} ({instances.Count} instances)";
    }
}
