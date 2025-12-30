# Ecng.IO.Compression

A comprehensive .NET library providing compression, decompression, and file system utilities with async support.

## Overview

Ecng.IO is part of the Ecng system framework by StockSharp, offering powerful helpers for:
- Data compression and decompression (GZip, Deflate, 7Zip/LZMA)
- ZIP archive extraction
- Fossil delta compression for efficient binary differencing
- File and directory operations
- Stream manipulation utilities
- Path management

## Target Frameworks

- .NET Standard 2.0
- .NET 6.0
- .NET 10.0

## Installation

Add a reference to the Ecng.IO.Compression project or NuGet package in your project.

```xml
<ProjectReference Include="path\to\Ecng.IO.Compression\IO.Compression.csproj" />
```

## Features

### 1. Compression & Decompression

The `CompressionHelper` class provides extension methods for compressing and decompressing data using various algorithms.

#### GZip Compression

```csharp
using Ecng.IO;

// Decompress GZip data to string
byte[] compressedData = GetGZipData();
string result = compressedData.UnGZip();

// Decompress GZip data to buffer
byte[] destination = new byte[1024];
int bytesWritten = compressedData.UnGZip(destination);

// Decompress from specific range
string result = compressedData.UnGZip(index: 0, count: 100);

// Modern .NET: Use spans for better performance
ReadOnlySpan<byte> compressedSpan = stackalloc byte[256];
string result = compressedSpan.UnGZip();
```

#### Deflate Compression

```csharp
using Ecng.IO;
using System.IO.Compression;

// Compress data with Deflate
byte[] data = GetRawData();
byte[] compressed = data.DeflateTo();

// Decompress Deflate data
string decompressed = compressed.UnDeflate();

// Decompress to buffer
byte[] destination = new byte[1024];
int bytesWritten = compressed.UnDeflate(destination);

// Decompress with custom buffer size
byte[] decompressed = compressed.DeflateFrom(bufferSize: CompressionHelper.DefaultBufferSize);
```

#### 7Zip/LZMA Compression

```csharp
using Ecng.IO;

// Compress with LZMA
byte[] data = GetRawData();
byte[] compressed = data.Do7Zip();

// Decompress LZMA data
byte[] decompressed = compressed.Un7Zip();
```

#### Generic Compression API

```csharp
using Ecng.IO;
using System.IO.Compression;

// Compress with any compression stream type
byte[] data = GetRawData();
byte[] compressed = data.Compress<GZipStream>(
    index: 0,
    count: data.Length,
    level: CompressionLevel.Optimal,
    bufferSize: CompressionHelper.DefaultBufferSize
);

// Decompress with any compression stream type
byte[] decompressed = compressed.Uncompress<GZipStream>(
    index: 0,
    count: compressed.Length,
    bufferSize: CompressionHelper.DefaultBufferSize
);
```

#### Async Compression

```csharp
using Ecng.IO;
using System.IO.Compression;
using System.Threading;

// Compress asynchronously
byte[] data = GetRawData();
byte[] compressed = await data.CompressAsync<GZipStream>(
    level: CompressionLevel.Optimal,
    bufferSize: CompressionHelper.DefaultBufferSize,
    cancellationToken: CancellationToken.None
);

// Decompress asynchronously
byte[] decompressed = await compressed.UncompressAsync<DeflateStream>(
    bufferSize: CompressionHelper.DefaultBufferSize,
    cancellationToken: CancellationToken.None
);

// Compress stream to stream
using var inputStream = File.OpenRead("input.dat");
using var outputStream = File.Create("output.gz");
await inputStream.CompressAsync<GZipStream>(
    output: outputStream,
    level: CompressionLevel.Optimal,
    leaveOpen: false,
    bufferSize: CompressionHelper.DefaultBufferSize
);

// Decompress stream to stream
using var compressedStream = File.OpenRead("compressed.gz");
using var decompressedStream = File.Create("decompressed.dat");
await compressedStream.UncompressAsync<GZipStream>(
    output: decompressedStream,
    leaveOpen: false,
    bufferSize: CompressionHelper.DefaultBufferSize
);
```

### 2. ZIP Archive Handling

