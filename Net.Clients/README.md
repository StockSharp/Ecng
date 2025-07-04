# Ecng.Net.Clients

Base classes for REST API clients.

## Purpose

Base classes for REST API clients.

## Key Features

- Simplifies HttpClient usage
- Retry policies
- Request logging

## Usage Example

```csharp
class MyClient : RestBaseApiClient {
    public Task<MyDto> Get() => GetAsync<MyDto>("item");
}
```
