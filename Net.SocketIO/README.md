# Ecng.Net.SocketIO

A high-performance .NET client library for WebSocket communication with automatic reconnection, message resending, and comprehensive connection state management.

## Features

- **WebSocket Client Implementation** - Full-featured WebSocket client with async/await support
- **Automatic Reconnection** - Configurable reconnection attempts with exponential backoff
- **Command Resending** - Automatic resend of commands after reconnection
- **Connection State Tracking** - Track and aggregate connection states across multiple connections
- **JSON Serialization** - Built-in JSON serialization for object messages
- **Flexible Logging** - Customizable logging hooks for info, error, and verbose messages
- **RestSharp Integration** - Helper methods for REST API calls with authentication
- **Thread-Safe** - Safe to use in multi-threaded environments

## Installation

Add the package reference to your project:

```xml
<PackageReference Include="Ecng.Net.SocketIO" Version="x.x.x" />
```

## Quick Start

### Basic WebSocket Connection

```csharp
using Ecng.Net;

var socket = new WebSocketClient(
    url: "wss://example.com/socket",
    stateChanged: state => Console.WriteLine($"State: {state}"),
    error: ex => Console.WriteLine($"Error: {ex.Message}"),
    process: (msg, ct) =>
    {
        Console.WriteLine($"Received: {msg.AsString()}");
        return ValueTask.CompletedTask;
    },
    infoLog: (fmt, args) => Console.WriteLine(fmt, args),
    errorLog: (fmt, args) => Console.Error.WriteLine(fmt, args),
    verboseLog: (fmt, args) => Console.WriteLine($"[VERBOSE] {fmt}", args)
);

await socket.ConnectAsync(CancellationToken.None);
```

### Sending Messages

```csharp
// Send JSON object
await socket.SendAsync(new { type = "subscribe", channel = "trades" });

// Send string
await socket.SendAsync("Hello, WebSocket!");

// Send raw bytes
byte[] data = Encoding.UTF8.GetBytes("Raw message");
await socket.SendAsync(data, WebSocketMessageType.Text);
```

## Core Components

### WebSocketClient

The main class for WebSocket communication.

#### Constructor Parameters

```csharp
public WebSocketClient(
    string url,                              // WebSocket URL (ws:// or wss://)
    Action<ConnectionStates> stateChanged,   // Connection state change callback
    Action<Exception> error,                 // Error handler
    Func<WebSocketMessage, CancellationToken, ValueTask> process,  // Message processor
    Action<string, object> infoLog,          // Info log handler
    Action<string, object> errorLog,         // Error log handler
    Action<string, object> verboseLog        // Verbose log handler (can be null)
)
```

#### Configuration Properties

```csharp
// Reconnection settings
socket.ReconnectAttempts = 10;              // -1 for infinite, 0 for no reconnect
socket.ReconnectInterval = TimeSpan.FromSeconds(5);
socket.ResendInterval = TimeSpan.FromSeconds(2);
socket.ResendTimeout = TimeSpan.FromMilliseconds(500);

// Message encoding
socket.Encoding = Encoding.UTF8;

// Buffer sizes
socket.BufferSize = 1024 * 1024;            // 1MB for compressed data
socket.BufferSizeUncompress = 10 * 1024 * 1024; // 10MB for uncompressed

// JSON serialization
socket.Indent = true;
socket.SendSettings = new JsonSerializerSettings { ... };

// Auto-resend control
socket.DisableAutoResend = false;
```

#### Connection Management

```csharp
// Connect
await socket.ConnectAsync(cancellationToken);
socket.Connect(); // Synchronous version

// Disconnect
socket.Disconnect();

// Check connection state
bool isConnected = socket.IsConnected;
ConnectionStates currentState = socket.State;

// Abort connection immediately
socket.Abort();
```

#### Sending Messages with Subscription Tracking

```csharp
// Send with subscription ID for automatic resend after reconnect
long subscriptionId = 12345;
await socket.SendAsync(
    obj: new { action = "subscribe", symbol = "BTCUSD" },
    subId: subscriptionId
);

// Unsubscribe (negative ID removes from resend queue)
await socket.SendAsync(
    obj: new { action = "unsubscribe", symbol = "BTCUSD" },
    subId: -subscriptionId
);

// Manual resend management
socket.RemoveResend(subscriptionId);  // Remove specific subscription
socket.RemoveResend();                 // Remove all subscriptions
```

#### Advanced Features