```csharp
using Ecng.IO;

// Extract all entries from ZIP
byte[] zipData = File.ReadAllBytes("archive.zip");
using (var entries = zipData.Unzip())
{
    foreach (var (name, body) in entries)
    {
        Console.WriteLine($"File: {name}");
        // Process stream...
        body.CopyTo(outputStream);
    }
}

// Extract with filter
using (var entries = zipData.Unzip(filter: name => name.EndsWith(".txt")))
{
    foreach (var (name, body) in entries)
    {
        // Only .txt files are extracted
        ProcessTextFile(name, body);
    }
}

// Extract from stream
using var fileStream = File.OpenRead("archive.zip");
using (var entries = fileStream.Unzip(leaveOpen: false))
{
    foreach (var (name, body) in entries)
    {
        Console.WriteLine($"Processing: {name}");
    }
}
```

### 3. Fossil Delta Compression

Fossil delta compression is ideal for efficiently storing and transmitting differences between binary files, such as software updates or version control systems.

```csharp
using Ecng.IO.Fossil;
using System.Threading;

// Create a delta between two versions
byte[] originalFile = File.ReadAllBytes("version1.bin");
byte[] newFile = File.ReadAllBytes("version2.bin");

byte[] delta = await Delta.Create(
    origin: originalFile,
    target: newFile,
    token: CancellationToken.None
);

// Delta is typically much smaller than the full new file
Console.WriteLine($"Original: {originalFile.Length} bytes");
Console.WriteLine($"New: {newFile.Length} bytes");
Console.WriteLine($"Delta: {delta.Length} bytes");

// Apply delta to reconstruct the new file
byte[] reconstructed = await Delta.Apply(
    origin: originalFile,
    delta: delta,
    token: CancellationToken.None
);

// Verify reconstruction
bool isIdentical = reconstructed.SequenceEqual(newFile); // true

// Get output size from delta without applying it
uint outputSize = Delta.OutputSize(delta);
Console.WriteLine($"Expected output size: {outputSize} bytes");
```

### 4. File and Directory Operations

The library extends the functionality available through `IOHelper`.

#### Directory Management

```csharp
using Ecng.Common;

// Clear directory contents
DirectoryInfo dir = IOHelper.ClearDirectory(@"C:\temp\data");

// Clear with filter
DirectoryInfo dir = IOHelper.ClearDirectory(
    @"C:\temp\data",
    filter: path => Path.GetExtension(path) == ".tmp"
);

// Clear asynchronously
DirectoryInfo dir = await IOHelper.ClearDirectoryAsync(
    @"C:\temp\data",
    cancellationToken: cancellationToken
);

// Copy entire directory
IOHelper.CopyDirectory(@"C:\source", @"C:\destination");

// Copy asynchronously
await IOHelper.CopyDirectoryAsync(
    @"C:\source",
    @"C:\destination",
    cancellationToken
);

// Create temporary directory
string tempDir = IOHelper.CreateTempDir();
// Returns: C:\Users\...\AppData\Local\Temp\{GUID}

// Delete directory safely (no error if not exists)
@"C:\temp\data".SafeDeleteDir();

// Delete empty directories recursively
IOHelper.DeleteEmptyDirs(@"C:\project");

// Block until directory is deleted (useful for locked files)
bool stillExists = IOHelper.BlockDeleteDir(
    @"C:\temp\locked",
    isRecursive: true,
    iterCount: 1000,
    sleep: 10
);
```

#### File Operations

```csharp
using Ecng.Common;

// Copy file and make writable
string destFile = IOHelper.CopyAndMakeWritable(
    @"C:\source\file.txt",
    @"C:\destination"
);

// Create directory for file if needed
string filePath = @"C:\deep\nested\path\file.txt";
bool wasCreated = filePath.CreateDirIfNotExists();

// Create file with content
IOHelper.CreateFile(
    rootPath: @"C:\data",
    relativePath: "logs",
    fileName: "app.log",
    content: Encoding.UTF8.GetBytes("Log entry")
);

// Check if file is locked
bool isLocked = IOHelper.IsFileLocked(@"C:\data\file.dat");

// Get assembly/file timestamp
DateTime buildTime = IOHelper.GetTimestamp(@"C:\app\MyApp.exe");
DateTime asmTime = typeof(MyClass).Assembly.GetTimestamp();
```

