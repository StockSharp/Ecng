# Ecng.Net.Clients

A powerful .NET library that provides utility base classes for building REST API clients with built-in serialization, error handling, retry policies, and caching support.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Features](#core-features)
  - [REST API Client](#rest-api-client)
  - [Authentication](#authentication)
  - [Request/Response Handling](#requestresponse-handling)
  - [Retry Policies](#retry-policies)
  - [Caching](#caching)
  - [Request Logging](#request-logging)
- [Advanced Features](#advanced-features)
  - [Custom Formatters](#custom-formatters)
  - [REST Attributes](#rest-attributes)
  - [Error Handling](#error-handling)
- [Utilities](#utilities)
  - [Sitemap Generation](#sitemap-generation)
  - [Captcha Validation](#captcha-validation)
  - [SMS Service](#sms-service)
  - [Currency Converter](#currency-converter)
- [API Reference](#api-reference)

## Overview

`Ecng.Net.Clients` wraps `HttpClient` with serialization and error handling, allowing your API client classes to stay clean and concise. Instead of writing repetitive HTTP client code, you can focus on defining your API endpoints.

### Why Use This Library?

**Standard .NET Approach:**
```csharp
using var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("users/123");
response.EnsureSuccessStatusCode();
var user = JsonSerializer.Deserialize<User>(
    await response.Content.ReadAsStringAsync());
```

**Using Ecng.Net.Clients:**
```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient(string token)
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");
        AddAuthBearer(token);
    }

    public Task<User> GetUser(int id, CancellationToken cancellationToken = default)
        => GetAsync<User>(GetCurrentMethod(), cancellationToken, id);
}

// Usage
var client = new MyApiClient(token);
var user = await client.GetUser(123);
```

## Installation

Add a reference to the `Ecng.Net.Clients` project or include the NuGet package (if published):

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Ecng\Net.Clients\Net.Clients.csproj" />
</ItemGroup>
```

## Quick Start

### Creating a Basic REST API Client

```csharp
using Ecng.Net;
using System.Net.Http.Formatting;

public class GitHubApiClient : RestBaseApiClient
{
    public GitHubApiClient(string accessToken = null)
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.github.com/");

        if (!string.IsNullOrEmpty(accessToken))
            AddAuthBearer(accessToken);

        // Configure retry policy
        RetryPolicy.ReadMaxCount = 3;
        RetryPolicy.WriteMaxCount = 2;
    }

    // GET request: GET /users/{username}
    public Task<GitHubUser> GetUser(string username, CancellationToken cancellationToken = default)
        => GetAsync<GitHubUser>(GetCurrentMethod(), cancellationToken, username);

    // POST request: POST /repos/{owner}/{repo}/issues
    public Task<Issue> CreateIssue(string owner, string repo, CreateIssueRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<Issue>(GetCurrentMethod(), cancellationToken, owner, repo, request);

    // PUT request: PUT /user/starred/{owner}/{repo}
    public Task StarRepository(string owner, string repo, CancellationToken cancellationToken = default)
        => PutAsync<VoidType>(GetCurrentMethod(), cancellationToken, owner, repo);

    // DELETE request: DELETE /user/starred/{owner}/{repo}
    public Task UnstarRepository(string owner, string repo, CancellationToken cancellationToken = default)
        => DeleteAsync<VoidType>(GetCurrentMethod(), cancellationToken, owner, repo);
}

// Usage
var client = new GitHubApiClient("your_access_token");
var user = await client.GetUser("octocat");
Console.WriteLine($"Name: {user.Name}, Followers: {user.Followers}");
```

## Core Features

### REST API Client

The `RestBaseApiClient` abstract base class provides the foundation for building REST API clients.

#### Constructor Parameters

```csharp
public abstract class RestBaseApiClient(
    HttpMessageInvoker http,           // HttpClient or custom message invoker
    MediaTypeFormatter request,        // Formatter for request serialization
    MediaTypeFormatter response)       // Formatter for response deserialization
```

#### HTTP Methods

The base class provides protected methods for all standard HTTP verbs:

```csharp
// GET request
protected Task<TResult> GetAsync<TResult>(
    string methodName,
    CancellationToken cancellationToken,
    params object[] args)

// POST request
protected Task<TResult> PostAsync<TResult>(
    string methodName,
    CancellationToken cancellationToken,
    params object[] args)

// PUT request
protected Task<TResult> PutAsync<TResult>(
    string methodName,
    CancellationToken cancellationToken,
    params object[] args)

// DELETE request
protected Task<TResult> DeleteAsync<TResult>(
    string methodName,
    CancellationToken cancellationToken,
    params object[] args)
```

#### URL Construction

By default, method names are automatically converted to URL paths:
- `GetUserAsync` → `getuser`
- `CreateOrder` → `createorder`

Arguments are added as:
- **GET/DELETE**: Query string parameters
- **POST/PUT**: Request body

### Authentication

The library supports multiple authentication schemes:

#### Bearer Token Authentication

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient(string token)
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");
        AddAuthBearer(token);  // Adds "Authorization: Bearer {token}"
    }
}
```

#### Basic Authentication

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient(string username, string password)
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{username}:{password}"));
        AddAuth(AuthenticationSchemes.Basic, credentials);
    }
}
```

#### Custom Authentication

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient(string apiKey)
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");
        AddAuth("X-API-Key", apiKey);
    }
}
```

#### Per-Request Headers

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        // Headers added to every request
        PerRequestHeaders["User-Agent"] = "MyApp/1.0";
        PerRequestHeaders["Accept-Language"] = "en-US";
    }
}
```

### Request/Response Handling

#### JSON Serialization (Default)

```csharp
using System.Net.Http.Formatting;
using Newtonsoft.Json;

var formatter = new JsonMediaTypeFormatter
{
    SerializerSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatString = "yyyy-MM-dd",
        Converters = { new StringEnumConverter() }
    }
};

public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), formatter, formatter)
    {
        BaseAddress = new Uri("https://api.example.com/");
    }
}
```

#### Form URL Encoded

For APIs that expect form-urlencoded data:

```csharp
using Ecng.Net;

public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(
            new HttpClient(),
            new RestApiFormUrlEncodedMediaTypeFormatter(),  // Request
            new JsonMediaTypeFormatter())                    // Response
    {
        BaseAddress = new Uri("https://api.example.com/");
    }

    public Task<TokenResponse> GetToken(string username, string password,
        CancellationToken cancellationToken = default)
        => PostAsync<TokenResponse>(GetCurrentMethod(), cancellationToken, username, password);
}

// Sends: username=john&password=secret123
```

#### Plain Text Responses

For APIs that return plain text:

```csharp
using Ecng.Net;

public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(
            new HttpClient(),
            new JsonMediaTypeFormatter(),
            new TextMediaTypeFormatter(new[] { "text/plain", "text/html" }))
    {
        BaseAddress = new Uri("https://api.example.com/");
    }

    public Task<string> GetPlainText(CancellationToken cancellationToken = default)
        => GetAsync<string>(GetCurrentMethod(), cancellationToken);
}
```

### Retry Policies

Built-in retry mechanism with exponential backoff:

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        // Configure retry policy
        RetryPolicy.ReadMaxCount = 5;   // Retry GET requests up to 5 times
        RetryPolicy.WriteMaxCount = 2;  // Retry POST/PUT/DELETE up to 2 times

        // RetryPolicy uses exponential backoff by default
    }
}
```

The `RetryPolicyInfo` class automatically handles:
- Network failures
- Timeout exceptions
- Server errors (5xx status codes)
- Exponential backoff between retries

### Caching

Cache API responses to reduce network calls:

#### In-Memory Cache

```csharp
using Ecng.Net;

public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        // Cache GET requests for 5 minutes
        Cache = new InMemoryRestApiClientCache(TimeSpan.FromMinutes(5));
    }
}
```

#### Custom Cache Implementation

```csharp
public class RedisRestApiClientCache : IRestApiClientCache
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRestApiClientCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public bool TryGet<T>(HttpMethod method, Uri uri, object body, out T value)
    {
        var db = _redis.GetDatabase();
        var key = $"{method}:{uri}";
        var cached = db.StringGet(key);

        if (cached.HasValue)
        {
            value = JsonConvert.DeserializeObject<T>(cached);
            return true;
        }

        value = default;
        return false;
    }

    public void Set<T>(HttpMethod method, Uri uri, object body, T value)
    {
        var db = _redis.GetDatabase();
        var key = $"{method}:{uri}";
        var serialized = JsonConvert.SerializeObject(value);
        db.StringSet(key, serialized, TimeSpan.FromMinutes(10));
    }

    public void Remove(HttpMethod method = default, string uriLike = default,
        ComparisonOperator op = ComparisonOperator.Greater)
    {
        // Implementation for cache invalidation
    }
}

// Usage
var client = new MyApiClient
{
    Cache = new RedisRestApiClientCache(redisConnection)
};
```

#### Cache Invalidation

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");
        Cache = new InMemoryRestApiClientCache(TimeSpan.FromMinutes(5));
    }

    public async Task UpdateUser(int userId, UserUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await PutAsync<User>(GetCurrentMethod(), cancellationToken, userId, request);

        // Invalidate cached user data
        Cache.Remove(HttpMethod.Get, $"users/{userId}", ComparisonOperator.Equal);
    }
}
```

### Request Logging

Monitor API calls for debugging and analytics:

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        // Enable request logging
        LogRequest += (method, uri, body) =>
        {
            Console.WriteLine($"[{DateTime.Now}] {method} {uri}");
            if (body != null)
                Console.WriteLine($"Body: {JsonConvert.SerializeObject(body)}");
        };
    }
}
```

#### Performance Tracing

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        // Enable performance tracing
        Tracing = true;
    }

    protected override void TraceCall(HttpMethod method, Uri uri, TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds > 1)
            Console.WriteLine($"SLOW: {method} {uri} took {elapsed.TotalSeconds:F2}s");
    }
}
```

## Advanced Features

### Custom Formatters

Create custom formatters for specialized serialization:

```csharp
public class XmlMediaTypeFormatter : MediaTypeFormatter
{
    public XmlMediaTypeFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
    }

    public override bool CanReadType(Type type) => true;
    public override bool CanWriteType(Type type) => true;

    public override async Task<object> ReadFromStreamAsync(
        Type type, Stream readStream, HttpContent content,
        IFormatterLogger formatterLogger, CancellationToken cancellationToken)
    {
        var serializer = new XmlSerializer(type);
        return serializer.Deserialize(readStream);
    }

    public override async Task WriteToStreamAsync(
        Type type, object value, Stream writeStream,
        HttpContent content, TransportContext transportContext,
        CancellationToken cancellationToken)
    {
        var serializer = new XmlSerializer(type);
        serializer.Serialize(writeStream, value);
    }
}

// Usage
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(
            new HttpClient(),
            new XmlMediaTypeFormatter(),
            new XmlMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");
    }
}
```

### REST Attributes

Use attributes to customize endpoint names and parameter handling:

#### Custom Endpoint Names

```csharp
using Ecng.Net;

