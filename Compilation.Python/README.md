# Ecng.Compilation.Python

Run Python scripts from .NET.

## Purpose

Run Python scripts from .NET.

## Key Features

- Easy interop with Python
- Pass arguments easily
- Capture output streams

## Usage Example

```csharp
var runner = new PythonRunner();
await runner.RunAsync("script.py", "arg1", "arg2");
```
