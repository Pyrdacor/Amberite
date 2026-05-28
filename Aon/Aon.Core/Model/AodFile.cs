namespace Ambermoon.Aon;

public sealed class AodFile
{
    private readonly Dictionary<string, AodDefinition> _byName;

    public IReadOnlyList<AodDefinition> Definitions { get; }

    internal AodFile(IReadOnlyList<AodDefinition> definitions)
    {
        Definitions = definitions;
        _byName = definitions.ToDictionary(d => d.Name);
    }

    public bool TryGet(string name, out AodDefinition? definition) =>
        _byName.TryGetValue(name, out definition);

    public T? Get<T>(string name) where T : AodDefinition =>
        _byName.TryGetValue(name, out var def) ? def as T : null;
}
