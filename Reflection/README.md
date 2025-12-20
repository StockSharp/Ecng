# Ecng.Reflection

A high-performance reflection library providing utilities for type introspection, member discovery, fast attribute retrieval, and dynamic type operations.

## Overview

**Ecng.Reflection** extends .NET's reflection capabilities with performance optimizations through caching, simplified API for common reflection tasks, and utilities for working with types, members, and attributes. It's designed to reduce the complexity and improve the performance of reflection-heavy code.

## Key Features

- **Fast attribute retrieval** with built-in caching
- **Simplified member access** (properties, fields, methods, constructors)
- **Type discovery and metadata** exploration
- **Assembly scanning** for type implementations
- **Generic type utilities** for working with generic types
- **Collection type detection** and item type extraction
- **Member signature comparison** for overload resolution
- **Indexer support** for types with indexers
- **Proxy type mapping** for custom type resolution

## Installation

Add a reference to the **Ecng.Reflection** project or NuGet package in your project.

```xml
<ItemGroup>
  <ProjectReference Include="..\Ecng.Reflection\Reflection.csproj" />
</ItemGroup>
```

## Usage Examples

### Working with Attributes

The library provides extension methods for easy attribute retrieval with caching support:

```csharp
using Ecng.Reflection;
using Ecng.Common;

public class MyClass
{
    [Obsolete("Use NewMethod instead")]
    public void OldMethod() { }

    [DisplayName("User Name")]
    public string Name { get; set; }
}

// Get a single attribute
var obsoleteAttr = typeof(MyClass)
    .GetMember<MethodInfo>("OldMethod")
    .GetAttribute<ObsoleteAttribute>();

Console.WriteLine(obsoleteAttr?.Message); // "Use NewMethod instead"

// Get all attributes
var attrs = typeof(MyClass)
    .GetProperty("Name")
    .GetAttributes<Attribute>();

// Check if type/member is obsolete
bool isObsolete = typeof(MyClass).GetMethod("OldMethod").IsObsolete();

// Check if type/member is browsable
bool isBrowsable = typeof(MyClass).GetProperty("Name").IsBrowsable();
```

### Getting Members by Name and Type

Retrieve members with simple, chainable syntax:

```csharp
using Ecng.Reflection;

public class Calculator
{
    public int Add(int a, int b) => a + b;
    public double Add(double a, double b) => a + b;

    public string Name { get; set; }
    private int _count;
}

// Get a specific method by parameter types
var addIntMethod = typeof(Calculator)
    .GetMember<MethodInfo>("Add", typeof(int), typeof(int));

// Get a property
var nameProperty = typeof(Calculator)
    .GetMember<PropertyInfo>("Name");

// Get a field (including private)
var countField = typeof(Calculator)
    .GetMember<FieldInfo>("_count", ReflectionHelper.AllInstanceMembers);

// Get constructor with specific parameters
var ctor = typeof(Calculator)
    .GetMember<ConstructorInfo>(typeof(string));
```

### Working with Generic Types

Extract generic type information and create generic types:

```csharp
using Ecng.Reflection;
using Ecng.Common;

// Get the generic type definition from a type hierarchy
var listType = typeof(List<string>).GetGenericType(typeof(IEnumerable<>));
// Returns: IEnumerable<string>

// Get a specific generic argument
var itemType = typeof(List<int>).GetGenericTypeArg(typeof(IEnumerable<>), 0);
// Returns: typeof(int)

// Create a generic type
var genericListDef = typeof(List<>);
var stringListType = genericListDef.Make(typeof(string));
// Returns: typeof(List<string>)

// Get item type from collection
var collectionItemType = typeof(List<int>).GetItemType();
// Returns: typeof(int)

// Works with IEnumerable<T>, ICollection<T>, and IAsyncEnumerable<T>
var enumerableItemType = typeof(IEnumerable<string>).GetItemType();
// Returns: typeof(string)
```

### Type Detection and Validation