public class MyApiClient : RestBaseApiClient
{
    // Method name: GetUserProfile
    // Without attribute: GET /getuserprofile?id=123
    // With attribute: GET /users/profile?id=123
    [Rest(Name = "users/profile")]
    public Task<UserProfile> GetUserProfile(int id, CancellationToken cancellationToken = default)
        => GetAsync<UserProfile>(GetCurrentMethod(), cancellationToken, id);
}
```

#### Custom Parameter Names

```csharp
public class MyApiClient : RestBaseApiClient
{
    // Without attributes: GET /search?searchTerm=hello&pageSize=10
    // With attributes: GET /search?q=hello&limit=10
    public Task<SearchResults> Search(
        [Rest(Name = "q")] string searchTerm,
        [Rest(Name = "limit")] int pageSize,
        CancellationToken cancellationToken = default)
        => GetAsync<SearchResults>(GetCurrentMethod(), cancellationToken, searchTerm, pageSize);
}
```

#### Ignoring Parameters

```csharp
public class MyApiClient : RestBaseApiClient
{
    // The 'options' parameter is used locally but not sent to the API
    public Task<User> GetUser(
        int userId,
        [Rest(Ignore = true)] RequestOptions options,
        CancellationToken cancellationToken = default)
    {
        // Use options for client-side logic
        if (options?.UseCache == true)
            Cache = new InMemoryRestApiClientCache(TimeSpan.FromMinutes(5));

        return GetAsync<User>(GetCurrentMethod(), cancellationToken, userId);
    }
}
```

### Error Handling

#### Extracting Error Details

```csharp
public class MyApiClient : RestBaseApiClient
{
    public MyApiClient()
        : base(new HttpClient(), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
    {
        BaseAddress = new Uri("https://api.example.com/");

        // Extract detailed error messages from API responses
        ExtractBadResponse = true;
    }
}

// If API returns: {"error": "Invalid credentials", "code": 401}
// Exception message will include: "401 (Unauthorized): {\"error\":\"Invalid credentials\",\"code\":401}"
```

#### Custom Response Validation

```csharp
public class MyApiClient : RestBaseApiClient
{
    protected override async Task ValidateResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonConvert.DeserializeObject<ApiError>(errorContent);

