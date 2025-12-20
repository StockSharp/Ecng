# Lzma - LZMA Compression Library

A .NET implementation of the LZMA (Lempel-Ziv-Markov chain-Algorithm) compression algorithm. This library provides stream-based compression and decompression capabilities with a simple, easy-to-use API.

## Features

- Stream-based LZMA compression and decompression
- Compatible with .lzma file format
- Full control over compression parameters
- Support for both raw LZMA streams and containerized streams
- Configurable dictionary size, literal context bits, and other encoding properties

## Installation

Add a reference to the Lzma project in your solution.

## Quick Start

### Basic Compression

```csharp
using System.IO;
using System.IO.Compression;
using Lzma;

// Compress data to a file
using (var fileStream = File.Create("output.lzma"))
using (var lzmaStream = new LzmaStream(fileStream, CompressionMode.Compress, leaveOpen: false))
{
    byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello, LZMA!");
    lzmaStream.Write(data, 0, data.Length);
}
```

### Basic Decompression

```csharp
using System.IO;
using System.IO.Compression;
using Lzma;

// Decompress data from a file
using (var fileStream = File.OpenRead("output.lzma"))
using (var lzmaStream = new LzmaStream(fileStream, CompressionMode.Decompress, leaveOpen: false))
{
    byte[] buffer = new byte[1024];
    int bytesRead = lzmaStream.Read(buffer, 0, buffer.Length);
    string result = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
    Console.WriteLine(result); // Output: Hello, LZMA!
}
```

## API Reference

### LzmaStream Class

The main class for LZMA compression/decompression. Inherits from `System.IO.Stream`.

#### Constructors

```csharp
// Create a compression or decompression stream with default settings
LzmaStream(Stream stream, CompressionMode mode, bool leaveOpen)

// Create a compression stream with specified compression level
LzmaStream(Stream stream, CompressionLevel level, bool leaveOpen)

// Create a compression stream with custom encoder properties
LzmaStream(Stream stream, EncoderProperties properties)
```

#### Properties

- `CanRead` - Returns true if the stream is in decompression mode
- `CanWrite` - Returns true if the stream is in compression mode
- `CanSeek` - Always returns false (seeking is not supported)
- `Length` - Gets the length of the decoded data (decompression mode only)

#### Methods

- `Read(byte[] buffer, int offset, int count)` - Reads and decodes data (decompression mode)
- `Write(byte[] buffer, int offset, int count)` - Encodes and writes data (compression mode)
- `Flush()` - Flushes the stream and completes compression
- `Close()` - Closes the stream

### Advanced Usage

#### Custom Compression Settings

```csharp
using Lzma;

// Create custom encoder properties
var encoderProperties = new EncoderProperties(
    workingSize: 1u << 12,        // Encoding buffer size
    dictionarySize: 1u << 24,     // 16 MB dictionary
    lc: 3,                         // Literal context bits (0-8)
    lp: 0,                         // Literal position bits (0-4)
    pb: 2,                         // Position bits (0-4)
    writeEndMarker: true           // Write end marker on close
);

using (var fileStream = File.Create("output.lzma"))
using (var lzmaStream = new LzmaStream(fileStream, encoderProperties))
{
    // Compress with custom settings
    byte[] data = File.ReadAllBytes("input.txt");
    lzmaStream.Write(data, 0, data.Length);
}
```

#### EncoderProperties

Controls the compression behavior:

```csharp
EncoderProperties properties = EncoderProperties.Default;

// Adjust properties
properties.DictionarySize = 1u << 23;  // 8 MB dictionary
properties.LC = 3;                      // Literal context bits
properties.LP = 0;                      // Literal position bits
properties.PB = 2;                      // Position bits
properties.WriteEndMarker = true;       // Write end marker
```

**Property Descriptions:**
- `WorkingSize` - Size of the encoding buffer in bytes
- `DictionarySize` - Size of the dictionary in bytes (larger = better compression, more memory)
- `LC` - Number of high bits of previous byte to use as context (0-8)
- `LP` - Number of low bits of dictionary position to include in literal position state (0-4)
- `PB` - Number of low bits of dictionary position to include in position state (0-4)
- `WriteEndMarker` - Whether to write an end marker when encoding is complete

