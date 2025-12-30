# Ecng.Common

Core utilities and extension methods for everyday .NET development. Includes string helpers, type conversion, time utilities, and more.

## String Utilities

### Basic String Operations

```csharp
using Ecng.Common;

// Check for null or empty
string text = GetText();
if (text.IsEmpty())
    return;

// Check for null, empty, or whitespace
if (text.IsEmptyOrWhiteSpace())
    return;

// Default value if empty
string value = text.IsEmpty("default value");

// Throw if empty
string required = input.ThrowIfEmpty(nameof(input));

// String formatting
string result = "{0} + {1} = {2}".Put(1, 2, 3);  // "1 + 2 = 3"

// Smart formatting with named parameters
string smart = "Hello {Name}!".PutEx(new { Name = "World" });
```

### String Manipulation

```csharp
// Join with separator
var items = new[] { "a", "b", "c" };
string joined = items.Join(", ");  // "a, b, c"

// Split by line separators (handles \r\n, \n, \r)
string[] lines = "line1\nline2\r\nline3".SplitByLineSeps();

// Case-insensitive comparison
bool equal = "ABC".EqualsIgnoreCase("abc");  // true
bool contains = "Hello World".ContainsIgnoreCase("world");  // true

// Remove characters
string cleaned = "hello123".Remove("123");  // "hello"

// Secure strings
SecureString secure = "password".Secure();
string plain = secure.UnSecure();
```

### Validation

```csharp
// Email validation
bool isEmail = "user@example.com".IsValidEmailAddress();

// URL validation
bool isUrl = "https://example.com".IsValidUrl();
```

## Type Conversion

The `Converter` class provides flexible type conversion between many types.

### Basic Conversion

```csharp
// String to primitive types
int number = "42".To<int>();
double value = "3.14".To<double>();
bool flag = "true".To<bool>();
DateTime date = "2024-01-15".To<DateTime>();
Guid id = "550e8400-e29b-41d4-a716-446655440000".To<Guid>();

// With default value on failure
int safe = "invalid".To(defaultValue: 0);

// Between types
byte[] bytes = 12345.To<byte[]>();
long ticks = DateTime.Now.To<long>();
```

### Network Types

```csharp
// IP Address conversions
IPAddress ip = "192.168.1.1".To<IPAddress>();
string ipStr = ip.To<string>();
byte[] ipBytes = ip.To<byte[]>();
long ipLong = ip.To<long>();

// Endpoints
EndPoint endpoint = "192.168.1.1:8080".To<EndPoint>();
IPEndPoint ipEndpoint = "192.168.1.1:8080".To<IPEndPoint>();
DnsEndPoint dnsEndpoint = "example.com:443".To<DnsEndPoint>();
```

### Custom Converters

```csharp
// Register custom converter
Converter.AddTypedConverter<MyType, string>(obj => obj.ToString());
Converter.AddTypedConverter<string, MyType>(s => MyType.Parse(s));

// Use typed conversion
string str = myObject.TypedTo<MyType, string>();
```

## CSV Parsing

### FastCsvReader

High-performance, allocation-free CSV parser.

```csharp
string csv = "Id;Name;Value\n1;Foo;100\n2;Bar;200";
var reader = new FastCsvReader(csv, ";");

while (reader.NextLine())
{
    int id = reader.ReadInt();
    string name = reader.ReadString();
    decimal value = reader.ReadDecimal();

    Console.WriteLine($"{id}: {name} = {value}");
}
```

### Reading Different Types

```csharp
var reader = new FastCsvReader(data, ",");

while (reader.NextLine())
{
    // Primitives
    int intVal = reader.ReadInt();
    long longVal = reader.ReadLong();
    double doubleVal = reader.ReadDouble();
    decimal decimalVal = reader.ReadDecimal();
    bool boolVal = reader.ReadBool();

    // Nullable types
    int? nullableInt = reader.ReadNullableInt();

    // Date/Time
    DateTime date = reader.ReadDateTime("yyyy-MM-dd");
    TimeSpan time = reader.ReadTimeSpan();

    // Enum
    MyEnum enumVal = reader.ReadEnum<MyEnum>();

    // Skip column
    reader.Skip();
}
```

## Time Utilities

### High-Precision Time

```csharp
using Ecng.Common;

// High-precision current time (uses Stopwatch internally)
DateTime now = TimeHelper.Now;
DateTimeOffset nowWithOffset = TimeHelper.NowWithOffset;

// Adjust time offset (for testing or sync)
TimeHelper.NowOffset = TimeSpan.FromSeconds(5);

// Sync with NTP server
TimeHelper.SyncMarketTime(timeout: 5000);
```