            throw new ApiException(error.Message, error.Code, response.StatusCode);
        }
    }
}
```

#### Custom Response Processing

```csharp
public class MyApiClient : RestBaseApiClient
{
    protected override async Task<TResult> GetResultAsync<TResult>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        // Handle empty responses
        if (typeof(TResult) == typeof(VoidType))
            return default;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // Unwrap API envelope: {"success": true, "data": {...}}
        var envelope = JsonConvert.DeserializeObject<ApiEnvelope<TResult>>(content);
        return envelope.Data;
    }
}
```

## Utilities

### Sitemap Generation

Generate XML sitemaps for search engine optimization:

```csharp
using Ecng.Net.Sitemap;
using System.Xml.Linq;

// Create sitemap nodes
var nodes = new List<SitemapNode>
{
    new SitemapNode("https://example.com/")
    {
        LastModified = DateTime.UtcNow,
        Frequency = SitemapFrequency.Daily,
        Priority = 1.0
    },
    new SitemapNode("https://example.com/products")
    {
        LastModified = DateTime.UtcNow.AddDays(-1),
        Frequency = SitemapFrequency.Weekly,
        Priority = 0.8
    },
    new SitemapNode("https://example.com/about")
    {
        Frequency = SitemapFrequency.Monthly,
        Priority = 0.5
    }
};

