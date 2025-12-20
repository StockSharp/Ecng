# SmartFormat - Advanced String Formatting Library

SmartFormat is a lightweight, extensible string formatting library that extends the capabilities of `string.Format`. It provides powerful templating features including conditional formatting, pluralization, list formatting, and more.

## Features

- All features of `string.Format` plus advanced extensions
- Conditional formatting (if/else logic)
- Pluralization and localization support
- List formatting with customizable separators
- Time and TimeSpan formatting
- Nested object property access
- Dictionary and JSON source support
- XML/XElement support
- Template-based formatting
- Extensible architecture with custom formatters and sources
- High performance with caching support

## Installation

Add a reference to the SmartFormat project in your solution.

## Quick Start

### Basic Usage

```csharp
using SmartFormat;

// Simple formatting (like string.Format)
string result = Smart.Format("Hello {0}!", "World");
// Output: Hello World!

// Named placeholders
string result = Smart.Format("Hello {Name}!", new { Name = "Alice" });
// Output: Hello Alice!
```

### Object Property Access

```csharp
var person = new { Name = "John", Age = 30, Address = new { City = "New York" } };

string result = Smart.Format("{Name} is {Age} years old and lives in {Address.City}.", person);
// Output: John is 30 years old and lives in New York.
```

## Core Formatters

### Conditional Formatter

Format strings based on conditions:

```csharp
// Syntax: {value:condition(true-text|false-text)}
string result = Smart.Format("{0:cond:>=18?Adult|Minor}", 25);
// Output: Adult

string result = Smart.Format("{0:cond:>0?Positive|Zero or Negative}", -5);
// Output: Zero or Negative
```

### Plural Formatter

Automatically handles pluralization:

```csharp
// Syntax: {value:plural:singular|plural}
string result = Smart.Format("You have {Count:plural:one item|{} items}.", new { Count = 1 });
// Output: You have one item.

string result = Smart.Format("You have {Count:plural:one item|{} items}.", new { Count = 5 });
// Output: You have 5 items.

// Advanced pluralization
string result = Smart.Format("{Count:plural:no items|one item|{} items}", new { Count = 0 });
// Output: no items
```

### List Formatter

Format collections with custom separators:

```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };

// Default separator (comma)
string result = Smart.Format("{0:list:{}|, }", numbers);
// Output: 1, 2, 3, 4, 5

// Custom separators
string result = Smart.Format("{0:list:{} and {}|, | and }", new[] { "Alice", "Bob", "Charlie" });
// Output: Alice, Bob and Charlie

// With index
var items = new[] { "Apple", "Banana", "Cherry" };
string result = Smart.Format("{0:list:{Index}: {Item}|, }", items);
// Output: 0: Apple, 1: Banana, 2: Cherry
```

### Choose Formatter

Select output based on value:

```csharp
// Syntax: {value:choose(option1|option2|option3):case1|case2|case3}
string result = Smart.Format("{Status:choose(0|1|2):Pending|Active|Completed}", new { Status = 1 });
// Output: Active

// With string matching
string result = Smart.Format("{Day:choose(Mon|Tue|Wed|Thu|Fri|Sat|Sun):Weekday|Weekday|Weekday|Weekday|Weekday|Weekend|Weekend}",
    new { Day = "Sat" });
// Output: Weekend
```

### Time Formatter

Format TimeSpan values in human-readable form:

```csharp
var timeSpan = TimeSpan.FromHours(2.5);

string result = Smart.Format("{0:time}", timeSpan);
// Output: 2 hours 30 minutes

string result = Smart.Format("{0:time:short}", TimeSpan.FromSeconds(90));
// Output: 1m 30s

string result = Smart.Format("{0:timespan:h' hours 'm' minutes'}", TimeSpan.FromMinutes(125));
// Output: 2 hours 5 minutes
```

### SubString Formatter

Extract substrings:

```csharp
string text = "Hello World";

// Get first N characters
string result = Smart.Format("{0:substr(0,5)}", text);
// Output: Hello

// Get last N characters
string result = Smart.Format("{0:substr(-5)}", text);
// Output: World
```

## Advanced Features

### Dictionary Support

```csharp
var dict = new Dictionary<string, object>
{
    ["Name"] = "Alice",
    ["Age"] = 30,
    ["City"] = "Boston"
};

string result = Smart.Format("{Name} is {Age} years old and lives in {City}.", dict);
// Output: Alice is 30 years old and lives in Boston.
```

### Nested Templates

```csharp
var data = new
{
    User = "John",
    Items = new[] { "Apple", "Banana", "Orange" }
};

string result = Smart.Format("{User}'s shopping list: {Items:list:{} ({Index})|, }", data);
// Output: John's shopping list: Apple (0), Banana (1), Orange (2)
```

### Combining Formatters

```csharp
var orders = new[] { 1, 2, 3 };

string result = Smart.Format("You have {0:list:{} order|, }.", orders);
// Output: You have 1 order, 2 order, 3 order.

string result = Smart.Format("You have {Count:plural:no orders|one order|{} orders}.", new { Count = orders.Length });
// Output: You have 3 orders.
```

### Conditional with Nested Objects

```csharp
var user = new
{
    Name = "Alice",
    Premium = true,
    Credits = 100
};

string result = Smart.Format("{Name}: {Premium:cond:Premium Member ({Credits} credits)|Standard Member}", user);
// Output: Alice: Premium Member (100 credits)
```

## Extension Methods

### StringBuilder Extensions

```csharp
using System.Text;
using SmartFormat;

var sb = new StringBuilder();

sb.AppendSmart("Hello {Name}!", new { Name = "World" });
// StringBuilder now contains: Hello World!

sb.AppendLineSmart("Value: {0}", 42);
// StringBuilder now contains: Hello World!Value: 42\n
```

### TextWriter Extensions

```csharp
using System.IO;
using SmartFormat;

using (var writer = new StreamWriter("output.txt"))
{
    writer.WriteSmart("Hello {Name}!", new { Name = "World" });
    writer.WriteLineSmart("Count: {0}", 5);
}
```

### String Extensions

```csharp
using SmartFormat;

string template = "Hello {Name}!";
string result = template.FormatSmart(new { Name = "Alice" });
// Output: Hello Alice!
```

## Performance Optimization

### Using Format Cache

For frequently used format strings, use caching to improve performance:

```csharp
using SmartFormat.Core.Formatting;

FormatCache cache = null;
string template = "Hello {Name}, you are {Age} years old.";

// First call - parses and caches the template
string result1 = template.FormatSmart(ref cache, new { Name = "Alice", Age = 30 });

// Subsequent calls - reuses cached parse result (faster)
string result2 = template.FormatSmart(ref cache, new { Name = "Bob", Age = 25 });
string result3 = template.FormatSmart(ref cache, new { Name = "Charlie", Age = 35 });
```

### Direct SmartFormatter Usage

```csharp
using SmartFormat;

var formatter = Smart.Default;

// Reuse the same formatter instance
string result1 = formatter.Format("{0} + {1} = {2}", 1, 2, 3);
string result2 = formatter.Format("{Name} is here", new { Name = "Alice" });
```

## Custom Configuration

### Creating a Custom Formatter

```csharp
using SmartFormat;
using SmartFormat.Extensions;

// Create a formatter with only specific extensions
var formatter = new SmartFormatter();

// Add source extensions
formatter.AddExtensions(
    new ReflectionSource(formatter),
    new DictionarySource(formatter),
    new DefaultSource(formatter)
);

// Add formatter extensions
formatter.AddExtensions(
    new DefaultFormatter(),
    new PluralLocalizationFormatter("en"),
    new ConditionalFormatter()
);

string result = formatter.Format("Hello {Name}!", new { Name = "World" });
```

