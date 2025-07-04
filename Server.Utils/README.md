# Ecng.Server.Utils

Utilities for hosting background services.

## Purpose

Utilities for hosting background services.

## Key Features

- Service path helpers
- Logging integration
- Graceful shutdown
- Timers for maintenance

## Usage Example

```csharp
using var host = new DaemonHost();
await host.StartAsync();
```