### Working with Raw LZMA Streams

For raw LZMA streams without container headers, use `EncoderStream` and `DecoderStream` directly:

#### Raw Compression

```csharp
using Lzma;

using (var fileStream = File.Create("output.raw"))
{
    var encoderStream = new EncoderStream(fileStream);
    encoderStream.Initialize(EncoderProperties.Default);

    byte[] data = System.Text.Encoding.UTF8.GetBytes("Raw LZMA data");
    encoderStream.Write(data, 0, data.Length);
    encoderStream.Close();
}
```

#### Raw Decompression

```csharp
using Lzma;

using (var fileStream = File.OpenRead("output.raw"))
{
    var decoderProperties = DecoderProperties.Default;
    var decoderStream = new DecoderStream(fileStream);
    decoderStream.Initialize(decoderProperties);

    byte[] buffer = new byte[1024];
    int bytesRead = decoderStream.Read(buffer, 0, buffer.Length);
    string result = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
}
```

## Usage Examples

### Compress a File

```csharp
using System.IO;
using System.IO.Compression;
using Lzma;

public void CompressFile(string inputPath, string outputPath)
{
    using (var inputStream = File.OpenRead(inputPath))
    using (var outputStream = File.Create(outputPath))
    using (var lzmaStream = new LzmaStream(outputStream, CompressionMode.Compress, false))
    {
        inputStream.CopyTo(lzmaStream);
    }
}
```

### Decompress a File

```csharp
using System.IO;
using System.IO.Compression;
using Lzma;

public void DecompressFile(string inputPath, string outputPath)
{
    using (var inputStream = File.OpenRead(inputPath))
    using (var lzmaStream = new LzmaStream(inputStream, CompressionMode.Decompress, false))
    using (var outputStream = File.Create(outputPath))
    {
        lzmaStream.CopyTo(outputStream);
    }
}
```

### Compress in Memory

```csharp
using System.IO;
using System.IO.Compression;
using Lzma;

public byte[] CompressData(byte[] data)
{
    using (var memoryStream = new MemoryStream())
    {
        using (var lzmaStream = new LzmaStream(memoryStream, CompressionMode.Compress, true))
        {
            lzmaStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }
}

public byte[] DecompressData(byte[] compressedData)
{
    using (var inputStream = new MemoryStream(compressedData))
    using (var lzmaStream = new LzmaStream(inputStream, CompressionMode.Decompress, true))
    using (var outputStream = new MemoryStream())
    {
        lzmaStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}
```

### Compress Text

```csharp
using System.IO;
using System.IO.Compression;
using System.Text;
using Lzma;

public string CompressString(string text)
{
    byte[] data = Encoding.UTF8.GetBytes(text);

    using (var memoryStream = new MemoryStream())
    {
        using (var lzmaStream = new LzmaStream(memoryStream, CompressionMode.Compress, true))
        {
            lzmaStream.Write(data, 0, data.Length);
        }
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}

public string DecompressString(string compressedBase64)
{
    byte[] compressedData = Convert.FromBase64String(compressedBase64);

    using (var inputStream = new MemoryStream(compressedData))
    using (var lzmaStream = new LzmaStream(inputStream, CompressionMode.Decompress, true))
    using (var outputStream = new MemoryStream())
    {
        lzmaStream.CopyTo(outputStream);
        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
```

## Important Notes

- Always call `Flush()` or `Close()` on the compression stream to ensure all data is written
- The stream does not support seeking (`CanSeek` is always false)
- For compression, the decompressed size is written to the header automatically
- Larger dictionary sizes provide better compression but require more memory
- The default encoder properties provide a good balance between compression ratio and speed

## Performance Tips

1. **Dictionary Size**: Use the largest dictionary size that fits in available memory
2. **Working Size**: Increase for better compression on large files
3. **Reuse Streams**: When compressing multiple small files, consider reusing stream instances
4. **Buffer Size**: Use appropriate buffer sizes when reading/writing data

## Target Frameworks

- .NET Standard 2.0
- .NET 6.0
- .NET 10.0

## License

This library is distributed under its respective license. Please refer to the source code headers for details.
