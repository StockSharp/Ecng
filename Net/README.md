# Ecng.Net

Core networking helpers.

## Purpose

Core networking helpers.

## Key Features

- REST utilities
- Uri helpers
- High level WebSocket client

## Usage Example

```csharp
var request = new JsonRequest("/api/data");
var result = await restClient.GetAsync<MyDto>(request);
```
