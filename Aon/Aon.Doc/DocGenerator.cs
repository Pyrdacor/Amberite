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

        bool propertyRows = _aod != null || allInstances.Count <= 1;
        tables.Add(propertyRows
            ? BuildPropertyRowTable(allInstances)
            : BuildEntityRowTable(allInstances));

        // Enum / bitfield reference tables — only when AOD is supplied
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
        List<FlatField>? flatFields = null;
        if (_aod != null && instances.Count > 0)
        {
            var typeName  = instances[0].inst.TypeName;
            var structDef = _aod.Get<StructDef>(typeName);
            if (structDef != null)
                flatFields = StructFlattener.Flatten(structDef, _aod).ToList();
        }

        var headers = new List<string>();
        if (_aod != null) { headers.Add("Offset"); headers.Add("Type"); }
        headers.Add("Property");
        if (instances.Count == 1)
            headers.Add("Value");
        else
            headers.AddRange(instances.Select(i => string.IsNullOrEmpty(i.inst.Name) ? i.FileName : i.inst.Name));

        var rows = new List<IReadOnlyList<Cell>>();

        if (flatFields != null)
        {
            foreach (var ff in flatFields)
            {
                if (ff.FixedValue != null) continue;   // skip compile-time constants
                AppendFieldRows(rows, ff.Name, ff, instances);
            }

            // Any instance fields not in the flat layout
            var knownNames = flatFields.Select(f => f.Name).ToHashSet();
            foreach (var name in FieldUnion(instances.Select(i => i.inst)).Where(n => !knownNames.Contains(n)))
                AppendFieldRows(rows, name, null, instances);
        }
        else
        {
            foreach (var name in FieldUnion(instances.Select(i => i.inst)))
                AppendFieldRows(rows, name, null, instances);
        }

        return new DocTable(
            instances.Count == 1 ? instances[0].inst.Name : null,
            headers,
            rows);
    }

    // Appends one or more rows for a single named field.
    // Arrays whose elements are objects (structs) are expanded to one row per element.
    private void AppendFieldRows(
        List<IReadOnlyList<Cell>> rows,
        string fieldName,
        FlatField? ff,
        IReadOnlyList<(string, AonInstance inst)> instances)
    {
        bool hasAod = ff != null;
        var values  = instances.Select(p => p.inst.Fields.FirstOrDefault(f => f.Name == fieldName)?.Value).ToList();

        if (!values.Any(IsExpandableArray))
        {
            // Single row
            var row = new List<Cell>();
            if (hasAod)
            {
                row.Add(new Cell($"0x{ff!.Offset:X4}", IsCode: true));
                row.Add(new Cell(ValueFormatter.FormatType(ff.Type, ff.IsOptional), IsCode: true));
            }
            row.Add(new Cell(fieldName));
            foreach (var v in values)
                row.Add(new Cell(v != null ? ValueFormatter.Format(v) : "-"));
            rows.Add(row);
            return;
        }

        // Expandable array: parent header row + one child row per element
        int maxLen = values.Max(v => v is AonArrayValue arr ? arr.Items.Count : 0);

        var parentRow = new List<Cell>();
        if (hasAod)
        {
            parentRow.Add(new Cell($"0x{ff!.Offset:X4}", IsCode: true));
            parentRow.Add(new Cell(ValueFormatter.FormatType(ff.Type, ff.IsOptional), IsCode: true));
        }
        parentRow.Add(new Cell(fieldName));
        foreach (var _ in instances) parentRow.Add(new Cell(""));
        rows.Add(parentRow);

        // Derive element type and per-element byte size for offset calculation
        TypeRef? elemType = ff?.Type switch
        {
            PrimitiveTypeRef p => new PrimitiveTypeRef(p.Type, null),
            NamedTypeRef n     => new NamedTypeRef(n.TypeName, null),
            _                  => null,
        };
        int elemSize = elemType != null && _aod != null ? SizeCalculator.TypeSize(elemType, _aod) : 0;

        for (int i = 0; i < maxLen; i++)
        {
            var child = new List<Cell>();
            if (hasAod)
            {
                int childOffset = ff!.Offset + i * elemSize;
                child.Add(elemSize > 0 ? new Cell($"0x{childOffset:X4}", IsCode: true) : new Cell(""));
                child.Add(elemType  != null ? new Cell(ValueFormatter.FormatType(elemType, false), IsCode: true) : new Cell(""));
            }
            child.Add(new Cell($"{fieldName}[{i}]"));
            foreach (var v in values)
            {
                var elem = v is AonArrayValue arr && i < arr.Items.Count
                    ? ValueFormatter.Format(arr.Items[i], depth: 0)
                    : "-";
                child.Add(new Cell(elem));
            }
            rows.Add(child);
        }
    }

    // ── entity-per-row table ─────────────────────────────────────────────────

    private DocTable BuildEntityRowTable(IReadOnlyList<(string FileName, AonInstance inst)> instances)
    {
        var cols     = BuildColumnSpecs(instances.Select(i => i.inst));
        var propCols = cols.Where(c => c.FieldName != "Name").ToList();

        var headers = new List<string> { "Name" };
        headers.AddRange(propCols.Select(c => c.Header));

        var rows = instances.Select(pair =>
        {
            var (fileName, inst) = pair;
            var nameVal = inst.Fields.FirstOrDefault(f => f.Name == "Name")?.Value;
            var label   = nameVal != null
                ? ValueFormatter.Format(nameVal)
                : (string.IsNullOrEmpty(inst.Name) ? fileName : inst.Name);

            var row = new List<Cell> { new Cell(label) };
            foreach (var col in propCols)
            {
                var field = inst.Fields.FirstOrDefault(f => f.Name == col.FieldName);
                string text;
                if (col.ArrayIndex >= 0)
                {
                    var arr = field?.Value as AonArrayValue;
                    text = arr != null && col.ArrayIndex < arr.Items.Count
                        ? ValueFormatter.Format(arr.Items[col.ArrayIndex], depth: 0)
                        : "-";
                }
                else
                {
                    text = field != null ? ValueFormatter.Format(field.Value) : "-";
                }
                row.Add(new Cell(text));
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

    // An array is "expandable" when all its items are objects (structs).
    // Primitive arrays (bytes, ints) stay inline.
    private static bool IsExpandableArray(AonValue? value) =>
        value is AonArrayValue arr && arr.Items.Count > 0 && arr.Items[0] is AonObjectValue;

    // Column specification for entity-rows mode.
    private readonly record struct ColSpec(string Header, string FieldName, int ArrayIndex);

    // Produces one ColSpec per logical column. Object-array fields are fanned out to
    // FieldName[0]..FieldName[N-1]; everything else stays as a single column.
    private static List<ColSpec> BuildColumnSpecs(IEnumerable<AonInstance> instances)
    {
        var maxLen  = new Dictionary<string, int>(StringComparer.Ordinal);
        var order   = new List<string>();

        foreach (var inst in instances)
            foreach (var field in inst.Fields)
            {
                int len = IsExpandableArray(field.Value)
                    ? ((AonArrayValue)field.Value).Items.Count
                    : 0;
                if (maxLen.TryGetValue(field.Name, out int cur))
                    maxLen[field.Name] = Math.Max(cur, len);
                else { maxLen[field.Name] = len; order.Add(field.Name); }
            }

        var specs = new List<ColSpec>();
        foreach (var name in order)
        {
            int n = maxLen[name];
            if (n > 0)
                for (int i = 0; i < n; i++)
                    specs.Add(new ColSpec($"{name}[{i}]", name, i));
            else
                specs.Add(new ColSpec(name, name, -1));
        }
        return specs;
    }

    // Ordered union of all field names across instances.
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
