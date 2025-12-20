# Ecng.Linq

A comprehensive library providing powerful extensions for LINQ, IQueryable, IAsyncEnumerable, and expression tree manipulation in .NET.

## Overview

Ecng.Linq extends the standard LINQ capabilities with:
- **Dynamic query ordering** - Order queries by property name at runtime
- **Async enumerable operations** - Full LINQ support for async sequences
- **Expression tree helpers** - Utilities for working with expression trees
- **IQueryable async extensions** - Async operations for queryable sources
- **Sync/Async interoperability** - Convert between synchronous and asynchronous enumerables

## Installation

Add a reference to the Ecng.Linq project in your .csproj file:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\Ecng.Linq\Linq.csproj" />
</ItemGroup>
```

## Table of Contents

- [Dynamic Query Ordering](#dynamic-query-ordering)
- [IQueryable Async Extensions](#iqueryable-async-extensions)
- [IAsyncEnumerable Extensions](#iasyncenumerable-extensions)
- [Expression Tree Helpers](#expression-tree-helpers)
- [Sync/Async Conversion](#syncasync-conversion)

---

## Dynamic Query Ordering

Order queryable collections dynamically using property names as strings, perfect for scenarios where sort order is determined at runtime (e.g., user input, configuration).

### OrderBy with Property Name

```csharp
using Ecng.Linq;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
}

IQueryable<Product> products = dbContext.Products;

// Order by property name (case-sensitive)
var orderedByName = products.OrderBy("Name", ignoreCase: false);

// Order by property name (case-insensitive)
var orderedByPrice = products.OrderBy("price", ignoreCase: true);

// Nested property ordering
var orderedByNested = products.OrderBy("Category.Name", ignoreCase: false);
```

### OrderByDescending with Property Name

```csharp
// Order descending by property name
var newestFirst = products.OrderByDescending("CreatedDate", ignoreCase: false);

// Case-insensitive descending order
var expensiveFirst = products.OrderByDescending("PRICE", ignoreCase: true);
```

### ThenBy for Multi-Level Sorting

```csharp
// Multiple sort criteria
var sorted = products
    .OrderBy("Category.Name", ignoreCase: false)
    .ThenBy("Price", ignoreCase: false)
    .ThenByDescending("CreatedDate", ignoreCase: false);
```

### Dynamic Sorting from User Input

```csharp
public IQueryable<Product> GetSortedProducts(string sortColumn, bool ascending)
{
    IQueryable<Product> query = dbContext.Products;

    if (ascending)
        return query.OrderBy(sortColumn, ignoreCase: true);
    else
        return query.OrderByDescending(sortColumn, ignoreCase: true);
}

// Usage
var products = GetSortedProducts("Name", ascending: true);
```

---

## IQueryable Async Extensions

Asynchronous operations for IQueryable sequences, ideal for database queries and remote data sources.

### CountAsync

```csharp
using Ecng.Linq;
using System.Threading;

IQueryable<Product> products = dbContext.Products.Where(p => p.Price > 100);

// Count elements asynchronously
long count = await products.CountAsync(CancellationToken.None);
Console.WriteLine($"Found {count} expensive products");
```

### AnyAsync

```csharp
// Check if any elements exist
bool hasExpensiveProducts = await products.AnyAsync(CancellationToken.None);

if (hasExpensiveProducts)
{
    Console.WriteLine("Expensive products are available");
}
```

### FirstOrDefaultAsync

```csharp
// Get first element or default
Product firstExpensive = await products
    .OrderBy("Price", ignoreCase: false)
    .FirstOrDefaultAsync(CancellationToken.None);

if (firstExpensive != null)
{
    Console.WriteLine($"Cheapest expensive product: {firstExpensive.Name}");
}
```

### ToArrayAsync

```csharp
// Materialize query to array asynchronously
Product[] productArray = await products.ToArrayAsync(CancellationToken.None);

