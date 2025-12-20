# Ecng.Net

A comprehensive networking library providing essential utilities for working with HTTP, URLs, sockets, multicast, email, and retry policies in .NET applications.

## Overview

Ecng.Net is a core networking helper library that simplifies common networking tasks including URL manipulation, HTTP client operations, socket management, multicast operations, email handling, and network path validation. The library is built on .NET Standard 2.0 and supports modern .NET versions including .NET 6.0 and .NET 10.0.

## Installation

Add a reference to the `Ecng.Net` project or NuGet package in your application.

## Key Features

- **URL Manipulation**: Advanced URL parsing and query string management
- **HTTP Utilities**: Headers, authentication schemas, and client extensions
- **Socket Operations**: Enhanced socket methods including multicast support
- **Network Helpers**: IP address validation, endpoint utilities, and path detection
- **Email Utilities**: Email validation and mail message helpers
- **Retry Policies**: Configurable retry logic with exponential backoff
- **OAuth Support**: OAuth token management interfaces

## Core Components

### 1. URL and Query String Management

The `Url` and `QueryString` classes provide powerful URL manipulation capabilities with fluent APIs.

#### Basic URL Usage

```csharp
using Ecng.Net;

// Create a URL
var url = new Url("https://api.example.com/data");

// Create URL with base and relative parts
var apiUrl = new Url("https://api.example.com", "/v1/users");

// Clone URL with modifications
var modifiedUrl = url.Clone();
modifiedUrl.Encode = UrlEncodes.Upper; // Use uppercase encoding
```

#### Query String Operations

```csharp
// Working with query strings
var url = new Url("https://api.example.com/search?q=test");

// Access query string
var queryString = url.QueryString;

// Add parameters
queryString.Append("page", 1)
           .Append("limit", 50)
           .Append("sort", "date");

// Get values
int page = queryString.GetValue<int>("page");
string sort = queryString.TryGetValue<string>("sort", "default");

// Check if parameter exists
if (queryString.Contains("q"))
{
    string query = queryString.GetValue<string>("q");
}

// Remove parameters
queryString.Remove("sort");

// Clear all parameters
queryString.Clear();

// Iterate over parameters
foreach (var kvp in queryString)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// Get raw query string
string rawQuery = queryString.Raw; // Returns "page=1&limit=50"
```

#### URL Encoding Options

```csharp
var url = new Url("https://example.com/api");

// Control encoding behavior
url.Encode = UrlEncodes.None;   // No encoding
url.Encode = UrlEncodes.Lower;  // Lowercase encoding (default)
url.Encode = UrlEncodes.Upper;  // Uppercase encoding

// Keep default page in URL
url.KeepDefaultPage = true;
```

### 2. Network Helper Extensions

The `NetworkHelper` class provides numerous extension methods for common networking tasks.

#### Endpoint Operations

```csharp
using System.Net;
using Ecng.Net;

// Check if endpoint is local
var endpoint = new IPEndPoint(IPAddress.Loopback, 8080);
bool isLocal = endpoint.IsLocal(); // true

var dnsEndpoint = new DnsEndPoint("localhost", 8080);
bool isDnsLocal = dnsEndpoint.IsLocal(); // true

// Check if endpoint IP is local
bool isLocalIp = endpoint.IsLocalIpAddress();

// Check if IP is loopback
IPAddress address = IPAddress.Parse("127.0.0.1");
bool isLoopback = address.IsLoopback(); // true
```

#### TCP Client Extensions

```csharp
using System.Net.Sockets;
using Ecng.Net;

var client = new TcpClient();
var endpoint = new DnsEndPoint("api.example.com", 443);

// Async connection with cancellation token
await client.ConnectAsync(endpoint, cancellationToken);
```

#### SSL/TLS Stream Creation

```csharp
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using Ecng.Net;

Stream networkStream = client.GetStream();

// Convert to SSL stream
var sslStream = networkStream.ToSsl(
    sslProtocol: SslProtocols.Tls12 | SslProtocols.Tls13,
    checkCertificateRevocation: true,
    validateRemoteCertificates: true,
    targetHost: "api.example.com",
    sslCertificate: null, // Optional client certificate path
    sslCertificatePassword: null
);
```

#### URL Encoding and Decoding