// Generate sitemap XML
XDocument sitemap = SitemapGenerator.GenerateSitemap(nodes);
sitemap.Save("sitemap.xml");
```

#### Multilingual Sitemaps

```csharp
using Ecng.Net.Sitemap;

var node = new SitemapNode("https://example.com/products")
{
    LastModified = DateTime.UtcNow,
    Frequency = SitemapFrequency.Weekly,
    Priority = 0.8
};

// Add alternate language versions
node.AlternateLinks.Add(new XhtmlLink("https://example.com/en/products", "en"));
node.AlternateLinks.Add(new XhtmlLink("https://example.com/fr/products", "fr"));
node.AlternateLinks.Add(new XhtmlLink("https://example.com/de/products", "de"));
node.AlternateLinks.Add(new XhtmlLink("https://example.com/products", "x-default"));

var sitemap = SitemapGenerator.GenerateSitemap(new[] { node });
sitemap.Save("sitemap-multilingual.xml");
```

#### Sitemap Index

For large sites with multiple sitemaps:

```csharp
using Ecng.Net.Sitemap;

var sitemapUrls = new[]
{
    "https://example.com/sitemap-products.xml",
    "https://example.com/sitemap-blog.xml",
    "https://example.com/sitemap-pages.xml"
};

XDocument sitemapIndex = SitemapGenerator.GenerateSitemapIndex(sitemapUrls);
sitemapIndex.Save("sitemap-index.xml");
```

#### Sitemap Frequency Options

```csharp
public enum SitemapFrequency
{
    Never,    // Archived URLs that never change
    Yearly,   // Changes yearly
    Monthly,  // Changes monthly
    Weekly,   // Changes weekly
    Daily,    // Changes daily
    Hourly,   // Changes hourly
    Always    // Changes on every access
}
```

### Captcha Validation

Interface for implementing captcha validation:

```csharp
using Ecng.Net.Captcha;