```csharp
// Custom initialization
socket.Init += ws =>
{
    ws.Options.SetRequestHeader("X-Custom-Header", "value");
    ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
};

// Post-connect hook
socket.PostConnect += async (isReconnect, ct) =>
{
    if (isReconnect)
        Console.WriteLine("Reconnected! Resubscribing...");

    await Task.CompletedTask;
};

// Pre-process received data (e.g., decompression)
socket.PreProcess2 = (input, output) =>
{
    // Decompress or transform data
    input.CopyTo(output);
    return input.Length;
};

// Send ping frame
await socket.SendOpCode(0x9);
```

### WebSocketMessage

Represents an incoming message from the WebSocket.

```csharp
// In your message processor
Func<WebSocketMessage, CancellationToken, ValueTask> process = (msg, ct) =>
{
    // Get as string
    string text = msg.AsString();

    // Deserialize to object
    var trade = msg.AsObject<TradeData>();

    // Deserialize to dynamic
    dynamic data = msg.AsObject();

    // Get JSON reader for streaming
    using var reader = msg.AsReader();

    // Access raw bytes
    ReadOnlyMemory<byte> bytes = msg.Memory;

    return ValueTask.CompletedTask;
};
```

### Connection States

The `ConnectionStates` enum represents the current state of the connection:

```csharp
public enum ConnectionStates
{
    Disconnected,   // Not connected
    Disconnecting,  // In process of disconnecting
    Connecting,     // In process of connecting
    Connected,      // Successfully connected
    Reconnecting,   // Attempting to reconnect
    Restored,       // Connection restored after reconnect
    Failed          // Connection failed
}
```

### ConnectionStateTracker

Track and aggregate states across multiple connections.

```csharp
var tracker = new ConnectionStateTracker();

// Add connections
tracker.Add(socket1);
tracker.Add(socket2);

// Monitor overall state
tracker.StateChanged += state =>
    Console.WriteLine($"Overall state: {state}");

// Connect all
await tracker.ConnectAsync(CancellationToken.None);

// Disconnect all
tracker.Disconnect();

// Remove connections
tracker.Remove(socket1);
```

The tracker aggregates states with the following logic:
- **Connected**: All connections are connected
- **Reconnecting**: Any connection is reconnecting
- **Restored**: All connections are connected or restored
- **Failed**: All connections have failed
- **Disconnected**: All connections are disconnected or failed

### IConnection Interface

Standard interface for connection management:

```csharp
public interface IConnection
{
    event Action<ConnectionStates> StateChanged;
    ValueTask ConnectAsync(CancellationToken cancellationToken);
    void Disconnect();
}
```

Both `WebSocketClient` and `ConnectionStateTracker` implement this interface.

## RestSharp Integration

The library includes helper methods for REST API calls, often used alongside WebSocket connections.

### Basic REST Request

```csharp
using Ecng.Net;
using RestSharp;

var request = new RestRequest(Method.Get);
request.AddQueryParameter("symbol", "BTCUSD");

var response = await request.InvokeAsync<PriceData>(
    url: new Uri("https://api.example.com/price"),
    caller: this,
    logVerbose: (fmt, args) => Console.WriteLine(fmt, args),
    token: CancellationToken.None
);

Console.WriteLine($"Price: {response.Price}");
```

### Authentication

```csharp
// Bearer token authentication
request.SetBearer(secureToken);

// Custom authenticator
var authenticator = new MyCustomAuthenticator();
var response = await request.InvokeAsync2<Data>(
    url: apiUrl,
    caller: this,
    logVerbose: logger,
    token: cancellationToken,
    auth: authenticator
);
```

### Error Handling

```csharp
try
{
    var response = await request.InvokeAsync<Data>(url, this, logger, token);
}
catch (RestSharpException ex)
{
    Console.WriteLine($"HTTP {ex.Response.StatusCode}: {ex.Response.Content}");
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Advanced REST Features

```csharp
// Custom content converter
var response = await request.InvokeAsync<Data>(
    url: apiUrl,
    caller: this,
    logVerbose: logger,
    token: cancellationToken,
    contentConverter: content => content.Replace("null", "\"\"")
);

// Handle specific error status codes
var response = await request.InvokeAsync3<Data>(
    url: apiUrl,
    caller: this,
    logVerbose: logger,
    token: cancellationToken,
    handleErrorStatus: statusCode =>
    {
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            // Custom handling
            return true; // Handled
        }
        return false; // Not handled, will throw
    }
);

// Add body as string
request.AddBodyAsStr("{\"key\": \"value\"}");

// Remove parameters
request.RemoveWhere(p => p.Name == "old_param");