Check type characteristics:

```csharp
using Ecng.Reflection;
using Ecng.Common;

// Check if type is a collection
bool isList = typeof(List<int>).IsCollection(); // true
bool isArray = typeof(int[]).IsCollection(); // true
bool isEnumerable = typeof(IEnumerable<int>).IsCollection(); // true

// Check if type is primitive (extended definition)
bool isPrimitive = typeof(int).IsPrimitive(); // true
bool isString = typeof(string).IsPrimitive(); // true
bool isDateTime = typeof(DateTime).IsPrimitive(); // true
bool isGuid = typeof(Guid).IsPrimitive(); // true

// Check if type is numeric
bool isNumeric = typeof(int).IsNumeric(); // true
bool isIntegerNumeric = typeof(int).IsNumericInteger(); // true
bool isFloatNumeric = typeof(double).IsNumeric(); // true (but IsNumericInteger() = false)

// Check if type is struct or enum
bool isStruct = typeof(DateTime).IsStruct(); // true
bool isEnum = typeof(DayOfWeek).IsEnum(); // true

// Check if type is delegate or attribute
bool isDelegate = typeof(Action).IsDelegate(); // true
bool isAttribute = typeof(ObsoleteAttribute).IsAttribute(); // true
```

### Member Information

Work with member metadata:

```csharp
using Ecng.Reflection;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public static int Count { get; set; }
}

var nameProperty = typeof(Product).GetProperty("Name");
var priceProperty = typeof(Product).GetProperty("Price");
var countProperty = typeof(Product).GetProperty("Count");

// Get the type of a member
Type nameType = nameProperty.GetMemberType(); // typeof(string)
Type priceType = priceProperty.GetMemberType(); // typeof(decimal)

// Check if member is static
bool isStatic = nameProperty.IsStatic(); // false
bool isCountStatic = countProperty.IsStatic(); // true

// Check if member is abstract
bool isAbstract = nameProperty.IsAbstract(); // false

// Check if member is virtual
bool isVirtual = nameProperty.IsVirtual(); // true (properties are virtual by default)

// Check if member is overloadable
bool isOverloadable = nameProperty.IsOverloadable(); // true

// Check if property is modifiable (has public setter and not read-only)
bool isModifiable = nameProperty.IsModifiable(); // true
```

### Working with Indexers

Access indexer properties:

```csharp
using Ecng.Reflection;

public class DataStore
{
    private Dictionary<string, object> _data = new();

    // Default indexer
    public object this[string key]
    {
        get => _data[key];
        set => _data[key] = value;
    }

    // Indexer with multiple parameters
    public object this[int row, int col]
    {
        get => _data[$"{row},{col}"];
        set => _data[$"{row},{col}"] = value;
    }
}

// Get default string indexer
var stringIndexer = typeof(DataStore).GetIndexer(typeof(string));

// Get indexer with multiple parameters
var multiIndexer = typeof(DataStore).GetIndexer(typeof(int), typeof(int));

// Get all indexers
var allIndexers = typeof(DataStore).GetIndexers();

// Get indexer types
var indexerTypes = stringIndexer.GetIndexerTypes();
// Returns: [typeof(string)]

// Check if property is an indexer
bool isIndexer = stringIndexer.IsIndexer(); // true
```

### Creating Instances

Fast instance creation:

```csharp
using Ecng.Reflection;
using Ecng.Common;

public class Person
{
    public Person() { }
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; set; }
    public int Age { get; set; }
}

// Create with default constructor
var person1 = typeof(Person).CreateInstance();

// Create with parameters
var person2 = typeof(Person).CreateInstance("John", 30);

// Create with generic type parameter
var person3 = typeof(Person).CreateInstance<Person>("Jane", 25);

// Note: For value types, a default constructor is automatically supported
var point = typeof(Point).CreateInstance(); // Works even without explicit constructor
```

### Finding Implementations

Scan assemblies for type implementations:

```csharp
using Ecng.Reflection;
using System.Reflection;

// Find all implementations of IDisposable in the current assembly
var disposableTypes = Assembly.GetExecutingAssembly()
    .FindImplementations<IDisposable>();

// Find all implementations with filters
var publicDisposableTypes = Assembly.GetExecutingAssembly()
    .FindImplementations<IDisposable>(
        showObsolete: false,      // Exclude obsolete types
        showNonPublic: false,     // Exclude non-public types
        showNonBrowsable: false   // Exclude non-browsable types
    );

// Find implementations with custom filter
var customTypes = Assembly.GetExecutingAssembly()
    .FindImplementations<IComparable>(
        extraFilter: t => t.Namespace?.StartsWith("MyApp") == true
    );

// Check if type is compatible with requirements
bool isCompatible = typeof(MyClass).IsRequiredType<IService>();
// Checks: not abstract, public, not generic definition, has parameterless constructor
```

### Method and Parameter Information

Work with methods and their parameters:

```csharp
using Ecng.Reflection;

public class MathService
{
    public int Calculate(int x, ref int y, out int result)
    {
        result = x + y;
        y = y * 2;
        return result;
    }

    public void Print(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }
}

var method = typeof(MathService).GetMethod("Calculate");

// Get parameter types with info
var paramTypes = method.GetParameterTypes();
// Returns: [(param: x, type: int), (param: y, type: int&), (param: result, type: int&)]

// Get parameter types without ref/out wrappers
var plainParamTypes = method.GetParameterTypes(removeRef: true);
// Returns: [(param: x, type: int), (param: y, type: int), (param: result, type: int)]

// Check if parameter is output
var parameters = method.GetParameters();
bool isYOutput = parameters[1].IsOutput(); // true (ref parameter)
bool isResultOutput = parameters[2].IsOutput(); // true (out parameter)

// Check for params array
var printMethod = typeof(MathService).GetMethod("Print");
var printParams = printMethod.GetParameters();
bool hasParams = printParams[1].IsParams(); // true

// Get delegate invoke method
var delegateType = typeof(Action<int>);
var invokeMethod = delegateType.GetInvokeMethod();
```

### Assembly and Type Validation

Verify assemblies and validate types:

```csharp
using Ecng.Reflection;

// Check if file is a valid assembly
bool isAssembly = @"C:\path\to\MyLibrary.dll".IsAssembly();

// Verify assembly and get assembly name
AssemblyName asmName = @"C:\path\to\MyLibrary.dll".VerifyAssembly();

if (asmName != null)
{
    Console.WriteLine($"Valid assembly: {asmName.FullName}");
}

// Check if type is runtime type
bool isRuntimeType = typeof(string).IsRuntimeType();
```

### Accessor Methods and Property Names

Work with property/event accessor methods:

```csharp
using Ecng.Reflection;

public class EventSource
{
    public event EventHandler DataChanged;
    public string Name { get; set; }
}

// Get property name from accessor method name
string propName = "get_Name".MakePropertyName(); // "Name"
string eventName = "add_DataChanged".MakePropertyName(); // "DataChanged"

// Get the owner member of an accessor method
var method = typeof(EventSource).GetMethod("get_Name", ReflectionHelper.AllInstanceMembers);
var owner = method.GetAccessorOwner();
// Returns: PropertyInfo for "Name" property

var addMethod = typeof(EventSource).GetMethod("add_DataChanged", ReflectionHelper.AllInstanceMembers);
var eventOwner = addMethod.GetAccessorOwner();
// Returns: EventInfo for "DataChanged" event
```

### Member Signature Comparison

Compare member signatures for overload resolution:

```csharp
using Ecng.Reflection;

public class Calculator
{
    public int Add(int a, int b) => a + b;
    public double Add(double a, double b) => a + b;
}

var method1 = typeof(Calculator).GetMember<MethodInfo>("Add", typeof(int), typeof(int));
var method2 = typeof(Calculator).GetMember<MethodInfo>("Add", typeof(double), typeof(double));

var sig1 = new MemberSignature(method1);
var sig2 = new MemberSignature(method2);

bool areSame = sig1.Equals(sig2); // false

// MemberSignature captures:
// - Return type
// - Parameter types
// - For indexers: indexer parameter types
```

### Filtering Members

Filter members by various criteria:

```csharp
using Ecng.Reflection;

public class DataService
{
    public string GetData(int id) => "data";
    public void SetData(int id, string value) { }
    public string this[int index]
    {
        get => "value";
        set { }
    }
}

// Get all public instance members
var members = typeof(DataService).GetMembers<MemberInfo>(ReflectionHelper.AllInstanceMembers);

// Get all properties
var properties = typeof(DataService).GetMembers<PropertyInfo>();

// Get all methods with specific parameters
var getMethods = typeof(DataService).GetMembers<MethodInfo>(typeof(int));

// Filter members by type signature
var filtered = members.FilterMembers(isSetter: false, typeof(int));
// Returns members that accept (int) as parameter(s)

// Check member type
bool isMethod = members[0].MemberIs(MemberTypes.Method);
bool isPropertyOrField = members[0].MemberIs(MemberTypes.Property, MemberTypes.Field);
```

### Proxy Types

Register proxy types for custom type resolution:

```csharp
using Ecng.Reflection;

// Register a proxy type mapping
ReflectionHelper.ProxyTypes[typeof(IMyInterface)] = typeof(MyImplementation);

// When getting members, the library will automatically use the proxy type
var members = typeof(IMyInterface).GetMembers<MethodInfo>();
// Actually retrieves members from MyImplementation
```

### Binding Flags Helpers

Use predefined binding flags for common scenarios:

```csharp
using Ecng.Reflection;

// Get all members (public + non-public, static + instance)
var allMembers = typeof(MyClass).GetMembers<MemberInfo>(ReflectionHelper.AllMembers);

// Get all static members
var staticMembers = typeof(MyClass).GetMembers<MethodInfo>(ReflectionHelper.AllStaticMembers);

// Get all instance members
var instanceMembers = typeof(MyClass).GetMembers<PropertyInfo>(ReflectionHelper.AllInstanceMembers);

// Common attribute targets
// ReflectionHelper.Members = AttributeTargets.Field | AttributeTargets.Property
// ReflectionHelper.Types = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface
```

### Ordering Members

Order members by their declaration order:

```csharp
using Ecng.Reflection;

public class MyClass
{
    public string FirstProperty { get; set; }
    public int SecondProperty { get; set; }
    public bool ThirdProperty { get; set; }
}

var properties = typeof(MyClass)
    .GetMembers<PropertyInfo>()
    .OrderByDeclaration();

// Properties will be in declaration order: FirstProperty, SecondProperty, ThirdProperty
```

### Matching Members with Binding Flags

Check if members match specific binding flags:

```csharp
using Ecng.Reflection;

public class TestClass
{
    public string PublicProperty { get; set; }
    private int PrivateField;
    public static void StaticMethod() { }
    public void InstanceMethod() { }
}

var publicProp = typeof(TestClass).GetProperty("PublicProperty");
var privateField = typeof(TestClass).GetField("PrivateField", ReflectionHelper.AllInstanceMembers);
var staticMethod = typeof(TestClass).GetMethod("StaticMethod");
var instanceMethod = typeof(TestClass).GetMethod("InstanceMethod");

// Check if members match binding flags
bool matchPublic = publicProp.IsMatch(BindingFlags.Public | BindingFlags.Instance); // true
bool matchPrivate = privateField.IsMatch(BindingFlags.NonPublic | BindingFlags.Instance); // true
bool matchStatic = staticMethod.IsMatch(BindingFlags.Public | BindingFlags.Static); // true
bool noMatch = staticMethod.IsMatch(BindingFlags.Public | BindingFlags.Instance); // false
```