public class RecaptchaValidator : ICaptchaValidator<RecaptchaResponse>
{
    private readonly string _secretKey;
    private readonly HttpClient _httpClient;

    public RecaptchaValidator(string secretKey)
    {
        _secretKey = secretKey;
        _httpClient = new HttpClient();
    }

    public async Task<RecaptchaResponse> ValidateAsync(
        string response,
        string address,
        CancellationToken cancellationToken = default)
    {
        var requestUri = "https://www.google.com/recaptcha/api/siteverify" +
            $"?secret={_secretKey}&response={response}&remoteip={address}";

        var httpResponse = await _httpClient.PostAsync(requestUri, null, cancellationToken);
        var content = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<RecaptchaResponse>(content);
    }
}

public class RecaptchaResponse
{
    public bool Success { get; set; }
    public DateTime ChallengeTs { get; set; }
    public string Hostname { get; set; }
    public string[] ErrorCodes { get; set; }
}

// Usage
var validator = new RecaptchaValidator("your-secret-key");
var result = await validator.ValidateAsync(captchaResponse, userIpAddress);

if (result.Success)
{
    // Captcha validated successfully
}
```

### SMS Service

Interface for implementing SMS messaging:

```csharp
using Ecng.Net.Sms;

public class TwilioSmsService : ISmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly HttpClient _httpClient;

    public TwilioSmsService(string accountSid, string authToken, string fromNumber)
    {
        _accountSid = accountSid;
        _authToken = authToken;
        _fromNumber = fromNumber;
        _httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<string> SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("From", _fromNumber),
            new KeyValuePair<string, string>("To", phone),
            new KeyValuePair<string, string>("Body", message)
        });

        var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

// Usage
var smsService = new TwilioSmsService("accountSid", "authToken", "+1234567890");
var result = await smsService.SendAsync("+1987654321", "Your verification code is: 123456");
```

### Currency Converter

Interface for implementing currency conversion:

```csharp
using Ecng.Net.Currencies;

public class ExchangeRateApiConverter : ICurrencyConverter
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ExchangeRateApiConverter(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.exchangerate-api.com/v4/")
        };
    }

    public async Task<decimal> GetRateAsync(
        CurrencyTypes from,
        CurrencyTypes to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"latest/{from}?apikey={_apiKey}",
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonConvert.DeserializeObject<ExchangeRateResponse>(content);

        return data.Rates[to.ToString()];
    }
}

public class ExchangeRateResponse
{
    public Dictionary<string, decimal> Rates { get; set; }
}

// Usage
var converter = new ExchangeRateApiConverter("your-api-key");
var rate = await converter.GetRateAsync(
    CurrencyTypes.USD,
    CurrencyTypes.EUR,
    DateTime.UtcNow);

