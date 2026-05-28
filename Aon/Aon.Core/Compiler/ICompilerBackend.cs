namespace Ambermoon.Aon;

public interface ICompilerBackend
{
    string Name { get; }

    CompileResult Compile(AodFile definitions, AonDocument instances);
}
