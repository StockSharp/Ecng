# Ecng.Net.SocketIO

Client library implementing the Socket.IO protocol.

## Purpose

Provides a high-level API for real-time communication over WebSockets.

## Key Features

- Easy real-time messaging
- Automatic reconnects
- Command resending
- Optional logging hooks

## Raw WebSocket

```csharp
var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri(url), CancellationToken.None);
// handshake and message parsing required
```

## Using Ecng

```csharp
var socket = new WebSocketClient(
    url,
    state => Console.WriteLine($"State: {state}"),
    ex => Console.WriteLine(ex),
    (c, msg, ct) => { Console.WriteLine(msg.Text); return ValueTask.CompletedTask; },
    Console.WriteLine,
    Console.Error.WriteLine,
    Console.WriteLine);

await socket.ConnectAsync();
socket.Send(new { hello = "world" });
```