// Convert parameters to query string
string queryString = request.Parameters.ToQueryString();
```

### JWT Decoding

```csharp
string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
var parts = token.DecodeJWT();

foreach (var part in parts)
{
    Console.WriteLine(part);
}
```

## Complete Examples

### Crypto Exchange WebSocket Client

```csharp
public class CryptoExchangeClient : IDisposable
{
    private readonly WebSocketClient _socket;
    private long _subscriptionCounter;

    public CryptoExchangeClient(string wsUrl)
    {
        _socket = new WebSocketClient(
            url: wsUrl,
            stateChanged: OnStateChanged,
            error: OnError,
            process: ProcessMessage,
            infoLog: (fmt, args) => Console.WriteLine($"[INFO] {fmt}", args),
            errorLog: (fmt, args) => Console.Error.WriteLine($"[ERROR] {fmt}", args),
            verboseLog: null
        )
        {
            ReconnectAttempts = -1,  // Infinite reconnection
            ReconnectInterval = TimeSpan.FromSeconds(5),
            ResendTimeout = TimeSpan.FromSeconds(1)
        };

        _socket.Init += ws =>
        {
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        };

        _socket.PostConnect += async (isReconnect, ct) =>
        {
            if (isReconnect)
            {
                Console.WriteLine("Reconnected! Subscriptions will be restored automatically.");
            }
        };
    }

    public async Task ConnectAsync()
    {
        await _socket.ConnectAsync(CancellationToken.None);
    }

    public async Task SubscribeToTradesAsync(string symbol)
    {
        var subId = ++_subscriptionCounter;

        await _socket.SendAsync(
            obj: new
            {
                type = "subscribe",
                channel = "trades",
                symbol = symbol
            },
            subId: subId
        );

        Console.WriteLine($"Subscribed to {symbol} trades (ID: {subId})");
    }

    public async Task UnsubscribeFromTradesAsync(long subscriptionId, string symbol)
    {
        await _socket.SendAsync(
            obj: new
            {
                type = "unsubscribe",
                channel = "trades",
                symbol = symbol
            },
            subId: -subscriptionId  // Negative to remove from resend queue
        );
    }

    private void OnStateChanged(ConnectionStates state)
    {
        Console.WriteLine($"Connection state: {state}");

        if (state == ConnectionStates.Connected)
        {
            // Connection established
        }
        else if (state == ConnectionStates.Restored)
        {
            // Connection restored after disconnect
        }
        else if (state == ConnectionStates.Failed)
        {
            // Connection failed after all retry attempts
        }
    }

    private void OnError(Exception ex)
    {
        Console.Error.WriteLine($"WebSocket error: {ex}");
    }

    private async ValueTask ProcessMessage(WebSocketMessage msg, CancellationToken ct)
    {
        try
        {
            var message = msg.AsObject<dynamic>();

            if (message.type == "trade")
            {
                Console.WriteLine($"Trade: {message.symbol} @ {message.price}");
            }
            else if (message.type == "error")
            {
                Console.Error.WriteLine($"Server error: {message.message}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing message: {ex.Message}");
        }

        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _socket?.Disconnect();
        _socket?.Dispose();
    }
}

// Usage
await using var client = new CryptoExchangeClient("wss://api.exchange.com/ws");
await client.ConnectAsync();
await client.SubscribeToTradesAsync("BTCUSD");

// Keep running
await Task.Delay(Timeout.Infinite);
```

### Multi-Connection Manager

```csharp
public class MultiExchangeClient
{
    private readonly ConnectionStateTracker _tracker;
    private readonly WebSocketClient _exchangeA;
    private readonly WebSocketClient _exchangeB;

    public MultiExchangeClient()
    {
        _tracker = new ConnectionStateTracker();

        _exchangeA = CreateClient("wss://exchange-a.com/ws", "Exchange A");
        _exchangeB = CreateClient("wss://exchange-b.com/ws", "Exchange B");

        _tracker.Add(_exchangeA);
        _tracker.Add(_exchangeB);

        _tracker.StateChanged += state =>
        {
            Console.WriteLine($"Overall connection state: {state}");

            if (state == ConnectionStates.Connected)
            {
                Console.WriteLine("All exchanges connected!");
            }
        };
    }

    private WebSocketClient CreateClient(string url, string name)
    {
        return new WebSocketClient(
            url: url,
            stateChanged: state => Console.WriteLine($"{name}: {state}"),
            error: ex => Console.Error.WriteLine($"{name} error: {ex.Message}"),
            process: (msg, ct) =>
            {
                Console.WriteLine($"{name}: {msg.AsString()}");
                return ValueTask.CompletedTask;
            },
            infoLog: (fmt, args) => Console.WriteLine($"[{name}] {fmt}", args),
            errorLog: (fmt, args) => Console.Error.WriteLine($"[{name}] {fmt}", args),
            verboseLog: null
        )
        {
            ReconnectAttempts = 5,
            ReconnectInterval = TimeSpan.FromSeconds(3)
        };
    }

