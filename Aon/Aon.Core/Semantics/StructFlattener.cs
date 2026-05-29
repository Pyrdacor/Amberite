namespace Ambermoon.Aon;

public static class StructFlattener
{
    public static IReadOnlyList<FlatField> Flatten(StructDef structDef, AodFile file)
    {
        // offset → FlatField (ordered insertion via SortedDictionary)
        var byOffset  = new SortedDictionary<int, FlatField>();
        var byName    = new Dictionary<string, int>();   // name → offset

        // Merge base struct first
        if (structDef.BaseType != null &&
            file.Get<StructDef>(structDef.BaseType) is { } baseStruct)
        {
            foreach (var f in Flatten(baseStruct, file))
            {
                byOffset[f.Offset] = f;
                byName[f.Name]     = f.Offset;
            }
        }

        int cursor = 0;

        foreach (var member in structDef.Members)
        {
            switch (member)
            {
                case OffsetDirective od:
                    cursor = od.Offset;
                    break;

                case FieldFixup ff:
                    if (byName.TryGetValue(ff.FieldName, out int fixedOffset))
                    {
                        var existing = byOffset[fixedOffset];
                        byOffset[fixedOffset] = existing with
                        {
                            FixedValue = new AonRefValue(ff.Value),
                        };
                    }
                    break;

                case FieldDecl fd:
                    var flat = new FlatField(cursor, fd.Type, fd.Name,
                        fd.IsOptional, fd.DefaultValue, null);

                    byOffset[cursor] = flat;
                    byName[fd.Name]  = cursor;
                    cursor += SizeCalculator.TypeSize(fd.Type, file);
                    break;
            }
        }

        return byOffset.Values.ToList();
    }
}