### Accessing Extensions

```csharp
// Get a specific formatter extension
var pluralFormatter = Smart.Default.GetFormatterExtension<PluralLocalizationFormatter>();
if (pluralFormatter != null)
{
    // Configure the formatter
    // ...
}

// Get a specific source extension
var reflectionSource = Smart.Default.GetSourceExtension<ReflectionSource>();
```

## Error Handling

### Error Actions

Configure how format errors are handled:

```csharp
using SmartFormat.Core.Settings;

var formatter = new SmartFormatter();
formatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;  // Default
// or
formatter.Settings.FormatErrorAction = ErrorAction.ThrowError;
// or
formatter.Settings.FormatErrorAction = ErrorAction.Ignore;
// or
formatter.Settings.FormatErrorAction = ErrorAction.MaintainTokens;
```

### Error Event Handling

```csharp
Smart.Default.OnFormattingFailure += (sender, args) =>
{
    Console.WriteLine($"Formatting error: {args.ErrorMessage}");
};
```

## Complete Examples

### User Notification System

```csharp
var notification = new
{
    UserName = "Alice",
    UnreadMessages = 5,
    Friends = new[] { "Bob", "Charlie", "Diana" },
    IsPremium = true
};

string message = Smart.Format(
    @"{UserName}, you have {UnreadMessages:plural:no new messages|one new message|{} new messages}.
{IsPremium:cond:Premium features are enabled.|Upgrade to premium!}
Your friends: {Friends:list:{}|, | and }.",
    notification);

// Output:
// Alice, you have 5 new messages.
// Premium features are enabled.
// Your friends: Bob, Charlie and Diana.
```

### Report Generation

```csharp
var report = new
{
    Title = "Sales Report",
    Date = DateTime.Now,
    TotalSales = 15234.50m,
    Items = new[]
    {
        new { Product = "Widget A", Quantity = 150, Price = 29.99m },
        new { Product = "Widget B", Quantity = 200, Price = 19.99m },
        new { Product = "Widget C", Quantity = 75, Price = 49.99m }
    }
};

string output = Smart.Format(
    @"{Title} - {Date:yyyy-MM-dd}
Total Sales: ${TotalSales:N2}

Items:
{Items:list:{Product}: {Quantity} units @ ${Price:N2} each|
}",
    report);
```

### Localized Messages

```csharp
var orderInfo = new
{
    ItemCount = 3,
    Status = 1,  // 0=Pending, 1=Shipped, 2=Delivered
    EstimatedDays = 2
};

string message = Smart.Format(
    @"{ItemCount:plural:one item|{} items} - Status: {Status:choose(0|1|2):Pending|Shipped|Delivered}
{Status:cond:>=1?Estimated delivery in {EstimatedDays:plural:one day|{} days}.|Processing your order.}",
    orderInfo);

// Output: 3 items - Status: Shipped
// Estimated delivery in 2 days.
```

## Async Support

```csharp
using System.Threading;
using SmartFormat;

var data = new { Name = "Alice", Value = 42 };

string result = await Smart.FormatAsync("{Name}: {Value}", new object[] { data }, CancellationToken.None);
// Output: Alice: 42
```

## Migration from string.Format

SmartFormat is designed to be a drop-in replacement for `string.Format`:

```csharp
// string.Format
string old = string.Format("Hello {0}, you are {1} years old.", "Alice", 30);

// SmartFormat (same syntax works)
string new1 = Smart.Format("Hello {0}, you are {1} years old.", "Alice", 30);

// SmartFormat (enhanced syntax)
string new2 = Smart.Format("Hello {Name}, you are {Age} years old.", new { Name = "Alice", Age = 30 });
```

## Target Frameworks

- .NET Standard 2.0
- .NET 6.0
- .NET 10.0

## License

Copyright (C) axuno gGmbH, Scott Rippey, Bernhard Millauer and other contributors.
Licensed under the MIT license.