### TimeSpan Extensions

```csharp
TimeSpan span = TimeSpan.FromDays(365);

double weeks = span.TotalWeeks();
double months = span.TotalMonths();
double years = span.TotalYears();

// Constants
long ticksPerWeek = TimeHelper.TicksPerWeek;
long ticksPerMonth = TimeHelper.TicksPerMonth;
long ticksPerYear = TimeHelper.TicksPerYear;

// Predefined spans
TimeSpan oneMinute = TimeHelper.Minute1;
TimeSpan fiveMinutes = TimeHelper.Minute5;
TimeSpan oneHour = TimeHelper.Hour1;
```

### DateTime Extensions

```csharp
DateTime dt = DateTime.Now;

// Truncation
DateTime dateOnly = dt.Truncate(TimeSpan.FromDays(1));
DateTime hourOnly = dt.Truncate(TimeSpan.FromHours(1));

// Apply timezone
DateTimeOffset local = dt.ApplyLocal();
DateTimeOffset utc = dt.ApplyUtc();
DateTimeOffset custom = dt.ApplyTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

// Async delay
await TimeSpan.FromSeconds(1).Delay(cancellationToken);
```

## I/O Utilities

### File Operations

```csharp
using Ecng.Common;

// Safe file operations
string content = IOHelper.ReadFile("path/to/file.txt");
IOHelper.WriteFile("path/to/file.txt", content);

// Atomic file write (writes to temp, then renames)
IOHelper.AtomicWriteFile("path/to/file.txt", content);

// Get relative path
string relative = IOHelper.GetRelativePath(basePath, fullPath);
```

### Stream Extensions

```csharp
// Read all bytes
byte[] data = stream.ReadToEnd();

// Copy with progress
await source.CopyToAsync(destination, progress: bytesWritten =>
{
    Console.WriteLine($"Written: {bytesWritten}");
});
```

## Math Utilities

```csharp
using Ecng.Common;

// Rounding
double rounded = 3.7.Round();      // 4
double ceiling = 3.1.Ceiling();    // 4
double floor = 3.9.Floor();        // 3

// Clamping
int clamped = 150.Max(100);        // 100
int clamped2 = 50.Min(100);        // 100

// Abs
int absolute = (-5).Abs();         // 5

// Percentage
decimal pct = 250m.Percent(1000m); // 25
```

## Random Generation

```csharp
using Ecng.Common;

// Random values
int randomInt = RandomGen.GetInt(1, 100);
double randomDouble = RandomGen.GetDouble();
bool randomBool = RandomGen.GetBool();

// Random bytes
byte[] randomBytes = RandomGen.GetBytes(32);

// Random string
string randomStr = RandomGen.GetString(16);
```

## Disposable Helpers

### Base Disposable Class

```csharp
public class MyResource : Disposable
{
    private IntPtr _handle;

    protected override void DisposeManaged()
    {
        // Clean up managed resources
        base.DisposeManaged();
    }

    protected override void DisposeNative()
    {
        // Clean up native resources
        CloseHandle(_handle);
        base.DisposeNative();
    }
}
```

### Disposable Scope

```csharp
// Dispose multiple objects at once
using var scope = new DisposeScope(resource1, resource2, resource3);

// Or with extension
resource1.DisposeWith(scope);
```

## File System Abstraction

### IFileSystem Interface

```csharp
// Local file system
IFileSystem fs = new LocalFileSystem();

// In-memory file system (for testing)
IFileSystem memFs = new MemoryFileSystem();

// Operations
bool exists = fs.FileExists("path/to/file");
byte[] data = fs.ReadAllBytes("path/to/file");
fs.WriteAllBytes("path/to/file", data);
fs.CreateDirectory("path/to/dir");
IEnumerable<string> files = fs.GetFiles("path", "*.txt");
```

## Currency Support

```csharp
// Currency types
CurrencyTypes usd = CurrencyTypes.USD;
CurrencyTypes eur = CurrencyTypes.EUR;

// Currency operations
Currency amount = new Currency(100, CurrencyTypes.USD);
string display = amount.ToString(); // "$100.00"
```

## Cloning

```csharp
// Deep clone
var clone = original.Clone();

// Typed clone
public class MyClass : Cloneable<MyClass>
{
    public override MyClass Clone()
    {
        return new MyClass { /* copy properties */ };
    }
}
```

## Watch (Benchmarking)

```csharp
using var watch = new Watch("Operation name");

// Do work...

// Automatically logs elapsed time on dispose
// Or get elapsed manually
TimeSpan elapsed = watch.Elapsed;
```

## NuGet

```
Install-Package Ecng.Common
```