#### Path Utilities

```csharp
using Ecng.Common;

// Convert to full path
string fullPath = @"relative\path\file.txt".ToFullPath();

// Add relative path segment
string combined = @"C:\base".AddRelative(@"subdir\file.txt");
// Returns: C:\base\subdir\file.txt

// Expand %Documents% variable
string path = @"%Documents%\MyApp\data.db".ToFullPathIfNeed();
// Returns: C:\Users\Username\Documents\MyApp\data.db

// Get relative path
string relative = @"C:\project\src\file.cs".GetRelativePath(@"C:\project");
// Returns: src\file.cs

// Normalize paths for comparison
string normalized = @"C:\Path\To\..\File.txt".NormalizePath();
string normalized2 = @"C:\path\to\..\file.txt".NormalizePath();

// Compare paths
bool areEqual = IOHelper.IsPathsEqual(
    @"C:\Path\To\File.txt",
    @"c:\path\to\file.txt"
); // true

// Check if path is directory
bool isDir = @"C:\Windows".IsDirectory();
bool isDir2 = @"C:\Windows".IsPathIsDir(); // alternative
```

#### Directory Queries

```csharp
using Ecng.Common;

// Get directories with pattern
IEnumerable<string> dirs = IOHelper.GetDirectories(
    @"C:\projects",
    searchPattern: "*.Tests",
    searchOption: SearchOption.AllDirectories
);

// Get directories asynchronously
IEnumerable<string> dirs = await IOHelper.GetDirectoriesAsync(
    @"C:\projects",
    searchPattern: "*",
    searchOption: SearchOption.TopDirectoryOnly,
    cancellationToken: cancellationToken
);

// Get files asynchronously
IEnumerable<string> files = await IOHelper.GetFilesAsync(
    @"C:\logs",
    searchPattern: "*.log",
    searchOption: SearchOption.AllDirectories,
    cancellationToken: cancellationToken
);

// Check if directory exists and has content
bool hasInstall = IOHelper.CheckInstallation(@"C:\Program Files\MyApp");

// Check if directory contains files (recursive)
bool hasFiles = IOHelper.CheckDirContainFiles(@"C:\temp");

// Check if directory contains anything
bool hasContent = IOHelper.CheckDirContainsAnything(@"C:\temp");

// Get disk free space
long freeSpace = IOHelper.GetDiskFreeSpace("C:");
Console.WriteLine($"Free: {freeSpace.ToHumanReadableFileSize()}");
```

### 5. Stream Operations

#### Reading from Streams

```csharp
using Ecng.Common;

// Read exact number of bytes
Stream stream = GetStream();
byte[] buffer = stream.ReadBuffer(size: 1024);

// Read bytes into buffer
byte[] data = new byte[512];
stream.ReadBytes(data, len: 256, pos: 0);

// Read bytes into Memory<byte>
Memory<byte> memory = new byte[512];
stream.ReadBytes(memory);

// Read full amount asynchronously (ensures all bytes are read)
byte[] buffer = new byte[1024];
int totalRead = await stream.ReadFullAsync(
    buffer,
    offset: 0,
    bytesToRead: 1024,
    cancellationToken
);

// Read typed data
int value = stream.Read<int>();
DateTime timestamp = stream.Read<DateTime>();
MyStruct data = stream.Read<MyStruct>();

// Read with dynamic size (for strings/arrays)
string text = (string)stream.Read(typeof(string));
byte[] bytes = (byte[])stream.Read(typeof(byte[]));

// Enumerate lines
foreach (string line in stream.EnumerateLines(Encoding.UTF8, leaveOpen: true))
{
    Console.WriteLine(line);
}
```

#### Writing to Streams

```csharp
using Ecng.Common;

Stream stream = GetStream();

// Write bytes
byte[] data = GetData();
stream.WriteBytes(data, len: data.Length, pos: 0);

// Write raw data
stream.WriteRaw(data);
stream.WriteRaw(myObject); // Converts object to bytes

// Write with length prefix
stream.WriteEx("Hello"); // Writes length + data
stream.WriteEx(new byte[] { 1, 2, 3 }); // Writes length + data
```

