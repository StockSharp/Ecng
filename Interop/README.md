# Ecng.Interop

A comprehensive .NET library for Platform/Invoke (P/Invoke) operations and native code interoperability. This library provides safe, easy-to-use wrappers for working with unmanaged memory, native libraries, and low-level data structures.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Installation](#installation)
- [Core Components](#core-components)
  - [Dynamic Library Loading](#dynamic-library-loading)
  - [Memory Management](#memory-management)
  - [Pointer Utilities](#pointer-utilities)
  - [String Marshaling](#string-marshaling)
  - [Type Marshaling](#type-marshaling)
  - [Fixed-Size Strings](#fixed-size-strings)
  - [Time Structures](#time-structures)
  - [Hardware Information](#hardware-information)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Platform Support](#platform-support)

## Overview

Ecng.Interop simplifies the complexity of interoperating with native code by providing:

- Type-safe wrappers for unmanaged memory operations
- Automatic memory management with safe handles
- Helper methods for common marshaling scenarios
- Optimized fixed-size string types for performance-critical interop scenarios
- Utilities for loading and calling native libraries dynamically

## Key Features

- **Safe Memory Management**: RAII-style memory management with `HGlobalSafeHandle` and `SafePointer`
- **Dynamic Library Loading**: Load and call functions from native DLLs at runtime
- **String Marshaling**: Support for ANSI, Unicode, UTF-8, and BSTR string formats
- **Fixed-Size Strings**: Pre-defined fixed-size string types (ASCII and UTF-8) for efficient marshaling
- **Pointer Reading/Writing**: Sequential pointer reading with `PtrReader` and type-safe pointer operations
- **Blittable Types**: Specialized types like `BlittableDecimal` for direct memory layout compatibility
- **Time Structures**: Compact time representations optimized for interop scenarios
- **Cross-Platform**: Works on Windows, Linux, and macOS

## Installation

Add a reference to the `Ecng.Interop` project or include the compiled DLL in your project.

```xml
<ProjectReference Include="path\to\Ecng.Interop\Interop.csproj" />
```

## Core Components

### Dynamic Library Loading

#### Using DllLibrary Base Class

The `DllLibrary` class provides a managed way to load native libraries and access their functions.

```csharp
using Ecng.Interop;

// Define your library wrapper
public class MyNativeLibrary : DllLibrary
{
    public MyNativeLibrary(string dllPath) : base(dllPath)
    {
        // Retrieve function pointers as delegates
        Add = GetHandler<AddDelegate>("Add");
        Multiply = TryGetHandler<MultiplyDelegate>("Multiply"); // Returns null if not found
    }

    // Define delegate types matching native function signatures
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AddDelegate(int a, int b);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MultiplyDelegate(int a, int b);

    // Public wrapper methods
    public Func<int, int, int> Add { get; }
    public Func<int, int, int> Multiply { get; }
}

// Usage
using (var lib = new MyNativeLibrary(@"C:\path\to\native.dll"))
{
    int result = lib.Add(10, 20); // Returns 30
    Console.WriteLine($"Result: {result}");

    // Check DLL version
    Console.WriteLine($"DLL Version: {lib.DllVersion}");
}
```

#### Manual Library Loading

```csharp
using Ecng.Interop;

// Load library manually
IntPtr libraryHandle = Marshaler.LoadLibrary(@"C:\path\to\library.dll");

try
{
    // Get function pointer
    IntPtr funcPtr = libraryHandle.GetProcAddress("MyFunction");

    // Convert to delegate
    var myFunc = funcPtr.GetDelegateForFunctionPointer<MyFunctionDelegate>();

    // Call the function
    int result = myFunc(42);
}
finally
{
    // Free the library
    libraryHandle.FreeLibrary();
}
```

### Memory Management

#### HGlobalSafeHandle

`HGlobalSafeHandle` provides automatic cleanup of unmanaged memory allocated with `Marshal.AllocHGlobal`.

```csharp
using Ecng.Interop;

// Allocate 1024 bytes of unmanaged memory
using (var handle = 1024.ToHGlobal())
{
    IntPtr ptr = handle.DangerousGetHandle();

    // Use the memory
    Marshal.WriteInt32(ptr, 42);
    int value = Marshal.ReadInt32(ptr);

    Console.WriteLine($"Value: {value}"); // Output: Value: 42
}
// Memory is automatically freed when disposed

// Allocate and write a string
using (var handle = Encoding.UTF8.ToHGlobal("Hello, World!"))
{
    IntPtr ptr = handle.DangerousGetHandle();
    string decoded = Encoding.UTF8.ToString(ptr);
    Console.WriteLine(decoded); // Output: Hello, World!
}
```

#### SafePointer

`SafePointer` wraps an unmanaged pointer with bounds checking and automatic shifting.

```csharp
using Ecng.Interop;

// Allocate memory
IntPtr memory = Marshal.AllocHGlobal(100);
try
{
    // Create a SafePointer with size boundary
    var safePtr = new SafePointer(memory, 100);

    // Read a value and auto-shift the pointer
    int value1 = safePtr.Read<int>(autoShift: true);
    int value2 = safePtr.Read<int>(autoShift: true);

    // Read a structure
    MyStruct myStruct = safePtr.ToStruct<MyStruct>(autoShift: true);

    // Copy to byte array
    byte[] buffer = new byte[20];
    safePtr.CopyTo(buffer, autoShift: true);

    // Manual shifting
    safePtr.Shift<long>(); // Shift by sizeof(long)
    safePtr.Shift(16);     // Shift by 16 bytes
}
finally
{
    Marshal.FreeHGlobal(memory);
}
```

#### GCHandle<T>

Generic wrapper around `GCHandle` for pinning managed objects.

```csharp
using Ecng.Interop;

byte[] data = new byte[] { 1, 2, 3, 4, 5 };

// Pin the array in memory
using (var gcHandle = new GCHandle<byte[]>(data, GCHandleType.Pinned))
{
    // Create a safe pointer to the pinned data
    SafePointer pointer = gcHandle.CreatePointer();

    // Pass pointer to native code
    NativeFunction(pointer.Pointer);
}
// Array is automatically unpinned when disposed
```

### Pointer Utilities

#### PtrReader

Sequential reading from unmanaged memory pointers.

```csharp
using Ecng.Interop;

IntPtr dataPtr = GetSomeNativeData();

var reader = new PtrReader(dataPtr);

// Read various types sequentially
byte b = reader.GetByte();
short s = reader.GetShort();
int i = reader.GetInt();
long l = reader.GetLong();
IntPtr p = reader.GetIntPtr();

// Read null-terminated strings
string str1 = reader.GetString();

// Read fixed-length strings
string str2 = reader.GetString(20); // Read 20 characters
```

#### Direct Pointer Operations

```csharp
using Ecng.Interop;

IntPtr ptr = Marshal.AllocHGlobal(100);
try
{
    // Write values
    ptr.Write<int>(42);
    (ptr + 4).Write<short>(100);
    (ptr + 6).Write<byte>(255);

    // Read values
    int intValue = ptr.Read<int>();
    short shortValue = (ptr + 4).Read<short>();
    byte byteValue = (ptr + 6).Read<byte>();

    // Copy to managed array
    byte[] buffer = new byte[10];
    ptr.CopyTo(buffer);
    ptr.CopyTo(buffer, offset: 0, length: 10);

    // Create spans (modern .NET)
    Span<byte> span = ptr.ToSpan(100);
    ReadOnlySpan<byte> roSpan = ptr.ToReadOnlySpan(100);
}
finally
{
    ptr.FreeHGlobal();
}
```

### String Marshaling

The library provides comprehensive string marshaling for different encodings.

```csharp
using Ecng.Interop;

// ANSI strings
string text = "Hello, World!";
IntPtr ansiPtr = text.FromAnsi();
try
{
    string decoded = ansiPtr.ToAnsi();
    Console.WriteLine(decoded);
}
finally
{
    Marshal.FreeHGlobal(ansiPtr);
}

// Unicode strings
IntPtr unicodePtr = text.FromUnicode();
try
{
    string decoded = unicodePtr.ToUnicode();
    Console.WriteLine(decoded);
}
finally
{
    Marshal.FreeHGlobal(unicodePtr);
}

// Platform-dependent (Auto)
IntPtr autoPtr = text.FromAuto();
try
{
    string decoded = autoPtr.ToAuto();
    Console.WriteLine(decoded);
}
finally
{
    Marshal.FreeHGlobal(autoPtr);
}

// BSTR (COM interop)
IntPtr bstrPtr = text.FromBSTR();
try
{
    string decoded = bstrPtr.ToBSTR();
    Console.WriteLine(decoded);
}
finally
{
    Marshal.FreeBSTR(bstrPtr);
}

// UTF-8 with unsafe pointers
unsafe
{
    byte* utf8Buffer = stackalloc byte[100];
    text.ToUtf8(utf8Buffer, 100);

    string decoded = 100.ToUtf8(utf8Buffer);
    Console.WriteLine(decoded);
}

// ASCII with unsafe pointers
unsafe
{
    byte* asciiBuffer = stackalloc byte[100];
    text.ToAscii(asciiBuffer, 100);

    string decoded = 100.ToAscii(asciiBuffer);
    Console.WriteLine(decoded);
}
```

### Type Marshaling

#### Structure Marshaling

```csharp
using Ecng.Interop;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct MyNativeStruct
{
    public int Id;
    public double Value;
    public byte Flag;
}

// Marshal from structure to pointer
MyNativeStruct data = new MyNativeStruct
{
    Id = 1,
    Value = 3.14,
    Flag = 1
};

IntPtr ptr = data.StructToPtr();
try
{
    // Pass ptr to native code
    NativeFunction(ptr);

    // Marshal back from pointer to structure
    MyNativeStruct result = ptr.ToStruct<MyNativeStruct>();
    Console.WriteLine($"ID: {result.Id}, Value: {result.Value}");
}
finally
{
    Marshal.FreeHGlobal(ptr);
}

// Get pointer and size
(IntPtr ptr, int size) = data.StructToPtrEx();
try
{
    Console.WriteLine($"Structure size: {size} bytes");
    // Use ptr and size
}
finally
{
    Marshal.FreeHGlobal(ptr);
}
```

#### BlittableDecimal

`BlittableDecimal` is a struct that matches the memory layout of `decimal` for direct marshaling.

```csharp
using Ecng.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PriceData
{
    public int Quantity;
    public BlittableDecimal Price; // Can be marshaled directly
}

PriceData data = new PriceData
{
    Quantity = 100,
    Price = (BlittableDecimal)123.45m
};

// Marshal to unmanaged memory
IntPtr ptr = data.StructToPtr();
try
{
    // Pass to native code
    NativeFunction(ptr);

    // Read back
    PriceData result = ptr.ToStruct<PriceData>();
    decimal price = result.Price; // Implicit conversion
    Console.WriteLine($"Quantity: {result.Quantity}, Price: {price}");
}
finally
{
    Marshal.FreeHGlobal(ptr);
}
```

### Fixed-Size Strings

Fixed-size string types provide efficient marshaling for scenarios where string length is known at compile time.

#### UTF-8 Fixed Strings

```csharp
using Ecng.Interop;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct NetworkPacket
{
    public int PacketId;
    public Utf8String16 Symbol;    // 16 bytes
    public Utf8String32 Message;   // 32 bytes
    public Utf8String8 Source;     // 8 bytes
}

// Usage
NetworkPacket packet = new NetworkPacket
{
    PacketId = 123,
    Symbol = (Utf8String16)"AAPL",
    Message = (Utf8String32)"Order executed",
    Source = (Utf8String8)"NYSE"
};

// Convert to strings
string symbol = packet.Symbol;   // Implicit conversion
string message = packet.Message;
string source = packet.Source.ToString();

Console.WriteLine($"Symbol: {symbol}, Message: {message}");

// Available UTF-8 sizes: 1-33, 48, 64, 65, 128, 129, 256
```

#### ASCII Fixed Strings

```csharp
using Ecng.Interop;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct LegacyRecord
{
    public int RecordId;
    public AsciiString32 Name;     // 32 bytes ASCII
    public AsciiString64 Address;  // 64 bytes ASCII
    public AsciiString16 City;     // 16 bytes ASCII
}

// Usage
LegacyRecord record = new LegacyRecord
{
    RecordId = 1,
    Name = (AsciiString32)"John Doe",
    Address = (AsciiString64)"123 Main Street",
    City = (AsciiString16)"New York"
};

// Convert to strings
string name = record.Name;      // Implicit conversion
string address = record.Address;
string city = record.City.ToString();

Console.WriteLine($"{name} from {city}");

// Available ASCII sizes: 1-32, 64, 128
```

### Time Structures

Compact time representations optimized for native interop.

#### Time4Sec (4-byte time with second resolution)

```csharp
using Ecng.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct LogEntry
{
    public Time4Sec Timestamp;  // 4 bytes instead of 8
    public int EventId;
}

// Usage
LogEntry entry = new LogEntry
{
    Timestamp = (Time4Sec)DateTime.UtcNow,
    EventId = 123
};

// Convert to DateTime
DateTime dt = entry.Timestamp;  // Implicit conversion
DateTimeOffset dto = entry.Timestamp;

Console.WriteLine($"Event at: {dt}");
Console.WriteLine($"Formatted: {entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", null)}");
```

#### Time8Mls (8-byte time with millisecond resolution)

```csharp
using Ecng.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct TradeData
{
    public Time8Mls ExecutionTime;
    public double Price;
    public int Volume;
}

// Usage
TradeData trade = new TradeData
{
    ExecutionTime = (Time8Mls)DateTime.UtcNow,
    Price = 150.25,
    Volume = 1000
};

DateTime executionTime = trade.ExecutionTime;
Console.WriteLine($"Trade executed at: {executionTime:yyyy-MM-dd HH:mm:ss.fff}");
```

#### Time8Mcs (8-byte time with microsecond resolution)

```csharp
using Ecng.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct HighFrequencyTick
{
    public Time8Mcs Timestamp;
    public double BidPrice;
    public double AskPrice;
}

// Usage
HighFrequencyTick tick = new HighFrequencyTick
{
    Timestamp = (Time8Mcs)DateTime.UtcNow,
    BidPrice = 100.50,
    AskPrice = 100.51
};

DateTime tickTime = tick.Timestamp;
Console.WriteLine($"Tick at: {tickTime:HH:mm:ss.ffffff}");
```

#### TimeNano (nanosecond resolution)

```csharp
using Ecng.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PrecisionEvent
{
    public TimeNano Timestamp;
    public int EventType;
}

// Usage
PrecisionEvent evt = new PrecisionEvent
{
    Timestamp = (TimeNano)DateTime.UtcNow,
    EventType = 5
};

DateTime eventTime = evt.Timestamp;
Console.WriteLine($"Precise event time: {eventTime:O}");
```

### Hardware Information

Generate hardware-based identifiers for licensing or device identification.

```csharp
using Ecng.Interop;

// Synchronous
string hardwareId = HardwareInfo.GetId();
Console.WriteLine($"Hardware ID: {hardwareId}");

// Asynchronous
string hardwareIdAsync = await HardwareInfo.GetIdAsync();
Console.WriteLine($"Hardware ID: {hardwareIdAsync}");

// With cancellation
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    string id = await HardwareInfo.GetIdAsync(cts.Token);
    Console.WriteLine($"Hardware ID: {id}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Hardware ID retrieval timed out");
}
```

The hardware ID is generated based on:
- **Windows**: CPU ID + Motherboard Serial Number (or MAC Address)
- **Linux**: Root partition UUID
- **macOS**: Platform UUID

## Usage Examples

### Complete Example: Calling Native Library

```csharp
using Ecng.Interop;
using System.Runtime.InteropServices;

// Define the native structure
[StructLayout(LayoutKind.Sequential)]
public struct Point3D
{
    public double X;
    public double Y;
    public double Z;
}

// Create library wrapper
public class MathLibrary : DllLibrary
{
    public MathLibrary() : base("mathlib.dll")
    {
        CalculateDistance = GetHandler<CalculateDistanceDelegate>("CalculateDistance");
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate double CalculateDistanceDelegate(IntPtr point1, IntPtr point2);

    private CalculateDistanceDelegate CalculateDistance;

    public double GetDistance(Point3D p1, Point3D p2)
    {
        IntPtr ptr1 = p1.StructToPtr();
        IntPtr ptr2 = p2.StructToPtr();
        try
        {
            return CalculateDistance(ptr1, ptr2);
        }
        finally
        {
            ptr1.FreeHGlobal();
            ptr2.FreeHGlobal();
        }
    }
}

// Usage
using (var lib = new MathLibrary())
{
    Point3D p1 = new Point3D { X = 0, Y = 0, Z = 0 };
    Point3D p2 = new Point3D { X = 3, Y = 4, Z = 0 };

    double distance = lib.GetDistance(p1, p2);
    Console.WriteLine($"Distance: {distance}"); // Output: Distance: 5
}
```

### Complete Example: Working with Native Arrays

```csharp
using Ecng.Interop;
using System.Runtime.InteropServices;

// Allocate array in unmanaged memory
int[] numbers = new int[] { 1, 2, 3, 4, 5 };
int sizeInBytes = sizeof(int) * numbers.Length;

using (var handle = sizeInBytes.ToHGlobal())
{
    IntPtr ptr = handle.DangerousGetHandle();

    // Copy managed array to unmanaged memory
    Marshal.Copy(numbers, 0, ptr, numbers.Length);

    // Create SafePointer for safe iteration
    var safePtr = new SafePointer(ptr, sizeInBytes);

    // Read values
    for (int i = 0; i < numbers.Length; i++)
    {
        int value = safePtr.Read<int>(autoShift: true);
        Console.WriteLine($"Value {i}: {value}");
    }
}
```

### Complete Example: Reading Native Structure with Strings

```csharp
using Ecng.Interop;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UserData
{
    public int UserId;
    public AsciiString32 UserName;
    public AsciiString64 Email;
    public Time8Mls RegistrationDate;
}

// Read from native memory
IntPtr nativeDataPtr = GetUserDataFromNativeCode();

UserData user = nativeDataPtr.ToStruct<UserData>();

Console.WriteLine($"User ID: {user.UserId}");
Console.WriteLine($"Name: {(string)user.UserName}");
Console.WriteLine($"Email: {(string)user.Email}");
Console.WriteLine($"Registered: {(DateTime)user.RegistrationDate}");
```

## Best Practices

### Memory Management

1. **Always dispose resources**: Use `using` statements for `DllLibrary`, `HGlobalSafeHandle`, and `GCHandle<T>`
2. **Prefer safe wrappers**: Use `SafePointer` over raw `IntPtr` when possible for bounds checking
3. **Match allocation/deallocation**: Use `FreeHGlobal()` for memory allocated with `AllocHGlobal()`

```csharp
// Good
using (var handle = 1024.ToHGlobal())
{
    // Use memory
} // Automatically freed

// Avoid
IntPtr ptr = Marshal.AllocHGlobal(1024);
// ... might forget to free
```

### String Marshaling

1. **Choose the right encoding**: Use UTF-8 for modern APIs, ASCII for legacy systems
2. **Use fixed-size strings** for structures: More efficient than string marshaling
3. **Be aware of null terminators**: ANSI/Unicode strings are null-terminated

```csharp
// Good for structures
[StructLayout(LayoutKind.Sequential)]
public struct Config
{
    public Utf8String32 Name;  // Fixed size, no allocation
}

// Avoid for structures (requires marshaling)
[StructLayout(LayoutKind.Sequential)]
public struct ConfigBad
{
    [MarshalAs(UnmanagedType.LPStr)]
    public string Name;  // Requires allocation and marshaling
}
```

### Platform Considerations

1. **Check platform**: Use `OperatingSystem` checks when necessary
2. **Handle pointer size**: Use `IntPtr.Size` for platform-dependent sizes
3. **Test on target platforms**: Marshaling behavior can differ between platforms

```csharp
if (OperatingSystem.IsWindows())
{
    // Windows-specific code
}
else if (OperatingSystem.IsLinux())
{
    // Linux-specific code
}
```

### Performance

1. **Pin arrays for bulk operations**: Use `GCHandle<T>` to avoid copying
2. **Use stackalloc for small buffers**: Avoid heap allocation when possible
3. **Batch operations**: Minimize transitions between managed and unmanaged code

```csharp
// Good: Single pinning for bulk operation
byte[] data = new byte[1000];
using (var handle = new GCHandle<byte[]>(data, GCHandleType.Pinned))
{
    SafePointer ptr = handle.CreatePointer();
    NativeBulkOperation(ptr.Pointer, data.Length);
}

// Avoid: Multiple small transitions
for (int i = 0; i < 1000; i++)
{
    NativeSingleOperation(data[i]); // Many transitions
}
```

## Platform Support

- **.NET Standard 2.0**: Compatible with .NET Framework 4.6.1+ and .NET Core 2.0+
- **.NET 6.0**: Full support
- **.NET 10.0**: Full support with latest features
- **Operating Systems**: Windows, Linux, macOS

## Dependencies

- **Ecng.Common**: Core utilities
- **WmiLight**: Windows Management Instrumentation for hardware info
- **Microsoft.Windows.CsWin32**: Windows API source generator

## Thread Safety

- Most classes are **not thread-safe** by default
- `DllLibrary` instances should not be shared between threads without synchronization
- Memory allocation/deallocation is thread-safe (handled by the runtime)

## License

This library is part of the StockSharp/Ecng project.
