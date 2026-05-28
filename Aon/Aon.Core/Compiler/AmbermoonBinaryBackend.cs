namespace Ambermoon.Aon;

/// <summary>
/// Compiles AON instances to big-endian binary blobs compatible with
/// Ambermoon's internal file formats.  Gaps between defined fields and any
/// trailing bytes up to the struct's declared (or computed) size are zero-filled.
/// </summary>
public sealed class AmbermoonBinaryBackend : ICompilerBackend
{
    public string Name => "Ambermoon";

    public CompileResult Compile(AodFile definitions, AonDocument instances)
    {
        var outputs     = new Dictionary<string, byte[]>();
        var diagnostics = new List<CompileDiagnostic>();

        foreach (var instance in instances.Instances)
        {
            if (definitions.Get<StructDef>(instance.TypeName) is not { } structDef)
            {
                diagnostics.Add(new CompileDiagnostic(DiagnosticSeverity.Error,
                    instance.Name, $"Unknown type '{instance.TypeName}'"));
                continue;
            }

            try
            {
                outputs[instance.Name] = EncodeInstance(instance, structDef, definitions, diagnostics);
            }
            catch (CompileException ex)
            {
                diagnostics.Add(new CompileDiagnostic(DiagnosticSeverity.Error,
                    instance.Name, ex.Message));
            }
        }

        return new CompileResult(outputs, diagnostics);
    }

    // ────────────────────────────────────────────────────────────────────────

    private static byte[] EncodeInstance(
        AonInstance instance,
        StructDef structDef,
        AodFile defs,
        List<CompileDiagnostic> diagnostics)
    {
        var flatFields   = StructFlattener.Flatten(structDef, defs);
        var instanceMap  = instance.Fields.ToDictionary(f => f.Name, f => f.Value);

        // Compute actual end-of-data offset from field layout
        int dataSize = flatFields.Count > 0
            ? flatFields.Max(f => f.Offset + SizeCalculator.TypeSize(f.Type, defs))
            : 0;

        // Buffer size: whichever is larger — the declared struct size or the data
        int bufferSize = structDef.DeclaredSizeInBytes.HasValue
            ? Math.Max(structDef.DeclaredSizeInBytes.Value, dataSize)
            : dataSize;

        // Validate: fields must not exceed the declared size
        if (structDef.DeclaredSizeInBytes.HasValue && dataSize > structDef.DeclaredSizeInBytes.Value)
        {
            diagnostics.Add(new CompileDiagnostic(DiagnosticSeverity.Error,
                instance.Name,
                $"Fields require {dataSize} bytes but struct '{structDef.Name}' " +
                $"declares [Size = {structDef.DeclaredSizeInBytes.Value}]"));
        }

        var buf = new byte[bufferSize];   // zero-filled by CLR

        foreach (var flat in flatFields)
        {
            AonValue? value;
            try
            {
                value = BinaryEncoder.ResolveFieldValue(flat, instanceMap, instance.Name);
            }
            catch (CompileException ex)
            {
                diagnostics.Add(new CompileDiagnostic(DiagnosticSeverity.Error,
                    instance.Name, ex.Message));
                continue;
            }

            if (value == null) continue;   // optional with no default → leave zeroed

            try
            {
                BinaryEncoder.Encode(value, flat.Type, buf, flat.Offset, defs);
            }
            catch (CompileException ex)
            {
                diagnostics.Add(new CompileDiagnostic(DiagnosticSeverity.Error,
                    instance.Name, $"Field '{flat.Name}': {ex.Message}"));
            }
        }

        return buf;
    }
}
