namespace Ambermoon.Aon;

public sealed class AonDocument
{
    private readonly Dictionary<string, AonInstance> _byName;

    public IReadOnlyList<AonInstance> Instances { get; }

    internal AonDocument(IReadOnlyList<AonInstance> instances)
    {
        Instances = instances;
        _byName = instances.ToDictionary(i => i.Name);
    }

    public bool TryGet(string name, out AonInstance? instance) =>
        _byName.TryGetValue(name, out instance);
}