```csharp
using Ecng.Net;

// URL encoding
string encoded = "Hello World!".EncodeUrl(); // "Hello+World%21"
string encodedUpper = "test@example".EncodeUrlUpper(); // Uses uppercase hex

// URL decoding
string decoded = "Hello+World%21".DecodeUrl(); // "Hello World!"

// HTML encoding
string htmlEncoded = "<div>Test</div>".EncodeToHtml(); // "&lt;div&gt;Test&lt;/div&gt;"
string htmlDecoded = htmlEncoded.DecodeFromHtml(); // "<div>Test</div>"

// Parse query strings
var parsed = "key1=value1&key2=value2".ParseUrl();

// XML escaping
string xmlSafe = "<tag>value</tag>".XmlEscape(); // "&lt;tag&gt;value&lt;/tag&gt;"

// URL-safe character checking
bool isSafe = '!'.IsUrlSafeChar(); // true
bool isNotSafe = '%'.IsUrlSafeChar(); // false
```

#### Encoding Extraction

```csharp
using System.Text;
using Ecng.Net;

// Extract encoding from Content-Type header
string contentType = "text/html; charset=utf-8";
Encoding encoding = contentType.TryExtractEncoding(); // Returns UTF8 encoding

string noCharset = "application/json";
Encoding fallback = noCharset.TryExtractEncoding(); // Returns null
```

#### Query String Helpers

```csharp
using Ecng.Net;

// Convert dictionary to query string
var parameters = new Dictionary<string, string>
{
    ["api_key"] = "12345",
    ["format"] = "json"
};
string queryString = parameters.ToQueryString(); // "api_key=12345&format=json"

// With URL encoding
var sensitiveParams = new Dictionary<string, string>
{
    ["email"] = "user@example.com",
    ["redirect"] = "https://example.com/callback"
};
string encoded = sensitiveParams.ToQueryString(encodeValue: true);

// Using tuples
var tupleParams = new[]
{
    ("name", "John Doe"),
    ("age", "30")
};
string fromTuples = tupleParams.ToQueryString(); // "name=John Doe&age=30"
```

### 3. Multicast Operations

The library provides enhanced multicast support for UDP sockets.

#### Basic Multicast

```csharp
using System.Net;
using System.Net.Sockets;
using Ecng.Net;

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
var multicastAddress = IPAddress.Parse("239.255.0.1");

// Join multicast group
socket.JoinMulticast(multicastAddress);

// Leave multicast group
socket.LeaveMulticast(multicastAddress);
```

#### Source-Specific Multicast

```csharp
using Ecng.Net;

// Configure source-specific multicast
var multicastConfig = new MulticastSourceAddress
{
    GroupAddress = IPAddress.Parse("239.255.0.1"),
    SourceAddress = IPAddress.Parse("192.168.1.100"),
    Port = 5000,
    IsEnabled = true
};

// Join with source filter
socket.JoinMulticast(multicastConfig);

// Leave source-specific multicast
socket.LeaveMulticast(multicastConfig);
```

### 4. HTTP Client Extensions

Convenient extensions for working with HttpClient.

#### User Agent Configuration

```csharp
using System.Net.Http;
using Ecng.Net;

var httpClient = new HttpClient();

// Apply Chrome user agent
httpClient.ApplyChromeAgent();
// Sets: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...
```

#### Bearer Token Authentication

```csharp
using System.Security;
using Ecng.Net;

var httpClient = new HttpClient();
var token = new SecureString();
// ... populate token

// Set bearer authentication
httpClient.SetBearer(token);
// Adds header: Authorization: Bearer {token}
```

#### HTTP Headers Constants

```csharp
using Ecng.Net;

// Use predefined header constants
httpClient.DefaultRequestHeaders.Add(HttpHeaders.UserAgent, "MyApp/1.0");
httpClient.DefaultRequestHeaders.Add(HttpHeaders.AcceptEncoding, "gzip, deflate");
httpClient.DefaultRequestHeaders.Add(HttpHeaders.CacheControl, "no-cache");

// Available constants:
// - Authorization
// - AcceptEncoding
// - AcceptLanguage
// - CacheControl
// - Connection
// - KeepAlive
// - LastModified
// - ProxyAuthenticate
// - ProxyAuthorization
// - ProxyConnection
// - UserAgent
// - Referer
// - WWWAuthenticate
```