#### Stream Utilities

```csharp
using Ecng.Common;

// Save stream to file
stream.Save(@"C:\output\data.bin");

// Save byte array to file
byte[] data = GetData();
data.Save(@"C:\output\data.bin");

// Try save with error handling
bool success = data.TrySave(
    @"C:\output\data.bin",
    errorHandler: ex => Console.WriteLine($"Error: {ex.Message}")
);

// Get actual buffer from MemoryStream
var ms = new MemoryStream();
ms.Write(someData, 0, someData.Length);
ArraySegment<byte> segment = ms.GetActualBuffer();
// segment contains only written data, not entire buffer

// Truncate StreamWriter
using var writer = new StreamWriter(@"C:\log.txt");
writer.WriteLine("Entry 1");
writer.Truncate(); // Clears the file
writer.WriteLine("Entry 2");
```

### 6. Process Execution

```csharp
using Ecng.Common;
using System.Diagnostics;

// Execute process and capture output
int exitCode = IOHelper.Execute(
    fileName: "git",
    arg: "status",
    output: line => Console.WriteLine($"OUT: {line}"),
    error: line => Console.WriteLine($"ERR: {line}"),
    waitForExit: TimeSpan.FromSeconds(30)
);

// Execute with advanced options
int exitCode = IOHelper.Execute(
    fileName: "dotnet",
    arg: "build",
    output: line => LogOutput(line),
    error: line => LogError(line),
    infoHandler: info => {
        info.WorkingDirectory = @"C:\project";
        info.EnvironmentVariables["CUSTOM_VAR"] = "value";
    },
    stdInput: "input data",
    priority: ProcessPriorityClass.High
);

// Execute asynchronously
int exitCode = await IOHelper.ExecuteAsync(
    fileName: "npm",
    arg: "install",
    output: line => Console.WriteLine(line),
    error: line => Console.Error.WriteLine(line),
    priority: ProcessPriorityClass.BelowNormal,
    cancellationToken: cancellationToken
);
```

### 7. Utility Functions

```csharp
using Ecng.Common;

// Convert file size to human-readable format
long bytes = 1536000;
string size = bytes.ToHumanReadableFileSize();
// Returns: "1.5 MB"

// Get size of unmanaged type
int size = IOHelper.SizeOf<int>(); // 4
int size = IOHelper.SizeOf<DateTime>(); // 8 (stored as long)
int size = typeof(MyStruct).SizeOf();

// Open file or URL with default application
bool success = @"C:\document.pdf".OpenLink(raiseError: false);
bool success = "https://example.com".OpenLink(raiseError: true);
```

## Buffer Sizes

The library uses a default buffer size for compression operations:

```csharp
// Default buffer size: 80 KB
const int bufferSize = CompressionHelper.DefaultBufferSize; // 81920 bytes
```

You can customize buffer sizes for performance tuning:

```csharp
// Use smaller buffer for memory-constrained environments
byte[] compressed = data.Compress<GZipStream>(bufferSize: 4096);

// Use larger buffer for better throughput
byte[] compressed = data.Compress<GZipStream>(bufferSize: 1024 * 1024);
```

## Error Handling

All methods throw standard .NET exceptions:

- `ArgumentNullException` - When required parameters are null
- `ArgumentOutOfRangeException` - When sizes or indices are invalid
- `IOException` - When I/O operations fail
- `UnauthorizedAccessException` - When access is denied
- `DirectoryNotFoundException` / `FileNotFoundException` - When paths don't exist

```csharp
try
{
    var data = compressedData.UnGZip();
}
catch (ArgumentNullException ex)
{
    // Input was null
}
catch (IOException ex)
{
    // Decompression failed
}
```

## Thread Safety

- `CompressionHelper` methods are thread-safe as they operate on method parameters
- Stream operations are not inherently thread-safe - synchronize access if needed
- File operations should be synchronized when accessing the same files from multiple threads
- Async methods support `CancellationToken` for cooperative cancellation

## Performance Tips

