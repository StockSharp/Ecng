# Ecng.Compilation

Dynamic code compilation infrastructure for C#, F#, and Python. Compile code at runtime, manage references, and cache assemblies.

## Overview

This library provides a unified interface for runtime code compilation supporting multiple languages through specialized providers.

## Core Types

### ICompiler Interface

The main abstraction for compilers.

```csharp
public interface ICompiler
{
    string Extension { get; }           // e.g., ".cs", ".fs", ".py"
    bool IsAssemblyPersistable { get; } // Can save compiled assembly
    bool IsTabsSupported { get; }       // Language supports tabs
    bool IsCaseSensitive { get; }       // Language is case-sensitive
    bool IsReferencesSupported { get; } // Supports external references

    ICompilerContext CreateContext();

    Task<CompilationResult> Compile(
        string name,
        IEnumerable<string> sources,
        IEnumerable<(string name, byte[] body)> refs,
        CancellationToken ct = default);

    Task<CompilationError[]> Analyse(
        object analyzer,
        IEnumerable<object> analyzerSettings,
        string name,
        IEnumerable<string> sources,
        IEnumerable<(string name, byte[] body)> refs,
        CancellationToken ct = default);
}
```

### CompilationResult

Result of a compilation operation.

```csharp
var result = await compiler.Compile("MyCode", sources, references);

if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Type}: {error.Message}");
        Console.WriteLine($"  Line {error.Line}, Column {error.Column}");
    }
}
else
{
    // Get compiled assembly
    Assembly assembly = result.Assembly;

    // Or get as bytes for storage
    byte[] assemblyBytes = result.AssemblyBytes;
}
```

### CompilationError

Represents a compilation error or warning.

```csharp
public class CompilationError
{
    public CompilationErrorTypes Type { get; }  // Error, Warning, Info
    public string Id { get; }                    // e.g., "CS0001"
    public string Message { get; }
    public int Line { get; }
    public int Column { get; }
}
```

## Reference Types

### AssemblyReference

Reference to a .NET assembly.

```csharp
// From file path
var fileRef = new AssemblyReference("path/to/assembly.dll");

// From loaded assembly
var loadedRef = new AssemblyReference(typeof(SomeClass).Assembly);
```

### NuGetReference

Reference to a NuGet package.

```csharp
var nugetRef = new NuGetReference
{
    PackageId = "Newtonsoft.Json",
    Version = "13.0.1"
};
```

## Compiler Provider

Manages available compilers and provides access to them.

```csharp
// Get registered compilers
IEnumerable<ICompiler> compilers = CompilerProvider.Compilers;

// Get compiler by extension
ICompiler csCompiler = CompilerProvider.GetCompiler(".cs");
ICompiler fsCompiler = CompilerProvider.GetCompiler(".fs");
ICompiler pyCompiler = CompilerProvider.GetCompiler(".py");
```

## Compiler Implementations

| Package | Language | Extension |
|---------|----------|-----------|
| Ecng.Compilation.Roslyn | C# | .cs |
| Ecng.Compilation.FSharp | F# | .fs |
| Ecng.Compilation.Python | Python | .py |

## Expression Formulas

Parse and evaluate mathematical expressions at runtime.

```csharp
using Ecng.Compilation.Expressions;

// Parse expression
var formula = ExpressionHelper.Parse("(a + b) * 2");

// Get variables used
IEnumerable<string> vars = formula.Variables; // ["a", "b"]

// Evaluate with values
var values = new Dictionary<string, decimal>
{
    ["a"] = 10,
    ["b"] = 5
};
decimal result = formula.Calculate(values); // 30
```

### Supported Operations

- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Comparison: `<`, `>`, `<=`, `>=`, `==`, `!=`
- Logical: `&&`, `||`, `!`
- Functions: `abs`, `sqrt`, `min`, `max`, `pow`, etc.

## Assembly Load Context

Manage assembly loading for isolation.

```csharp
using Ecng.Compilation;

// Track loaded assemblies
var tracker = new AssemblyLoadContextTracker();

// Load assembly in isolated context
var context = tracker.CreateContext("MyPlugin");
Assembly asm = context.LoadFromAssemblyPath("plugin.dll");

// Unload when done
tracker.Unload("MyPlugin");
```

## Compiler Cache

Cache compiled assemblies for reuse.

```csharp
public interface ICompilerCache
{
    bool TryGet(string key, out byte[] assembly);
    void Set(string key, byte[] assembly);
    void Remove(string key);
    void Clear();
}
```

## Usage Examples

### Compile C# Code

```csharp
var compiler = CompilerProvider.GetCompiler(".cs");

string code = @"
public class Calculator
{
    public int Add(int a, int b) => a + b;
}";

var result = await compiler.Compile(
    name: "Calculator",
    sources: new[] { code },
    refs: Array.Empty<(string, byte[])>());

if (!result.HasErrors)
{
    var type = result.Assembly.GetType("Calculator");
    var instance = Activator.CreateInstance(type);
    var method = type.GetMethod("Add");
    int sum = (int)method.Invoke(instance, new object[] { 2, 3 });
    Console.WriteLine(sum); // 5
}
```

### Compile with References

```csharp
var refs = new List<(string, byte[])>();

// Add reference
var asmBytes = File.ReadAllBytes("MyLibrary.dll");
refs.Add(("MyLibrary.dll", asmBytes));

var result = await compiler.Compile("MyCode", sources, refs);
```

## NuGet

```
Install-Package Ecng.Compilation
Install-Package Ecng.Compilation.Roslyn   # For C# support
Install-Package Ecng.Compilation.FSharp   # For F# support
Install-Package Ecng.Compilation.Python   # For Python support
```