### 5. Authentication Schemas

Helper methods for formatting authentication headers.

```csharp
using System.Security;
using Ecng.Net;

// Bearer authentication
var bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
string authHeader = AuthSchemas.Bearer.FormatAuth(bearerToken);
// Returns: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

// Basic authentication
var credentials = "username:password".UTF8().Base64();
string basicAuth = AuthSchemas.Basic.FormatAuth(credentials);
// Returns: "Basic dXNlcm5hbWU6cGFzc3dvcmQ="

// With SecureString
var secureToken = new SecureString();
// ... populate secure token
string secureAuth = AuthSchemas.Bearer.FormatAuth(secureToken);
```

### 6. OAuth Support

Interfaces for implementing OAuth authentication flows.

```csharp
using Ecng.Net;

// Implement OAuth provider
public class MyOAuthProvider : IOAuthProvider
{
    public async Task<IOAuthToken> RequestToken(
        long socialId,
        bool isDemo,
        CancellationToken cancellationToken)
    {
        // Request token from OAuth server
        var token = await GetOAuthTokenAsync(socialId, isDemo);

        return new OAuthToken
        {
            Value = token.AccessToken,
            Expires = DateTime.UtcNow.AddSeconds(token.ExpiresIn)
        };
    }
}

// OAuth token implementation
public class OAuthToken : IOAuthToken
{
    public string Value { get; set; }
    public DateTime? Expires { get; set; }
}
```

### 7. Retry Policy with Exponential Backoff

Configure and execute retry logic for network operations.

```csharp
using System.Net.Sockets;
using Ecng.Net;

// Configure retry policy
var retryPolicy = new RetryPolicyInfo
{
    ReadMaxCount = 5,        // Max retry attempts for reads
    WriteMaxCount = 3,       // Max retry attempts for writes
    InitialDelay = TimeSpan.FromSeconds(1),    // Initial delay
    MaxDelay = TimeSpan.FromSeconds(30)        // Maximum delay cap
};

// Configure which socket errors to retry
retryPolicy.Track.Clear();
retryPolicy.Track.Add(SocketError.TimedOut);
retryPolicy.Track.Add(SocketError.HostNotFound);
retryPolicy.Track.Add(SocketError.ConnectionRefused);

// Execute with retry logic
var result = await retryPolicy.TryRepeat(
    async (ct) =>
    {
        // Your async operation
        return await httpClient.GetStringAsync("https://api.example.com/data", ct);
    },
    maxCount: retryPolicy.ReadMaxCount,
    cancellationToken
);

// Retry delays use exponential backoff with jitter:
// Attempt 1: ~1s + jitter
// Attempt 2: ~2s + jitter
// Attempt 3: ~4s + jitter
// Attempt 4: ~8s + jitter
// Attempt 5: ~16s + jitter (capped at MaxDelay)
```

#### Manual Delay Calculation

```csharp
var policy = new RetryPolicyInfo();

// Calculate delay for specific attempt
TimeSpan delay1 = policy.GetDelay(1); // ~1 second + jitter
TimeSpan delay2 = policy.GetDelay(2); // ~2 seconds + jitter
TimeSpan delay3 = policy.GetDelay(3); // ~4 seconds + jitter
```

### 8. Email Utilities

Helpers for working with email messages and SMTP.

#### Email Validation

```csharp
using Ecng.Net;

// Validate email format
bool isValid = "user@example.com".IsEmailValid(); // true
bool isInvalid = "invalid.email".IsEmailValid();   // false
```

#### Email Message Extensions

```csharp
using System.Net.Mail;
using Ecng.Net;

var message = new MailMessage
{
    From = new MailAddress("sender@example.com"),
    Subject = "Test Email",
    Body = "Plain text body"
};

message.To.Add("recipient@example.com");

// Add HTML body
message.AddHtml("<html><body><h1>Hello!</h1></body></html>");

// Add plain text alternative
message.AddPlain("Hello in plain text!");

// Attach files
using var fileStream = File.OpenRead("document.pdf");
message.Attach("document.pdf", fileStream);

// Send asynchronously (Note: Method is marked obsolete, use SmtpClient.SendMailAsync directly)
await message.SendAsync(cancellationToken);
```

