# Ecng.Data

Common abstractions for data access layers.

## Purpose

Common abstractions for data access layers.

## Key Features

- Reusable connection definitions
- Transaction helpers
- Connection pools

## Usage Example

```csharp
var pair = new DatabaseConnectionPair { Provider = provider, ConnectionString = connStr };
using var conn = pair.CreateConnection();
```
