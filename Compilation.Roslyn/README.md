# Ecng.Compilation.Roslyn

Compile C# code using Roslyn.

## Purpose

Compile C# code using Roslyn.

## Key Features

- Produces in-memory assemblies
- Add references dynamically
- Emit to disk or memory

## Usage Example

```csharp
var compiler = new RoslynCompiler();
var asm = compiler.CompileSource(source);
```