decimal amountInEur = 100m * rate;
Console.WriteLine($"$100 USD = €{amountInEur:F2} EUR");
```

## API Reference

### RestBaseApiClient

Base class for creating REST API clients.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `BaseAddress` | `Uri` | The base URL for all API requests |
| `Http` | `HttpMessageInvoker` | The underlying HTTP client |
| `RequestFormatter` | `MediaTypeFormatter` | Serializer for request bodies |
| `ResponseFormatter` | `MediaTypeFormatter` | Deserializer for responses |
| `PerRequestHeaders` | `IDictionary<string, string>` | Headers added to every request |
| `Cache` | `IRestApiClientCache` | Response cache implementation |
| `RetryPolicy` | `RetryPolicyInfo` | Retry configuration |
| `ExtractBadResponse` | `bool` | Include error details in exceptions |
| `Tracing` | `bool` | Enable performance tracing |

#### Events

| Event | Description |
|-------|-------------|
| `LogRequest` | Fired before each request is sent |

#### Protected Methods

| Method | Description |
|--------|-------------|
| `GetAsync<TResult>()` | Execute HTTP GET request |
| `PostAsync<TResult>()` | Execute HTTP POST request |
| `PutAsync<TResult>()` | Execute HTTP PUT request |
| `DeleteAsync<TResult>()` | Execute HTTP DELETE request |
| `GetCurrentMethod()` | Get current method name for URL construction |
| `AddAuthBearer()` | Add Bearer token authentication |
| `AddAuth()` | Add custom authentication header |

### IRestApiClientCache

Interface for implementing response caching.

#### Methods

| Method | Description |
|--------|-------------|
| `TryGet<T>()` | Attempt to retrieve cached value |
| `Set<T>()` | Store value in cache |
| `Remove()` | Remove cached entries |

### InMemoryRestApiClientCache

In-memory cache implementation with expiration.

#### Constructor

```csharp
public InMemoryRestApiClientCache(TimeSpan timeout)
```

### RestAttribute

Attribute for customizing REST API behavior.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Custom name for endpoint or parameter |
| `IsRequired` | `bool` | Whether parameter is required |
| `Ignore` | `bool` | Exclude parameter from request |

### SitemapGenerator

Static class for generating XML sitemaps.

#### Methods

| Method | Description |
|--------|-------------|
| `GenerateSitemap()` | Create sitemap XML from nodes |
| `GenerateSitemapIndex()` | Create sitemap index XML |
| `CheckDocumentSize()` | Validate sitemap size (max 10MB) |
| `CheckSitemapCount()` | Validate sitemap count (max 50,000) |

### SitemapNode

Represents a URL in a sitemap.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Url` | `string` | The URL (required) |
| `LastModified` | `DateTime?` | Last modification date |
| `Frequency` | `SitemapFrequency?` | Change frequency hint |
| `Priority` | `double?` | Priority (0.0 to 1.0) |
| `AlternateLinks` | `XhtmlLinkCollection` | Alternate language versions |

### Media Type Formatters

#### JsonMediaTypeFormatter
Standard JSON serialization using Newtonsoft.Json (from `Microsoft.AspNet.WebApi.Client`).

#### RestApiFormUrlEncodedMediaTypeFormatter
Form URL-encoded serialization for `application/x-www-form-urlencoded` content type.

#### TextMediaTypeFormatter
Plain text serialization/deserialization for text-based responses.

## Best Practices

### 1. Use CancellationToken

Always support cancellation in your API methods:

```csharp
public Task<User> GetUser(int id, CancellationToken cancellationToken = default)
    => GetAsync<User>(GetCurrentMethod(), cancellationToken, id);
```

### 2. Configure Appropriate Retry Policies

Set different retry counts for read vs. write operations:

```csharp
public MyApiClient()
{
    RetryPolicy.ReadMaxCount = 5;   // More retries for reads
    RetryPolicy.WriteMaxCount = 1;  // Fewer retries for writes
}
```

### 3. Use Caching for Read-Heavy APIs

Enable caching for APIs with mostly GET requests:

```csharp
public MyApiClient()
{
    Cache = new InMemoryRestApiClientCache(TimeSpan.FromMinutes(5));
}
```

### 4. Handle Errors Gracefully

Enable detailed error extraction for better debugging:

```csharp
public MyApiClient()
{
    ExtractBadResponse = true;
}
```

### 5. Use Strongly-Typed DTOs

Define clear data transfer objects for requests and responses:

```csharp
public class CreateUserRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public Task<User> CreateUser(CreateUserRequest request, CancellationToken cancellationToken = default)
    => PostAsync<User>(GetCurrentMethod(), cancellationToken, request);
```

## License

This library is part of the StockSharp/Ecng project. Please refer to the project's license file for terms of use.

## Contributing

Contributions are welcome! Please ensure that your code follows the existing patterns and includes appropriate documentation.

## Support

For issues, questions, or feature requests, please use the StockSharp project's issue tracker.
