# Ecng.Nuget

Helpers for interacting with NuGet feeds.

## Purpose

Helpers for interacting with NuGet feeds.

## Key Features

- Automates package publishing
- Handles API keys
- Query package info

## Usage Example

```csharp
var client = new NugetClient("https://api.nuget.org/v3/index.json");
await client.PushAsync(nupkg, apiKey);
```