#### Creating Attachments

```csharp
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using Ecng.Net;

// Create attachment from stream
using var stream = new MemoryStream(fileBytes);
var attachment = MailHelper.ToAttachment("report.pdf", stream);

// Create attachment with custom encoding
var customAttachment = MailHelper.CreateAttachment(
    stream,
    "report.pdf",
    TransferEncoding.Base64
);

// Attach to message
message.Attachments.Add(attachment);
```

### 9. HTTP Status Code Utilities

Extract and work with HTTP status codes from exceptions.

```csharp
using System.Net;
using System.Net.Http;
using Ecng.Net;

try
{
    await httpClient.GetAsync("https://api.example.com/resource");
}
catch (HttpRequestException ex)
{
    // Try to extract status code
    HttpStatusCode? statusCode = ex.TryGetStatusCode();

    if (statusCode == HttpStatusCode.NotFound)
    {
        Console.WriteLine("Resource not found");
    }
    else if (statusCode == HttpStatusCode.Unauthorized)
    {
        Console.WriteLine("Authentication required");
    }
}

// Create custom HTTP exceptions
var notFoundException = HttpStatusCode.NotFound.CreateHttpRequestException(
    "The requested resource was not found"
);

var unauthorizedException = HttpStatusCode.Unauthorized.CreateHttpRequestException(
    "Invalid API key"
);

// Customize status code phrases for better detection
NetworkHelper.SetStatusCodePhrase(HttpStatusCode.TooManyRequests, "rate limit");
```

### 10. Socket Error Handling

Extract socket errors from exceptions.

```csharp
using System.Net.Sockets;
using Ecng.Net;

try
{
    // Network operation
    await socket.ConnectAsync(endpoint);
}
catch (Exception ex)
{
    // Extract socket error from exception chain
    SocketError? socketError = ex.TryGetSocketError();

    if (socketError == SocketError.TimedOut)
    {
        Console.WriteLine("Connection timed out");
    }
    else if (socketError == SocketError.ConnectionRefused)
    {
        Console.WriteLine("Connection refused by server");
    }
}
```

### 11. IP Address and Subnet Operations

Work with IP addresses and subnet calculations.

```csharp
using System.Net;
using Ecng.Net;

var ipAddress = IPAddress.Parse("192.168.1.100");

// Check if IP is in subnet
bool isInSubnet = ipAddress.IsInSubnet("192.168.1.0/24"); // true
bool notInSubnet = ipAddress.IsInSubnet("10.0.0.0/8");    // false

// Works with IPv6
var ipv6 = IPAddress.Parse("2001:db8::1");
bool isInV6Subnet = ipv6.IsInSubnet("2001:db8::/32"); // true
```

### 12. Network Path Detection

Detect various types of network paths and addresses.

```csharp
using Ecng.Net;

// UNC path detection
bool isUnc1 = @"\\server\share\file.txt".IsUncPath(); // true
bool isUnc2 = "//server/share/file.txt".IsUncPath();  // true

// URL path detection
bool isUrl1 = "https://example.com/path".IsUrlPath(); // true
bool isUrl2 = "http://example.com".IsUrlPath();       // true
bool isUrl3 = "ftp://ftp.example.com".IsUrlPath();    // true

// File URI detection
bool isFileUri = "file:///C:/path/to/file.txt".IsFileUriPath(); // true

// WebDAV path detection
bool isWebDav1 = "dav://server/path".IsWebDavPath();   // true
bool isWebDav2 = "davs://server/path".IsWebDavPath();  // true (secure)

// Host:port address detection
bool isHostPort1 = "127.0.0.1:8080".IsHostPortAddress();     // true
bool isHostPort2 = "localhost:5000".IsHostPortAddress();     // true
bool notHostPort = "C:\\folder".IsHostPortAddress();         // false (Windows path)

// General network path detection (checks all above)
bool isNetwork1 = @"\\server\share".IsNetworkPath();     // true
bool isNetwork2 = "https://example.com".IsNetworkPath(); // true
bool isNetwork3 = "localhost:8080".IsNetworkPath();      // true
bool isLocal = "C:\\local\\file.txt".IsNetworkPath();    // false
```