    public async Task ConnectAllAsync()
    {
        await _tracker.ConnectAsync(CancellationToken.None);
    }

    public void DisconnectAll()
    {
        _tracker.Disconnect();
    }
}
```

### WebSocket with REST API Integration

```csharp
public class TradingClient
{
    private readonly WebSocketClient _wsClient;
    private readonly Uri _restApiUrl;

    public TradingClient(string wsUrl, string restUrl)
    {
        _restApiUrl = new Uri(restUrl);
        _wsClient = new WebSocketClient(
            url: wsUrl,
            stateChanged: state => Console.WriteLine($"WS State: {state}"),
            error: ex => Console.Error.WriteLine($"WS Error: {ex}"),
            process: ProcessWebSocketMessage,
            infoLog: (fmt, args) => Console.WriteLine(fmt, args),
            errorLog: (fmt, args) => Console.Error.WriteLine(fmt, args),
            verboseLog: null
        );
    }

    public async Task<AccountInfo> GetAccountInfoAsync()
    {
        var request = new RestRequest("/account", Method.Get);
        request.SetBearer(GetAuthToken());

        try
        {
            var account = await request.InvokeAsync<AccountInfo>(
                url: new Uri(_restApiUrl, request.Resource),
                caller: this,
                logVerbose: (fmt, args) => Console.WriteLine(fmt, args),
                token: CancellationToken.None
            );

            return account;
        }
        catch (RestSharpException ex)
        {
            Console.Error.WriteLine($"REST API Error: {ex.Message}");
            Console.Error.WriteLine($"Status: {ex.Response.StatusCode}");
            Console.Error.WriteLine($"Content: {ex.Response.Content}");
            throw;
        }
    }

    public async Task PlaceOrderAsync(string symbol, decimal price, decimal quantity)
    {
        var request = new RestRequest("/orders", Method.Post);
        request.SetBearer(GetAuthToken());
        request.AddBodyAsStr(new
        {
            symbol = symbol,
            price = price,
            quantity = quantity
        }.ToJson());

        var order = await request.InvokeAsync<Order>(
            url: new Uri(_restApiUrl, request.Resource),
            caller: this,
            logVerbose: (fmt, args) => Console.WriteLine(fmt, args),
            token: CancellationToken.None
        );

        Console.WriteLine($"Order placed: {order.Id}");
    }

    private async ValueTask ProcessWebSocketMessage(WebSocketMessage msg, CancellationToken ct)
    {
        var data = msg.AsObject<dynamic>();

        if (data.type == "order_update")
        {
            Console.WriteLine($"Order {data.orderId} status: {data.status}");
        }

        await ValueTask.CompletedTask;
    }

    private SecureString GetAuthToken()
    {
        // Return your authentication token
        throw new NotImplementedException();
    }
}
```

## Best Practices

1. **Always handle errors**: Provide error handlers to catch and log exceptions.

2. **Configure reconnection**: Set appropriate reconnection attempts and intervals based on your use case.

3. **Use subscription IDs**: Track subscriptions with IDs for automatic resend after reconnection.

4. **Monitor connection states**: React to state changes to update your UI or trigger business logic.

5. **Dispose properly**: Always dispose of WebSocketClient when done to clean up resources.

6. **Use async/await**: Prefer async methods for better scalability.

7. **Implement backoff**: Use increasing reconnection intervals to avoid overwhelming the server.

8. **Log appropriately**: Use different log levels (info, error, verbose) for debugging and monitoring.

## Thread Safety

The `WebSocketClient` class is designed to be thread-safe for the following operations:
- Sending messages
- Connection/disconnection
- State management
- Subscription tracking

However, you should not share a single `WebSocketMessage` instance across threads, as it contains read-only memory references.

## Performance Considerations

- **Buffer Sizes**: Adjust `BufferSize` and `BufferSizeUncompress` based on your message sizes.
- **Resend Interval**: Lower intervals increase network traffic; higher intervals delay recovery.
- **Reconnect Attempts**: Balance between reliability and resource usage.
- **Verbose Logging**: Disable in production for better performance.

## License

This library is part of the Ecng framework.

## Support

For issues, questions, or contributions, please refer to the main StockSharp repository.
