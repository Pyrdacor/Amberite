# Amberite

Tooling and language infrastructure for the [Ambermoon](https://github.com/Pyrdacor/Ambermoon) game framework.

## Projects

| Directory | Description |
|-----------|-------------|
| [`Amber/`](Amber/) | Amber — a typed scripting language targeting Motorola 68000 conventions |
| [`Ambdev/`](Ambdev/) | Ambdev — a DSL for declaring and validating Ambermoon map event data |

Both are .NET 10 C# projects built with [ANTLR4](https://www.antlr.org/) and xUnit.

## Building

```
cd Ambdev
dotnet build

cd Amber
dotnet build
```

## Testing

```
cd Ambdev
dotnet test

cd Amber
dotnet test
```