### VoidType Class

A marker class for representing void type in generic contexts:

```csharp
using Ecng.Reflection;

// Use VoidType when you need a type parameter but want to represent "void"
var voidType = typeof(VoidType);

// This can be useful in generic scenarios where you need a placeholder
// for methods that don't return a value
```

## Performance Considerations

The library uses extensive caching to improve performance:

- **Attribute cache**: Caches retrieved attributes by (type, provider, inherit) key
- **Generic type cache**: Caches generic type lookups
- **Collection type cache**: Caches collection type checks
- **Member property caches**: Caches results for IsAbstract, IsVirtual, IsStatic checks

### Cache Management

```csharp
using Ecng.Reflection;
using Ecng.Common;

// Enable/disable caching (enabled by default)
ReflectionHelper.CacheEnabled = true;
AttributeHelper.CacheEnabled = true;

// Clear all caches
ReflectionHelper.ClearCache();
AttributeHelper.ClearCache();
```

## Common Patterns

### Getting All Public Properties with Setters

```csharp
var writableProps = typeof(MyClass)
    .GetMembers<PropertyInfo>(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.IsModifiable());
```

### Finding All Methods That Take Specific Parameters

```csharp
var methods = typeof(MyClass)
    .GetMembers<MethodInfo>()
    .Where(m => m.GetParameterTypes()
        .Select(t => t.type)
        .SequenceEqual(new[] { typeof(string), typeof(int) }));
```

### Scanning Multiple Assemblies for Implementations

```csharp
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
var allImplementations = assemblies
    .SelectMany(asm => asm.FindImplementations<IMyInterface>(
        showObsolete: false,
        showNonPublic: false
    ));
```

### Working with Nullable Types

```csharp
using Ecng.Common;

// Check if type is nullable
bool isNullable = typeof(int?).IsNullable(); // true

// Get underlying type
Type underlyingType = typeof(int?).GetUnderlyingType(); // typeof(int)
```

## Advanced Topics

### Custom Type Constructors

Implement `ITypeConstructor` for custom type instantiation logic:

```csharp
using Ecng.Common;

public class CustomType : ITypeConstructor
{
    public object CreateInstance(params object[] args)
    {
        // Custom instantiation logic
        return new CustomType();
    }
}

// When using CreateInstance, custom logic will be invoked
var instance = typeof(CustomType).CreateInstance();
```

### Working with ref/out Parameters

```csharp
using Ecng.Reflection;

// GetParameterTypes with removeRef: true strips ref/out wrappers
var method = typeof(MyClass).GetMethod("MethodWithRefParams");
var types = method.GetParameterTypes(removeRef: true);

// Check if parameter is output (ref or out)
foreach (var param in method.GetParameters())
{
    if (param.IsOutput())
    {
        Console.WriteLine($"{param.Name} is an output parameter");
    }
}
```

## Constants and Definitions

```csharp
// Indexer property name
ReflectionHelper.IndexerName // "Item"

// Accessor prefixes
ReflectionHelper.GetPrefix    // "get_"
ReflectionHelper.SetPrefix    // "set_"
ReflectionHelper.AddPrefix    // "add_"
ReflectionHelper.RemovePrefix // "remove_"

// Binding flags
ReflectionHelper.AllMembers         // Static | Instance | Public | NonPublic
ReflectionHelper.AllStaticMembers   // Static | Public | NonPublic
ReflectionHelper.AllInstanceMembers // Instance | Public | NonPublic

// Attribute targets
ReflectionHelper.Members // Field | Property
ReflectionHelper.Types   // Class | Struct | Interface
```

## Dependencies

- **Ecng.Collections**: Collection utilities
- **Ecng.Common**: Common type helpers and extension methods

## License

Part of the StockSharp/Ecng framework.

## See Also

- [Ecng.Common](../Common/README.md) - Common utilities and type extensions
- [Ecng.Collections](../Collections/README.md) - Collection utilities