1. **Use async methods** for I/O-bound operations to avoid blocking threads
2. **Use Span/Memory APIs** on .NET 6.0+ for better performance and less allocation
3. **Choose appropriate compression levels**:
   - `CompressionLevel.Fastest` - Quick but larger files
   - `CompressionLevel.Optimal` - Balanced (default)
   - `CompressionLevel.SmallestSize` - Slow but smallest files (.NET 7+)
4. **Adjust buffer sizes** based on your data and memory constraints
5. **Use Fossil Delta** for incremental updates instead of full file transfers

## Examples

### Example 1: Compress and Save File

```csharp
using Ecng.IO;
using System.IO.Compression;

// Read, compress, and save
byte[] original = File.ReadAllBytes(@"C:\data\large-file.json");
byte[] compressed = original.Compress<GZipStream>(
    level: CompressionLevel.Optimal
);
compressed.Save(@"C:\data\large-file.json.gz");

Console.WriteLine($"Original: {original.Length.ToHumanReadableFileSize()}");
Console.WriteLine($"Compressed: {compressed.Length.ToHumanReadableFileSize()}");
Console.WriteLine($"Ratio: {(100.0 * compressed.Length / original.Length):F1}%");
```

### Example 2: Process ZIP Archive

```csharp
using Ecng.IO;

byte[] zipData = File.ReadAllBytes(@"C:\downloads\archive.zip");

using (var entries = zipData.Unzip(filter: name => name.EndsWith(".csv")))
{
    foreach (var (fileName, stream) in entries)
    {
        Console.WriteLine($"Processing: {fileName}");

        using var reader = new StreamReader(stream);
        string csvContent = reader.ReadToEnd();

        // Process CSV...
        ProcessCsvData(csvContent);
    }
}
```

### Example 3: Incremental Binary Updates with Fossil Delta

```csharp
using Ecng.IO.Fossil;

// Server side: Create delta
byte[] v1 = File.ReadAllBytes(@"app-v1.0.exe");
byte[] v2 = File.ReadAllBytes(@"app-v2.0.exe");
byte[] delta = await Delta.Create(v1, v2, CancellationToken.None);

// Upload only the delta (much smaller than full v2)
await UploadToServer("update-1.0-to-2.0.delta", delta);

// Client side: Apply delta
byte[] currentVersion = File.ReadAllBytes(@"app.exe");
byte[] deltaUpdate = await DownloadFromServer("update-1.0-to-2.0.delta");
byte[] newVersion = await Delta.Apply(currentVersion, deltaUpdate, CancellationToken.None);

File.WriteAllBytes(@"app.exe", newVersion);
```

### Example 4: Async Directory Processing

```csharp
using Ecng.Common;

// Find all log files and compress them
var logFiles = await IOHelper.GetFilesAsync(
    @"C:\logs",
    searchPattern: "*.log",
    searchOption: SearchOption.AllDirectories,
    cancellationToken: cancellationToken
);

foreach (string logFile in logFiles)
{
    byte[] content = await File.ReadAllBytesAsync(logFile, cancellationToken);
    byte[] compressed = await content.CompressAsync<GZipStream>(
        cancellationToken: cancellationToken
    );

    await File.WriteAllBytesAsync(
        logFile + ".gz",
        compressed,
        cancellationToken
    );

    Console.WriteLine($"Compressed: {logFile}");
}
```

### Example 5: Stream Processing Pipeline

```csharp
using Ecng.IO;
using Ecng.Common;
using System.IO.Compression;

// Create a processing pipeline
await using var inputFile = File.OpenRead(@"C:\data\input.txt");
await using var compressed = new MemoryStream();
await using var encrypted = new MemoryStream();

// Compress
await inputFile.CompressAsync<GZipStream>(
    output: compressed,
    level: CompressionLevel.Optimal,
    leaveOpen: true
);

compressed.Position = 0;

// Further processing...
// (e.g., encrypt, hash, etc.)

// Save final result
compressed.Position = 0;
await using var output = File.Create(@"C:\data\output.bin");
await compressed.CopyToAsync(output);
```

## License

Copyright © StockSharp 2010-2025

## Related Libraries

- **Ecng.Common** - Core utilities and extensions
- **Ecng.Lzma** - LZMA compression implementation
- **Ecng.Serialization** - Serialization utilities

## Support

For issues and questions, please visit the StockSharp repository or documentation.
