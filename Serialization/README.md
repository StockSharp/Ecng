# Ecng.Serialization

A comprehensive serialization library providing JSON serialization and high-performance binary primitives for .NET applications.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [JSON Serialization](#json-serialization)
- [Binary Serialization](#binary-serialization)
- [SettingsStorage](#settingsstorage)
- [IPersistable Interface](#ipersistable-interface)
- [Extension Methods](#extension-methods)
- [Advanced Features](#advanced-features)

## Overview

Ecng.Serialization provides a flexible and efficient serialization framework with the following key features:

- **JSON Serialization**: Full-featured JSON serializer with customizable settings
- **Binary Primitives**: High-performance `SpanWriter` and `SpanReader` for compact binary formats
- **SettingsStorage**: Dictionary-based storage system for application settings
- **IPersistable Pattern**: Interface-based serialization for custom types
- **Async Support**: Asynchronous serialization methods with cancellation token support
- **Extension Methods**: Convenient helpers for common serialization tasks

## Installation

Add a reference to the `Ecng.Serialization` assembly in your project.

```xml
<PackageReference Include="Ecng.Serialization" Version="x.x.x" />
```

## Quick Start

### JSON Serialization

```csharp
using Ecng.Serialization;

// Create a JSON serializer with default settings
var serializer = JsonSerializer<MyData>.CreateDefault();

// Serialize to file
await using var stream = File.OpenWrite("data.json");
await serializer.SerializeAsync(data, stream, CancellationToken.None);

// Deserialize from file
await using var readStream = File.OpenRead("data.json");
var loaded = await serializer.DeserializeAsync(readStream, CancellationToken.None);
```

### Binary Primitives

```csharp
using Ecng.Serialization;

// Write binary data
SpanWriter writer = stackalloc byte[256];
writer.WriteInt32(42);
writer.WriteString("hello");
writer.WriteDateTime(DateTime.UtcNow);

// Read binary data
var reader = new SpanReader(writer.GetWrittenSpan());
int number = reader.ReadInt32();
string text = reader.ReadString(5, Encoding.UTF8);
DateTime timestamp = reader.ReadDateTime();
```

### SettingsStorage

```csharp
using Ecng.Serialization;

// Create and populate settings
var settings = new SettingsStorage();
settings.Set("Host", "localhost");
settings.Set("Port", 8080);
settings.Set("Timeout", TimeSpan.FromSeconds(30));

// Serialize to JSON string
var serializer = JsonSerializer<SettingsStorage>.CreateDefault();
string json = serializer.SaveToString(settings);

// Deserialize from JSON string
var restored = serializer.LoadFromString(json);
string host = restored.GetValue<string>("Host");
int port = restored.GetValue<int>("Port");
```

## Core Concepts

### ISerializer Interface

The `ISerializer<T>` interface is the foundation of the serialization framework:

```csharp
public interface ISerializer<T>
{
    string FileExtension { get; }
    ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);
    ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
}
```

All serializers implement this interface, providing consistent API across different formats.

## JSON Serialization

### Creating a JSON Serializer

```csharp
// Default configuration (indented, enums as strings, ignore null values)
var serializer = JsonSerializer<MyClass>.CreateDefault();

// Custom configuration
var customSerializer = new JsonSerializer<MyClass>
{
    Indent = true,                    // Pretty-print JSON
    EnumAsString = true,              // Serialize enums as strings
    NullValueHandling = NullValueHandling.Ignore,  // Omit null values
    Encoding = Encoding.UTF8,         // Text encoding
    BufferSize = 4096                 // Buffer size for I/O
};
```

### JSON Serializer Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Indent` | `bool` | `false` | Format JSON with indentation |
| `EnumAsString` | `bool` | `false` | Serialize enums as strings instead of numbers |
| `NullValueHandling` | `NullValueHandling` | `Include` | How to handle null values |
| `Encoding` | `Encoding` | `UTF8` | Text encoding for serialization |
| `BufferSize` | `int` | `1024` | Buffer size for stream operations |
| `FillMode` | `bool` | `true` | Enable fill mode for IPersistable objects |
| `EncryptedAsByteArray` | `bool` | `false` | Serialize SecureString as byte array |

### Serialization Examples

#### Async File Serialization

```csharp
var serializer = JsonSerializer<OrderBook>.CreateDefault();

// Save to file
await using var file = File.OpenWrite("orderbook.json");
await serializer.SerializeAsync(orderBook, file, CancellationToken.None);

// Load from file
await using var input = File.OpenRead("orderbook.json");
var orderBook = await serializer.DeserializeAsync(input, CancellationToken.None);
```

#### Synchronous Serialization

```csharp
var serializer = JsonSerializer<MyData>.CreateDefault();

// Serialize to byte array
byte[] data = serializer.Serialize(myObject);

// Deserialize from byte array
var restored = serializer.Deserialize(data);

// Serialize to file
serializer.Serialize(myObject, "output.json");

// Deserialize from file
var loaded = serializer.Deserialize("output.json");
```

#### String Serialization

```csharp
var serializer = JsonSerializer<SettingsStorage>.CreateDefault();

// Serialize to string
string json = serializer.SaveToString(settings);

// Deserialize from string
var settings = serializer.LoadFromString(json);
```

### Supported Types

The JSON serializer supports:

- **Primitives**: `int`, `long`, `double`, `decimal`, `bool`, `string`, etc.
- **Date/Time**: `DateTime`, `DateTimeOffset`, `TimeSpan`
- **Collections**: Arrays, `List<T>`, `IEnumerable<T>`
- **Special Types**: `Guid`, `byte[]`, `SecureString`, `TimeZoneInfo`, `Type`
- **Custom Types**: Types implementing `IPersistable` or `IAsyncPersistable`
- **SettingsStorage**: Native support for settings dictionary

## Binary Serialization

### SpanWriter - Writing Binary Data

`SpanWriter` is a high-performance ref struct for writing primitive types to a span of bytes.

```csharp
// Allocate buffer on stack
SpanWriter writer = stackalloc byte[1024];

// Write primitive types
writer.WriteByte(255);
writer.WriteSByte(-128);
writer.WriteBoolean(true);
writer.WriteInt16(short.MaxValue);
writer.WriteUInt16(ushort.MaxValue);
writer.WriteInt32(42);
writer.WriteUInt32(100u);
writer.WriteInt64(long.MaxValue);
writer.WriteUInt64(ulong.MaxValue);
writer.WriteSingle(3.14f);
writer.WriteDouble(2.718281828);
writer.WriteDecimal(1234.5678m);

// Write date/time types
writer.WriteDateTime(DateTime.UtcNow);
writer.WriteTimeSpan(TimeSpan.FromHours(1));

// Write strings (requires encoding)
writer.WriteString("Hello, World!", Encoding.UTF8);

// Write GUID
writer.WriteGuid(Guid.NewGuid());

// Write character
writer.WriteChar('A');

// Get written data
ReadOnlySpan<byte> data = writer.GetWrittenSpan();
int bytesWritten = writer.Position;
```

#### Big-Endian / Little-Endian

```csharp
// Little-endian (default, Intel x86/x64)
SpanWriter writerLE = stackalloc byte[256];
writerLE.WriteInt32(0x12345678);  // Bytes: 78 56 34 12

// Big-endian (network byte order)
SpanWriter writerBE = new SpanWriter(buffer, isBigEndian: true);
writerBE.WriteInt32(0x12345678);  // Bytes: 12 34 56 78
```

#### Advanced SpanWriter Usage

```csharp
byte[] buffer = new byte[1024];
var writer = new SpanWriter(buffer);

// Skip bytes (advance position without writing)
writer.Skip(16);

// Write span of bytes directly
ReadOnlySpan<byte> source = stackalloc byte[] { 1, 2, 3, 4, 5 };
writer.WriteSpan(source);

// Write structures (value types)
var header = new PacketHeader { Version = 1, Length = 100 };
writer.WriteStruct(header, Marshal.SizeOf<PacketHeader>());

// Check remaining space
if (!writer.IsFull)
{
    int remaining = writer.Remaining;
    // Write more data
}

// Get position
int currentPos = writer.Position;
```

### SpanReader - Reading Binary Data

`SpanReader` is a high-performance ref struct for reading primitive types from a span of bytes.

```csharp
ReadOnlySpan<byte> data = /* your binary data */;
var reader = new SpanReader(data);

// Read primitive types (must match write order)
byte b = reader.ReadByte();
sbyte sb = reader.ReadSByte();
bool flag = reader.ReadBoolean();
short s = reader.ReadInt16();
ushort us = reader.ReadUInt16();
int i = reader.ReadInt32();
uint ui = reader.ReadUInt32();
long l = reader.ReadInt64();
ulong ul = reader.ReadUInt64();
float f = reader.ReadSingle();
double d = reader.ReadDouble();
decimal dec = reader.ReadDecimal();

// Read date/time types
DateTime dt = reader.ReadDateTime();
TimeSpan ts = reader.ReadTimeSpan();

// Read string (must know length)
string text = reader.ReadString(13, Encoding.UTF8);

// Read GUID
Guid id = reader.ReadGuid();

// Read character
char c = reader.ReadChar();

// Check if end of span
if (!reader.End)
{
    int remaining = reader.Remaining;
    // Read more data
}
```

#### Advanced SpanReader Usage

```csharp
var reader = new SpanReader(binaryData);

// Skip bytes
reader.Skip(16);

// Read span of bytes
ReadOnlySpan<byte> chunk = reader.ReadSpan(256);

// Read structure
var header = reader.ReadStruct<PacketHeader>(Marshal.SizeOf<PacketHeader>());

// Read array of structures
var items = new Item[10];
reader.ReadStructArray(items, Marshal.SizeOf<Item>(), 10);

// Get current position
int position = reader.Position;

// Check if at end
bool isEnd = reader.End;
int bytesLeft = reader.Remaining;
```

### Binary Serialization Example

```csharp
public class BinarySerializer
{
    public byte[] Serialize(TradeData trade)
    {
        byte[] buffer = new byte[256];
        var writer = new SpanWriter(buffer);

        writer.WriteInt64(trade.Id);
        writer.WriteDecimal(trade.Price);
        writer.WriteDecimal(trade.Volume);
        writer.WriteDateTime(trade.Timestamp);
        writer.WriteInt32(trade.Direction);

        return buffer[..writer.Position];
    }

    public TradeData Deserialize(byte[] data)
    {
        var reader = new SpanReader(data);

        return new TradeData
        {
            Id = reader.ReadInt64(),
            Price = reader.ReadDecimal(),
            Volume = reader.ReadDecimal(),
            Timestamp = reader.ReadDateTime(),
            Direction = reader.ReadInt32()
        };
    }
}
```

## SettingsStorage

`SettingsStorage` is a thread-safe dictionary for storing configuration and settings.

### Basic Usage

```csharp
var settings = new SettingsStorage();

// Set values (fluent API)
settings.Set("ServerUrl", "https://api.example.com")
        .Set("Port", 8080)
        .Set("EnableLogging", true)
        .Set("Timeout", TimeSpan.FromSeconds(30))
        .Set("MaxRetries", 3);

// Get values with type safety
string url = settings.GetValue<string>("ServerUrl");
int port = settings.GetValue<int>("Port");
bool logging = settings.GetValue<bool>("EnableLogging");

// Get values with default
int retries = settings.GetValue("MaxRetries", defaultValue: 5);
string missing = settings.GetValue("NotFound", defaultValue: "default");

// Check if key exists
if (settings.Contains("ServerUrl"))
{
    // Key exists
}

// Get all setting names
IEnumerable<string> names = settings.Names;
```

### Nested Settings

```csharp
var settings = new SettingsStorage();

// Create nested settings
var database = new SettingsStorage()
    .Set("Host", "localhost")
    .Set("Port", 5432)
    .Set("Database", "myapp");

var logging = new SettingsStorage()
    .Set("Level", "Info")
    .Set("FilePath", "logs/app.log");

settings.Set("Database", database)
        .Set("Logging", logging);

// Retrieve nested settings
var dbSettings = settings.GetValue<SettingsStorage>("Database");
string dbHost = dbSettings.GetValue<string>("Host");

// Or retrieve specific nested value
var logSettings = settings.GetValue<SettingsStorage>("Logging");
string logLevel = logSettings.GetValue<string>("Level");
```

### Async Value Retrieval

```csharp
var settings = new SettingsStorage();

// Async get with cancellation token
string value = await settings.GetValueAsync<string>(
    "ServerUrl",
    defaultValue: "https://default.com",
    cancellationToken: CancellationToken.None
);
```

### Serializing SettingsStorage

```csharp
var settings = new SettingsStorage()
    .Set("AppName", "MyApp")
    .Set("Version", "1.0.0");

// Serialize to JSON
var serializer = JsonSerializer<SettingsStorage>.CreateDefault();
string json = serializer.SaveToString(settings);

// Output:
// {
//   "AppName": "MyApp",
//   "Version": "1.0.0"
// }

// Deserialize from JSON
var restored = serializer.LoadFromString(json);
```

## IPersistable Interface

The `IPersistable` interface enables custom serialization for your types.

### Implementing IPersistable

```csharp
public class TradingStrategy : IPersistable
{
    public string Name { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public int MaxPositions { get; set; }
    public TimeSpan HoldingPeriod { get; set; }

    public void Load(SettingsStorage storage)
    {
        Name = storage.GetValue<string>(nameof(Name));
        StopLoss = storage.GetValue<decimal>(nameof(StopLoss));
        TakeProfit = storage.GetValue<decimal>(nameof(TakeProfit));
        MaxPositions = storage.GetValue<int>(nameof(MaxPositions));
        HoldingPeriod = storage.GetValue<TimeSpan>(nameof(HoldingPeriod));
    }

    public void Save(SettingsStorage storage)
    {
        storage.Set(nameof(Name), Name)
               .Set(nameof(StopLoss), StopLoss)
               .Set(nameof(TakeProfit), TakeProfit)
               .Set(nameof(MaxPositions), MaxPositions)
               .Set(nameof(HoldingPeriod), HoldingPeriod);
    }
}
```

### Using IPersistable Objects

```csharp
var strategy = new TradingStrategy
{
    Name = "Momentum",
    StopLoss = 0.02m,
    TakeProfit = 0.05m,
    MaxPositions = 10,
    HoldingPeriod = TimeSpan.FromHours(24)
};

// Save to SettingsStorage
var settings = strategy.Save();

// Serialize to JSON
var serializer = JsonSerializer<TradingStrategy>.CreateDefault();
await using var file = File.OpenWrite("strategy.json");
await serializer.SerializeAsync(strategy, file, CancellationToken.None);

// Deserialize from JSON
await using var input = File.OpenRead("strategy.json");
var loaded = await serializer.DeserializeAsync(input, CancellationToken.None);

// Clone an object
var clone = strategy.Clone();

// Apply state from one object to another
var newStrategy = new TradingStrategy();
newStrategy.Apply(strategy);
```

### IAsyncPersistable Interface

For asynchronous serialization scenarios:

```csharp
public class AsyncDataLoader : IAsyncPersistable
{
    public string ConnectionString { get; set; }
    public List<string> LoadedData { get; set; }

    public async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
    {
        ConnectionString = storage.GetValue<string>(nameof(ConnectionString));

        // Perform async operations
        await Task.Delay(100, cancellationToken);

        var data = storage.GetValue<string[]>(nameof(LoadedData));
        LoadedData = new List<string>(data);
    }

    public async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
    {
        storage.Set(nameof(ConnectionString), ConnectionString);

        // Perform async operations
        await Task.Delay(100, cancellationToken);

        storage.Set(nameof(LoadedData), LoadedData.ToArray());
    }
}
```

### IPersistable Helper Methods

```csharp
// Save to SettingsStorage
SettingsStorage settings = myObject.Save();

// Load from SettingsStorage
var obj = new MyClass();
obj.Load(settings);

// Load typed object
var typed = settings.Load<MyClass>();

// Save entire object with type information
var storage = myObject.SaveEntire(isAssemblyQualifiedName: false);

// Load entire object with type creation
var restored = storage.LoadEntire<IPersistable>();

// Clone
var clone = myObject.Clone();

// Async clone
var asyncClone = await myAsyncObject.CloneAsync(CancellationToken.None);

// Apply state from clone
myObject.Apply(clone);

// Async apply
await myAsyncObject.ApplyAsync(clone, CancellationToken.None);
```

## Extension Methods

### ISerializer Extensions

```csharp
var serializer = JsonSerializer<MyData>.CreateDefault();

// Synchronous serialization
byte[] data = serializer.Serialize(myObject);
serializer.Serialize(myObject, "output.json");
serializer.Serialize(myObject, stream);

// Synchronous deserialization
var obj1 = serializer.Deserialize(data);
var obj2 = serializer.Deserialize("input.json");
var obj3 = serializer.Deserialize(stream);

// String serialization
string json = serializer.SaveToString(myObject);
var restored = serializer.LoadFromString(json);
```

### JSON Helper Methods

```csharp
using Ecng.Serialization;

// Serialize to JSON string
string json = myObject.ToJson(indent: true);

// Deserialize from JSON string
var obj = json.DeserializeObject<MyClass>();

// Create JSON serializer settings
var settings = JsonHelper.CreateJsonSerializerSettings();

// Skip BOM from byte array
byte[] cleanData = jsonBytes.SkipBom();

// Skip BOM from string
string cleanJson = jsonString.SkipBom();
```

## Advanced Features

### Custom Serializers

Register custom serializers for specific types:

```csharp
// Register custom serializer
PersistableHelper.RegisterCustomSerializer<MyType>(
    serialize: obj =>
    {
        var storage = new SettingsStorage();
        storage.Set("CustomField", obj.CustomProperty);
        return storage;
    },
    deserialize: storage =>
    {
        return new MyType
        {
            CustomProperty = storage.GetValue<string>("CustomField")
        };
    }
);

// Use custom serializer
MyType obj = new MyType { CustomProperty = "value" };
if (obj.TrySerialize(out var storage))
{
    // Custom serialization succeeded
}

if (storage.TryDeserialize<MyType>(out var deserialized))
{
    // Custom deserialization succeeded
}

// Unregister custom serializer
PersistableHelper.UnRegisterCustomSerializer<MyType>();
```

### Type Adapters

Register adapters for non-persistable types:

```csharp
// Register adapter for a type
typeof(MyType).RegisterAdapterType(typeof(MyTypeAdapter));

// Remove adapter
typeof(MyType).RemoveAdapterType();

// Check if adapter exists
if (typeof(MyType).TryGetAdapterType(out Type adapterType))
{
    // Adapter registered
}
```

### Adapter Implementation

```csharp
public class MyTypeAdapter : IPersistable, IPersistableAdapter
{
    public object UnderlyingValue { get; set; }

    public void Load(SettingsStorage storage)
    {
        var myType = new MyType
        {
            Property = storage.GetValue<string>("Property")
        };
        UnderlyingValue = myType;
    }

    public void Save(SettingsStorage storage)
    {
        var myType = (MyType)UnderlyingValue;
        storage.Set("Property", myType.Property);
    }
}
```

### Tuple Serialization

```csharp
// Convert tuple to storage
var pair = new RefPair<int, string> { First = 42, Second = "hello" };
var storage = pair.ToStorage();

// Convert storage to tuple
var restored = storage.ToRefPair<int, string>();

// Also supports RefTriple, RefQuadruple, RefFive
var triple = new RefTriple<int, string, bool>
{
    First = 1,
    Second = "two",
    Third = true
};
var tripleStorage = triple.ToStorage();
var restoredTriple = tripleStorage.ToRefTriple<int, string, bool>();
```

### MemberInfo Serialization

```csharp
using System.Reflection;

// Serialize MemberInfo
MethodInfo method = typeof(MyClass).GetMethod("MyMethod");
var storage = method.ToStorage(isAssemblyQualifiedName: false);

// Deserialize MemberInfo
var restored = storage.ToMember<MethodInfo>();

// Also works with Type, PropertyInfo, FieldInfo, etc.
Type type = typeof(MyClass);
var typeStorage = type.ToStorage();
var restoredType = typeStorage.ToMember<Type>();
```

### Object Serialization

```csharp
// Serialize any object to storage
object value = 42;
var storage = value.ToStorage(isAssemblyQualifiedName: false);

// Deserialize from storage
object restored = storage.FromStorage();

// Works with IPersistable objects too
var persistable = new MyPersistableClass();
var objStorage = persistable.ToStorage();
var objRestored = objStorage.FromStorage();
```

### Conditional Loading

```csharp
var obj = new MyPersistableClass();

// Load only if storage is not null
bool loaded = obj.LoadIfNotNull(settings, "MyKey");

// Load from nested setting if exists
if (obj.LoadIfNotNull(settings.GetValue<SettingsStorage>("Nested")))
{
    // Successfully loaded from nested settings
}
```

### File Extension

```csharp
var serializer = JsonSerializer<MyData>.CreateDefault();

// Get file extension for the format
string ext = serializer.FileExtension;  // Returns "json"

// Use in file operations
string fileName = $"data.{serializer.FileExtension}";
```

## Best Practices

### 1. Use CreateDefault() for JSON

```csharp
// Good: Uses sensible defaults (indent, enums as strings, ignore nulls)
var serializer = JsonSerializer<MyData>.CreateDefault();

// Avoid: Manual configuration unless you need specific settings
var serializer = new JsonSerializer<MyData>
{
    Indent = true,
    EnumAsString = true,
    NullValueHandling = NullValueHandling.Ignore
};
```

### 2. Prefer Async Methods

```csharp
// Good: Async for I/O operations
await serializer.SerializeAsync(data, stream, cancellationToken);

// Avoid: Sync methods for file/network I/O
serializer.Serialize(data, stream);  // Only for in-memory streams
```

### 3. Use IPersistable for Domain Objects

```csharp
// Good: Explicit control over serialization
public class Order : IPersistable
{
    public void Load(SettingsStorage storage) { /* ... */ }
    public void Save(SettingsStorage storage) { /* ... */ }
}

// Avoid: Relying on reflection for complex objects
```

### 4. Dispose Streams Properly

```csharp
// Good: Using statement ensures disposal
await using var stream = File.OpenRead("data.json");
var data = await serializer.DeserializeAsync(stream, cancellationToken);

// Avoid: Manual disposal
var stream = File.OpenRead("data.json");
try
{
    var data = await serializer.DeserializeAsync(stream, cancellationToken);
}
finally
{
    stream.Dispose();
}
```

### 5. Stack Allocation for Binary

```csharp
// Good: Stack allocation for small buffers
SpanWriter writer = stackalloc byte[256];
writer.WriteInt32(42);

// Avoid: Heap allocation unless necessary
byte[] buffer = new byte[256];
var writer = new SpanWriter(buffer);
```

## Performance Considerations

### Binary Serialization

- `SpanWriter` and `SpanReader` use stack allocation for maximum performance
- Zero allocation for primitives when using `stackalloc`
- No boxing/unboxing
- Direct memory access

### JSON Serialization

- Configurable buffer sizes for optimal I/O
- Async methods prevent thread blocking
- Streaming API for large files
- Efficient enum handling

### SettingsStorage

- Thread-safe dictionary implementation
- Case-insensitive key lookup
- Lazy type conversion
- Minimal allocations

## Error Handling

```csharp
try
{
    var serializer = JsonSerializer<MyData>.CreateDefault();
    var data = await serializer.DeserializeAsync(stream, cancellationToken);
}
catch (JsonException ex)
{
    // JSON parsing error
    Console.WriteLine($"Invalid JSON: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Serialization logic error
    Console.WriteLine($"Serialization error: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Operation was cancelled
    Console.WriteLine("Operation cancelled");
}
```

## Thread Safety

- `SettingsStorage` is thread-safe
- `JsonSerializer<T>` instances are thread-safe for concurrent reads
- `SpanWriter` and `SpanReader` are ref structs and not thread-safe (use on stack)

## License

See the main StockSharp repository for licensing information.

## Contributing

Contributions are welcome! Please submit pull requests to the main StockSharp repository.

## Support

For issues and questions, please use the StockSharp issue tracker or community forums.
