using Ambdev.Interpreter.Runtime;

internal static class Fixture
{
    internal static AmbdevInterpreter Run(string source)
        => AmbdevRunner.Execute(source, "<test>");

    internal static EtypeInfo  Etype(this AmbdevInterpreter i, int    index) => i.Registry.GetEtype(index);
    internal static EspecInfo  Espec(this AmbdevInterpreter i, string name)  => i.Registry.GetEspec(name);
    internal static EventInfo  Event(this AmbdevInterpreter i, int    index) => i.Registry.GetEvent(index);
    internal static ChainInfo  Chain(this AmbdevInterpreter i, int    index) => i.Registry.GetChain(index);
}