### 13. Image File Detection

Detect image files by extension.

```csharp
using Ecng.Net;

// Check if file is an image
bool isPng = "photo.png".IsImage();     // true
bool isJpg = "picture.jpg".IsImage();   // true
bool isSvg = "icon.svg".IsImage();      // true
bool isWebP = "image.webp".IsImage();   // true
bool notImage = "document.pdf".IsImage(); // false

// Supported formats: .png, .jpg, .jpeg, .bmp, .gif, .svg,
//                   .webp, .ico, .tiff, .avif, .apng

// Check if file is a vector image
bool isVector = "icon.svg".IsImageVector();  // true
bool notVector = "photo.jpg".IsImageVector(); // false
```

### 14. URL Content Detection

Detect URLs in text content.

```csharp
using Ecng.Net;

// Check if string contains URL patterns
bool hasUrl1 = "Check out https://example.com".CheckContainsUrl(); // true
bool hasUrl2 = "Visit http://site.com".CheckContainsUrl();         // true
bool hasUrl3 = "Link: ftp://files.com".CheckContainsUrl();        // true
bool hasUrl4 = "Click href=page.html".CheckContainsUrl();         // true
bool noUrl = "No URLs here".CheckContainsUrl();                    // false

// Check if Uri is localhost
var localhostUri = new Uri("http://localhost:8080");
bool isLocalhost = localhostUri.IsLocalhost(); // true

var remoteUri = new Uri("https://example.com");
bool notLocalhost = remoteUri.IsLocalhost(); // false
```

### 15. Gravatar Support

Generate Gravatar URLs from email addresses.

```csharp
using Ecng.Net;

string email = "user@example.com";

// Get Gravatar token (MD5 hash of email)
string token = email.GetGravatarToken();
// Returns: "b58996c504c5638798eb6b511e6f49af"

// Get full Gravatar URL
string gravatarUrl = token.GetGravatarUrl(size: 200);
// Returns: "https://www.gravatar.com/avatar/b58996c504c5638798eb6b511e6f49af?size=200"

// Complete flow
string avatarUrl = email.GetGravatarToken().GetGravatarUrl(80);
```

### 16. URL Safety and Cleaning

Clean and validate URLs for safety.

```csharp
using Ecng.Net;

// Check URL safety and clean
string unsafeUrl = "Тест URL with спец!@#chars%";
string safeUrl = unsafeUrl.CheckUrl(
    latin: true,   // Convert to Latin characters
    screen: true,  // Apply light screening
    clear: true    // Remove unsafe characters
);

// Individual operations
string cleared = "url%with*unsafe+chars".ClearUrl();
// Removes unsafe URL characters

// Convert to uppercase URL encoding
string upperEncoded = "test@example".UrlEncodeToUpperCase();
```

## Advanced Examples

### Complete HTTP Client with Retry Policy

```csharp
using System.Net.Http;
using System.Net.Sockets;
using Ecng.Net;

public class ResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly RetryPolicyInfo _retryPolicy;

    public ResilientHttpClient()
    {
        _httpClient = new HttpClient();
        _httpClient.ApplyChromeAgent();

        _retryPolicy = new RetryPolicyInfo
        {
            ReadMaxCount = 5,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        _retryPolicy.Track.Add(SocketError.TimedOut);
        _retryPolicy.Track.Add(SocketError.ConnectionReset);
    }

    public async Task<string> GetWithRetryAsync(
        string url,
        CancellationToken ct = default)
    {
        return await _retryPolicy.TryRepeat(
            async (token) =>
            {
                var response = await _httpClient.GetAsync(url, token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            },
            maxCount: _retryPolicy.ReadMaxCount,
            cancellationToken: ct
        );
    }
}

// Usage
var client = new ResilientHttpClient();
var data = await client.GetWithRetryAsync("https://api.example.com/data");
```

### Building API URLs with Query Parameters

