# Ecng.Logging

Flexible logging framework.

## Purpose

Flexible logging framework.

## Key Features

- Multiple listeners
- Severity filters
- File and console outputs

## Usage Example

```csharp
var manager = new LogManager();
manager.Listeners.Add(new ConsoleLogListener());
var log = manager.Application;
log.Info("Started");
```
