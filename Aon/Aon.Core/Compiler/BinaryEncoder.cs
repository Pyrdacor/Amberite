namespace Ambermoon.Aon;

internal static class BinaryEncoder
{
    public static void Encode(AonValue value, TypeRef typeRef, byte[] buf, int offset, AodFile defs)
    {
        switch (typeRef)
        {
            // ubyte[N] / sbyte[N] can receive a string value → null-terminated byte array
            case PrimitiveTypeRef p when p.ArraySize.HasValue
                && value is AonStringValue sv
                && (p.Type is PrimitiveType.UByte or PrimitiveType.SByte):
                EncodeString(sv.Value, buf, offset, p.ArraySize.Value);
                break;

            case PrimitiveTypeRef p when p.ArraySize.HasValue:
                EncodeArray(value, new PrimitiveTypeRef(p.Type, null), buf, offset, defs, p.ArraySize.Value);
                break;
            case PrimitiveTypeRef p:
                EncodePrimitive(value, p.Type, buf, offset, defs);
                break;
            case NamedTypeRef n when n.ArraySize.HasValue:
                EncodeArray(value, new NamedTypeRef(n.TypeName, null), buf, offset, defs, n.ArraySize.Value);
                break;
            case NamedTypeRef n:
                EncodeNamed(value, n.TypeName, buf, offset, defs);
                break;
        }
    }

    // ── array ────────────────────────────────────────────────────────────────

    private static void EncodeArray(AonValue value, TypeRef elemType, byte[] buf, int offset, AodFile defs, int count)
    {
        if (value is not AonArrayValue arr)
            throw new CompileException($"Expected array value, got {value.GetType().Name}");

        int elemSize = SizeCalculator.TypeSize(elemType, defs);
        for (int i = 0; i < Math.Min(arr.Items.Count, count); i++)
            Encode(arr.Items[i], elemType, buf, offset + i * elemSize, defs);
    }

    // ── named type (enum / bitfield / struct) ────────────────────────────────

    private static void EncodeNamed(AonValue value, string typeName, byte[] buf, int offset, AodFile defs)
    {
        var def = defs.Get<AodDefinition>(typeName)
            ?? throw new CompileException($"Unknown type '{typeName}'");

        switch (def)
        {
            case EnumDef e:
                EncodePrimitive(value, e.UnderlyingType, buf, offset, defs);
                break;
            case BitfieldDef b:
                EncodePrimitive(value, b.UnderlyingType, buf, offset, defs);
                break;
            case StructDef s:
                EncodeNestedStruct(value, s, buf, offset, defs);
                break;
        }
    }

    // ── primitive ────────────────────────────────────────────────────────────

    private static void EncodePrimitive(AonValue value, PrimitiveType type, byte[] buf, int offset, AodFile defs)
    {
        long v = ResolveNumeric(value, defs);
        switch (type)
        {
            case PrimitiveType.UByte:
            case PrimitiveType.SByte:
                buf[offset] = (byte)(v & 0xFF);
                break;
            case PrimitiveType.UWord:
            case PrimitiveType.SWord:
                buf[offset]     = (byte)((v >> 8) & 0xFF);
                buf[offset + 1] = (byte)(v & 0xFF);
                break;
            case PrimitiveType.UDWord:
            case PrimitiveType.SDWord:
                buf[offset]     = (byte)((v >> 24) & 0xFF);
                buf[offset + 1] = (byte)((v >> 16) & 0xFF);
                buf[offset + 2] = (byte)((v >>  8) & 0xFF);
                buf[offset + 3] = (byte)(v & 0xFF);
                break;
        }
    }

    // ── nested struct ────────────────────────────────────────────────────────

    private static void EncodeNestedStruct(AonValue value, StructDef structDef, byte[] buf, int baseOffset, AodFile defs)
    {
        if (value is not AonObjectValue obj)
            throw new CompileException($"Expected object value for struct '{structDef.Name}', got {value.GetType().Name}");

        var fieldMap = obj.Fields.ToDictionary(f => f.Name, f => f.Value);
        var flatFields = StructFlattener.Flatten(structDef, defs);

        foreach (var flat in flatFields)
        {
            var resolved = ResolveFieldValue(flat, fieldMap, structDef.Name);
            if (resolved == null) continue;
            Encode(resolved, flat.Type, buf, baseOffset + flat.Offset, defs);
        }
    }

    // ── numeric resolution ───────────────────────────────────────────────────

    internal static long ResolveNumeric(AonValue value, AodFile defs) =>
        value switch
        {
            AonIntValue i    => i.Value,
            AonHexValue h    => h.Value,
            AonFlagsValue f  => ResolveNumeric(f.Left, defs) | ResolveNumeric(f.Right, defs),
            AonRefValue r    => ResolveRef(r.QualifiedName, defs),
            _ => throw new CompileException($"Cannot encode {value.GetType().Name} as a numeric value"),
        };

    private static long ResolveRef(string qualifiedName, AodFile defs)
    {
        var dot = qualifiedName.IndexOf('.');
        if (dot < 0)
        {
            if (long.TryParse(qualifiedName, out long n)) return n;
            throw new CompileException($"Cannot resolve '{qualifiedName}' as a value");
        }

        var typeName  = qualifiedName[..dot];
        var memberName = qualifiedName[(dot + 1)..];

        if (defs.Get<EnumDef>(typeName) is { } e)
        {
            int next = 0;
            foreach (var m in e.Members)
            {
                int v = m.ExplicitValue ?? next;
                next = v + 1;
                if (m.Name == memberName) return v;
            }
            throw new CompileException($"Enum '{typeName}' has no member '{memberName}'");
        }

        if (defs.Get<BitfieldDef>(typeName) is { } b)
        {
            for (int i = 0; i < b.Flags.Count; i++)
                if (b.Flags[i] == memberName) return 1L << i;
            throw new CompileException($"Bitfield '{typeName}' has no flag '{memberName}'");
        }

        throw new CompileException($"'{typeName}' is not an enum or bitfield");
    }

    // ── string encoding ──────────────────────────────────────────────────────

    private static void EncodeString(string value, byte[] buf, int offset, int maxLen)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var enc   = System.Text.Encoding.GetEncoding(850);
        var bytes = enc.GetBytes(value);
        int len   = Math.Min(bytes.Length, maxLen - 1);   // leave room for null terminator
        Array.Copy(bytes, 0, buf, offset, len);
        // buf[offset + len] is already 0 (CLR zero-fills arrays)
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    internal static AonValue? ResolveFieldValue(
        FlatField flat,
        IReadOnlyDictionary<string, AonValue> instanceFields,
        string contextName)
    {
        if (flat.FixedValue != null)
            return flat.FixedValue;

        if (instanceFields.TryGetValue(flat.Name, out var v))
            return v;

        if (flat.IsOptional)
        {
            if (flat.DefaultValue == null) return null;   // leave zeroed
            return flat.DefaultValue switch
            {
                AodIntDefault i => new AonIntValue(i.Value),
                AodRefDefault r => new AonRefValue(r.QualifiedName),
                _ => null,
            };
        }

        throw new CompileException($"[{contextName}] Missing required field '{flat.Name}'");
    }
}