```csharp
using Ecng.Net;

public class ApiClient
{
    private readonly Url _baseUrl;

    public ApiClient(string apiBaseUrl)
    {
        _baseUrl = new Url(apiBaseUrl);
    }

    public string BuildSearchUrl(string query, int page, int pageSize, string sortBy)
    {
        var url = _baseUrl.Clone();
        url.QueryString
            .Append("q", query)
            .Append("page", page)
            .Append("pageSize", pageSize)
            .Append("sort", sortBy);

        return url.ToString();
    }

    public string BuildFilteredUrl(Dictionary<string, object> filters)
    {
        var url = _baseUrl.Clone();

        foreach (var filter in filters)
        {
            url.QueryString.Append(filter.Key, filter.Value);
        }

        return url.ToString();
    }
}

// Usage
var client = new ApiClient("https://api.example.com/search");
var searchUrl = client.BuildSearchUrl("stocks", 1, 20, "date");
// Returns: https://api.example.com/search?page=1&pageSize=20&q=stocks&sort=date
```

### Multicast UDP Receiver

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using Ecng.Net;

public class MulticastReceiver : IDisposable
{
    private readonly Socket _socket;
    private readonly MulticastSourceAddress _config;

    public MulticastReceiver(string groupAddress, string sourceAddress, int port)
    {
        _config = new MulticastSourceAddress
        {
            GroupAddress = IPAddress.Parse(groupAddress),
            SourceAddress = IPAddress.Parse(sourceAddress),
            Port = port,
            IsEnabled = true
        };

        _socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );

        _socket.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true
        );

        _socket.Bind(new IPEndPoint(IPAddress.Any, _config.Port));
        _socket.JoinMulticast(_config);
    }

    public async Task<string> ReceiveAsync(CancellationToken ct = default)
    {
        var buffer = new byte[NetworkHelper.MtuSize];
        var received = await _socket.ReceiveAsync(buffer, SocketFlags.None, ct);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }

    public void Dispose()
    {
        if (_config.IsEnabled)
        {
            _socket.LeaveMulticast(_config);
        }
        _socket.Dispose();
    }
}

// Usage
using var receiver = new MulticastReceiver("239.255.0.1", "192.168.1.100", 5000);
var message = await receiver.ReceiveAsync(cancellationToken);
```

## Constants

### Maximum Transmission Unit (MTU)

```csharp
using Ecng.Net;

// Standard MTU size for network operations
int mtuSize = NetworkHelper.MtuSize; // 1600 bytes
```

## Best Practices

1. **URL Encoding**: Always encode user input when building URLs to prevent injection attacks.

2. **Retry Policies**: Configure retry policies based on your network reliability requirements. Use exponential backoff to avoid overwhelming servers.

3. **Cancellation Tokens**: Always pass cancellation tokens to async operations for proper cleanup.

4. **Resource Disposal**: Dispose of sockets, HTTP clients, and mail messages properly using `using` statements.

5. **Security**: Use `SecureString` for sensitive tokens and credentials when possible.

6. **Error Handling**: Always catch and handle network exceptions appropriately. Use `TryGetSocketError` and `TryGetStatusCode` for detailed error information.

7. **SSL/TLS**: Always validate certificates in production environments. Only disable validation for testing purposes.

8. **Multicast**: Remember to leave multicast groups when done to free up system resources.

## Dependencies

- .NET Standard 2.0+
- Ecng.ComponentModel
- Newtonsoft.Json

## Thread Safety

Most classes in this library are not thread-safe by default. When using from multiple threads:
- Use proper synchronization for shared instances
- Consider using thread-safe collections like `SynchronizedSet` where available
- Create separate instances per thread when possible

## Performance Considerations

- **Query String**: Query strings are compiled and cached. Modifications trigger recompilation.
- **Retry Policy**: Uses jitter to prevent thundering herd problems.
- **URL Encoding**: Encoding operations allocate new strings. Cache results when possible.
- **Socket Operations**: Reuse sockets when possible to avoid connection overhead.

## Platform Support

- .NET Standard 2.0
- .NET 6.0
- .NET 10.0
- Cross-platform (Windows, Linux, macOS)

## License

Part of the StockSharp/Ecng project. Please refer to the main project license for details.

## Related Libraries

- **Ecng.Common**: Core utilities and extensions
- **Ecng.Collections**: Thread-safe collections
- **Ecng.ComponentModel**: Component model utilities

## Contributing

This library is part of the Ecng ecosystem. For contributions, please refer to the main project guidelines.

## Support

For issues, questions, or contributions related to Ecng.Net, please visit the main project repository.