foreach (var product in productArray)
{
    Console.WriteLine(product.Name);
}
```

### SkipLong

```csharp
// Skip a large number of elements (supports long instead of int)
var pagedResults = products
    .OrderBy("CreatedDate", ignoreCase: false)
    .SkipLong(10_000_000L)
    .Take(100);

var results = await pagedResults.ToArrayAsync(CancellationToken.None);
```

---

## IAsyncEnumerable Extensions

Comprehensive LINQ-style operations for async sequences, providing the full power of LINQ for asynchronous data streams.

### Basic Operations

```csharp
using Ecng.Linq;
using System.Collections.Generic;
using System.Threading;

async IAsyncEnumerable<int> GetNumbersAsync()
{
    for (int i = 0; i < 100; i++)
    {
        await Task.Delay(10);
        yield return i;
    }
}

// Where - Filter elements
var evenNumbers = GetNumbersAsync().Where(n => n % 2 == 0);

// Select - Transform elements
var doubled = GetNumbersAsync().Select(n => n * 2);

// Take - Limit results
var first10 = GetNumbersAsync().Take(10);

// Skip - Skip elements
var skipFirst10 = GetNumbersAsync().Skip(10);

// Iterate asynchronously
await foreach (var number in first10)
{
    Console.WriteLine(number);
}
```

### Aggregation Operations

```csharp
IAsyncEnumerable<int> numbers = GetNumbersAsync();

// Count elements
int count = await numbers.CountAsync();

// Sum
int sum = await numbers.SumAsync();

// Average
double average = await numbers.AverageAsync();

// Min/Max
int min = await numbers.MinAsync();
int max = await numbers.MaxAsync();
```

### Element Access

```csharp
IAsyncEnumerable<Product> asyncProducts = GetProductsAsync();

// First element
Product first = await asyncProducts.FirstAsync();

// First or default
Product firstOrNull = await asyncProducts.FirstOrDefaultAsync();

// Last element
Product last = await asyncProducts.LastAsync();

// Single element (throws if more than one)
Product single = await asyncProducts.SingleAsync();

// Element at index
Product atIndex = await asyncProducts.ElementAtAsync(5);
```

### Existence Checks

```csharp
// Check if any elements exist
bool hasAny = await asyncProducts.AnyAsync();

// Check if any match predicate
bool hasExpensive = await asyncProducts.AnyAsync(p => p.Price > 1000);

// Check if all match predicate
bool allExpensive = await asyncProducts.AllAsync(p => p.Price > 100);

// Check if contains specific element
bool contains = await asyncProducts.ContainsAsync(specificProduct);
```

### Collection Conversion

```csharp
// To array
Product[] array = await asyncProducts.ToArrayAsync();

// To list
List<Product> list = await asyncProducts.ToListAsync();

// To dictionary
Dictionary<int, Product> dict = await asyncProducts
    .ToDictionaryAsync(p => p.Id);

// To dictionary with value selector
Dictionary<int, string> nameDict = await asyncProducts
    .ToDictionaryAsync(p => p.Id, p => p.Name);

// To hash set
HashSet<Product> set = await asyncProducts.ToHashSetAsync();
```

### Sorting and Ordering

```csharp
// Order by
var orderedProducts = asyncProducts.OrderBy(p => p.Price);

// Order by descending
var descProducts = asyncProducts.OrderByDescending(p => p.CreatedDate);

// Reverse
var reversed = asyncProducts.Reverse();

