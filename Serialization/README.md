# Ecng.Serialization

Helpers for JSON and high-performance binary serialization.

## Purpose

Provide easy to use serializers with built in `SettingsStorage` support so objects can be persisted or transmitted with minimal code.

## Key Features

- JSON serializer with indentation and enum string options
- Streaming API and asynchronous methods
- Custom converters and `IPersistable` helpers
- `SpanWriter`/`SpanReader` for compact binary format

## JSON Example

```csharp
var serializer = JsonSerializer<MyData>.CreateDefault();
await using var stream = File.OpenWrite("data.json");
await serializer.SerializeAsync(data, stream, CancellationToken.None);

await using var read = File.OpenRead("data.json");
var loaded = await serializer.DeserializeAsync(read, CancellationToken.None);
```

Same using standard .NET:

```csharp
await System.Text.Json.JsonSerializer.SerializeAsync(stream, data);
```

## Binary primitives

```csharp
SpanWriter writer = stackalloc byte[256];
writer.WriteInt32(42);
writer.WriteString("hello");

var reader = new SpanReader(writer.Buffer);
int num = reader.ReadInt32();
string text = reader.ReadString();
```

## SettingsStorage

```csharp
var storage = new SettingsStorage();
myObj.Save(storage);

string raw = storage.SaveToString<JsonSerializer<SettingsStorage>>();
var restored = raw.LoadFromString<JsonSerializer<SettingsStorage>>();
```
