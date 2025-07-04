# Ecng.Net.Clients

Utility base classes for building REST API clients.

## Purpose

Wraps `HttpClient` with serialization and error handling so API classes stay concise.

## Key Features

- Simplifies `HttpClient` usage
- Retry policies with backoff
- Automatic serialization
- Request logging

## Standard .NET

```csharp
using var client = new HttpClient { BaseAddress = new Uri("https://api/") };
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var resp = await client.GetAsync("item");
resp.EnsureSuccessStatusCode();
var dto = JsonSerializer.Deserialize<MyDto>(
    await resp.Content.ReadAsStringAsync());
```

## Using Ecng

```csharp
class MyClient(string token) : RestBaseApiClient(new HttpClient(),
    new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
{
    public MyClient() { AddAuthBearer(token); BaseAddress = new("https://api/"); }

    public Task<MyDto> Get() => GetAsync<MyDto>("item");
}
```
