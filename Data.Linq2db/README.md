# Ecng.Data.Linq2db

Helpers for linq2db.

## Purpose

Helpers for linq2db.

## Key Features

- Simplifies DB connections
- Reuse mapping configurations
- Async query helpers

## Usage Example

```csharp
var db = new LinqToDbService(options);
var list = await db.Query<MyEntity>().ToListAsync();
```