await foreach (var product in orderedProducts)
{
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

### Set Operations

```csharp
IAsyncEnumerable<int> sequence1 = GetSequence1();
IAsyncEnumerable<int> sequence2 = GetSequence2();

// Distinct elements
var distinct = sequence1.Distinct();

// Distinct by key
var distinctProducts = asyncProducts.DistinctBy(p => p.Name);

// Union
var union = sequence1.Union(sequence2);

// Intersect
var intersection = sequence1.Intersect(sequence2);

// Except (set difference)
var difference = sequence1.Except(sequence2);
```

### Sequence Manipulation

```csharp
IAsyncEnumerable<int> numbers = GetNumbersAsync();

// Concat - Combine sequences
var combined = numbers.Concat(GetMoreNumbersAsync());

// Append - Add element at end
var withExtra = numbers.Append(999);

// Prepend - Add element at start
var withPrefix = numbers.Prepend(-1);

// Chunk - Split into batches
var batches = numbers.Chunk(10);
await foreach (var batch in batches)
{
    Console.WriteLine($"Batch of {batch.Length} items");
}
```

### Conditional Operations

```csharp
// Skip while condition is true
var skipLowPrices = asyncProducts.SkipWhile(p => p.Price < 100);

// Take while condition is true
var takeCheap = asyncProducts.TakeWhile(p => p.Price < 100);

// Default if empty
var withDefault = asyncProducts.DefaultIfEmpty();

// Default if empty with specific value
var withSpecificDefault = asyncProducts.DefaultIfEmpty(new Product { Name = "Default" });
```

### Advanced Projections

```csharp
// Select many - Flatten nested sequences
var allTags = asyncProducts.SelectMany(p => p.Tags);

// Zip - Combine two sequences
var prices = GetPricesAsync();
var names = GetNamesAsync();
var combined = prices.Zip(names, (price, name) => new { Name = name, Price = price });

// Zip with tuples
var tuples = prices.Zip(names);
await foreach (var (price, name) in tuples)
{
    Console.WriteLine($"{name}: ${price}");
}
```

### Type Filtering and Casting

```csharp
IAsyncEnumerable<object> mixedObjects = GetMixedObjectsAsync();

// Filter by type
IAsyncEnumerable<Product> onlyProducts = mixedObjects.OfType<Product>();

// Cast all elements
IAsyncEnumerable<Product> casted = mixedObjects.Cast<Product>();

// Cast with converter
var converted = asyncProducts.Cast<Product, ProductDto>(p => new ProductDto
{
    Name = p.Name
});
```

### Grouping

```csharp
// Group by key (assumes source is pre-sorted by key)
var grouped = asyncProducts.GroupByAsync(p => p.CategoryId);

await foreach (var group in grouped)
{
    Console.WriteLine($"Category {group.Key}:");
    foreach (var product in group)
    {
        Console.WriteLine($"  - {product.Name}");
    }
}
```

### Custom Extensions

```csharp
// Filter based on comparison with previous element
var changed = asyncPrices.WhereWithPrevious((prev, curr) => curr != prev);

await foreach (var price in changed)
{
    Console.WriteLine($"Price changed to: {price}");
}
```

### Aggregation with Custom Functions

```csharp
// Aggregate without seed
var concatenated = asyncProducts
    .Select(p => p.Name)
    .AggregateAsync((acc, name) => acc + ", " + name);

// Aggregate with seed
var total = await asyncProducts
    .AggregateAsync(0m, (sum, product) => sum + product.Price);

Console.WriteLine($"Total value: ${total}");
```

### Sequence Comparison

```csharp
IAsyncEnumerable<int> seq1 = GetSequence1Async();
IAsyncEnumerable<int> seq2 = GetSequence2Async();

// Check if sequences are equal
bool areEqual = await seq1.SequenceEqualAsync(seq2);

if (areEqual)
{
    Console.WriteLine("Sequences are identical");
}
```

### Static Generators

```csharp
using System.Linq;

// Generate range asynchronously
var range = AsyncEnumerable.Range(0, 100);

// Repeat value asynchronously
var repeated = AsyncEnumerable.Repeat("Hello", 10);

// Empty sequence
var empty = AsyncEnumerable.Empty<Product>();

await foreach (var number in range)
{
    Console.WriteLine(number);
}
```

---

## Expression Tree Helpers

Utilities for working with expression trees, useful for building dynamic queries and analyzing lambda expressions.

### Evaluate Expressions

```csharp
using Ecng.Linq;
using System.Linq.Expressions;

// Evaluate constant expression
Expression<Func<int>> expr = () => 42;
int result = expr.Body.Evaluate() as int?; // 42

// Evaluate complex expression
Expression<Func<int, int>> doubleExpr = x => x * 2;
// Note: Use Expression.Invoke for parameterized expressions
```

### Get Constant Values

```csharp
// Extract constant value from expression
ConstantExpression constExpr = Expression.Constant(100);
int value = constExpr.GetConstant<int>(); // 100
```

### Strip Quote Expressions

```csharp
// Remove quote wrappers from expressions
Expression quotedExpr = Expression.Quote(Expression.Constant(42));
Expression unquoted = quotedExpr.StripQuotes();
```

### Get Member Values

```csharp
using System.Reflection;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var person = new Person { Name = "John", Age = 30 };
PropertyInfo nameProperty = typeof(Person).GetProperty("Name");

// Get property value
object name = nameProperty.GetMemberValue(person); // "John"
```

### Get Value from Expression

```csharp
// Extract value from member expression
Expression<Func<Person, string>> expr = p => p.Name;
var memberExpr = expr.Body as MemberExpression;

// Get the value if the expression can be evaluated
// string value = memberExpr.GetValue<string>();
```

### Work with Nested Members

```csharp
public class Order
{
    public Customer Customer { get; set; }
}

public class Customer
{
    public string Name { get; set; }
}

Expression<Func<Order, string>> expr = o => o.Customer.Name;
var memberExpr = expr.Body as MemberExpression;

// Get the innermost member
var innerMember = memberExpr.GetInnerMember(); // Points to 'o' parameter
```

### Convert Expression Types to Operators

```csharp
using Ecng.Common;

// Convert expression type to comparison operator
ExpressionType greaterThan = ExpressionType.GreaterThan;
ComparisonOperator op = greaterThan.ToOperator(); // ComparisonOperator.Greater

// Supported conversions:
// GreaterThan -> Greater
// GreaterThanOrEqual -> GreaterOrEqual
// LessThan -> Less
// LessThanOrEqual -> LessOrEqual
// Equal -> Equal
// NotEqual -> NotEqual
```

### Replace Query Provider

```csharp
// Replace the query provider in an expression tree
IQueryable<Product> query = dbContext.Products.Where(p => p.Price > 100);
IQueryProvider newProvider = customQueryProvider;

query.Expression.ReplaceSource(newProvider);
// The expression now uses the new provider
```

---

## Sync/Async Conversion

Convert between synchronous IEnumerable and asynchronous IAsyncEnumerable seamlessly.

### Convert IEnumerable to IAsyncEnumerable

```csharp
using Ecng.Linq;
using System.Linq;

// From array
int[] numbers = { 1, 2, 3, 4, 5 };
IAsyncEnumerable<int> asyncNumbers = numbers.ToAsyncEnumerable();

// From list
List<Product> products = GetProducts();
IAsyncEnumerable<Product> asyncProducts = products.ToAsyncEnumerable();

// From LINQ query
var expensiveProducts = products
    .Where(p => p.Price > 100)
    .ToAsyncEnumerable();

// Iterate asynchronously
await foreach (var product in asyncProducts)
{
    Console.WriteLine(product.Name);
}
```

### Using SyncAsyncEnumerable Wrapper

```csharp
// Manually wrap a synchronous enumerable
IEnumerable<int> syncNumbers = Enumerable.Range(1, 100);
var asyncWrapper = new SyncAsyncEnumerable<int>(syncNumbers);

await foreach (var number in asyncWrapper)
{
    Console.WriteLine(number);
}
```

### Cancellation Support

```csharp
var cancellationTokenSource = new CancellationTokenSource();
cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));

IAsyncEnumerable<int> numbers = syncNumbers.ToAsyncEnumerable();

try
{
    await foreach (var number in numbers.WithCancellation(cancellationTokenSource.Token))
    {
        await Task.Delay(100);
        Console.WriteLine(number);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled");
}
```

---

## Complete Usage Example

Here's a comprehensive example demonstrating multiple features:

```csharp
using Ecng.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ProductService
{
    private readonly IQueryable<Product> _products;

    public ProductService(IQueryable<Product> products)
    {
        _products = products;
    }

    // Dynamic sorting with async execution
    public async Task<Product[]> GetSortedProductsAsync(
        string sortBy,
        bool ascending,
        CancellationToken ct)
    {
        var query = ascending
            ? _products.OrderBy(sortBy, ignoreCase: true)
            : _products.OrderByDescending(sortBy, ignoreCase: true);

        return await query.ToArrayAsync(ct);
    }

    // Pagination with large datasets
    public async Task<Product[]> GetPagedProductsAsync(
        long skip,
        int take,
        CancellationToken ct)
    {
        var query = _products
            .OrderBy("CreatedDate", ignoreCase: false)
            .SkipLong(skip)
            .Take(take);

        return await query.ToArrayAsync(ct);
    }

    // Check availability
    public async Task<bool> HasProductsInStockAsync(CancellationToken ct)
    {
        return await _products
            .Where(p => p.Stock > 0)
            .AnyAsync(ct);
    }

    // Process async stream
    public async Task ProcessProductStreamAsync(
        IAsyncEnumerable<Product> productStream,
        CancellationToken ct)
    {
        var expensiveProducts = productStream
            .Where(p => p.Price > 1000)
            .OrderByDescending(p => p.Price)
            .Take(10);

        var products = await expensiveProducts.ToListAsync(ct);

        foreach (var product in products)
        {
            Console.WriteLine($"{product.Name}: ${product.Price}");
        }
    }

    // Batch processing
    public async Task ProcessInBatchesAsync(
        IAsyncEnumerable<Product> products,
        CancellationToken ct)
    {
        var batches = products.Chunk(100);

        await foreach (var batch in batches.WithCancellation(ct))
        {
            await ProcessBatchAsync(batch, ct);
        }
    }

    private async Task ProcessBatchAsync(Product[] batch, CancellationToken ct)
    {
        // Process batch
        await Task.Delay(100, ct);
        Console.WriteLine($"Processed batch of {batch.Length} products");
    }

    // Convert and aggregate
    public async Task<decimal> CalculateTotalValueAsync(
        IEnumerable<Product> products,
        CancellationToken ct)
    {
        var asyncProducts = products.ToAsyncEnumerable();

        return await asyncProducts
            .Where(p => p.Stock > 0)
            .AggregateAsync(0m, (sum, p) => sum + (p.Price * p.Stock), ct);
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

---

## Framework Compatibility

- **.NET Standard 2.0** - Includes compatibility layer for IAsyncEnumerable operations
- **.NET 6.0** - Full support for modern async patterns
- **.NET 10.0+** - Native IAsyncEnumerable support

The library automatically provides IAsyncEnumerable extension methods for frameworks that don't include them natively (pre-.NET 10.0).

---

## Best Practices

1. **Always use CancellationToken** when working with async operations
2. **Dispose async enumerators** properly using `await using` or `WithCancellation()`
3. **Use ToAsyncEnumerable()** to bridge sync and async code seamlessly
4. **Leverage dynamic ordering** for user-driven sorting scenarios
5. **Prefer SkipLong** for large dataset pagination
6. **Use Chunk** for batch processing of large sequences

---

## Thread Safety

- Expression helpers are thread-safe for read operations
- IAsyncEnumerable extensions are designed for single-threaded async iteration
- SyncAsyncEnumerable wrapper is thread-safe if the underlying IEnumerable is thread-safe

---

## Performance Considerations

- Dynamic ordering uses reflection; cache queries when possible
- Async operations add overhead; use synchronous LINQ when async is not needed
- ToAsyncEnumerable() has minimal overhead for arrays and lists
- Chunk operations allocate arrays; adjust batch size based on memory constraints

---

## License

This library is part of the Ecng framework. See the project root for license information.
