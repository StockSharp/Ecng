# Ecng.UnitTesting

Enhanced assertion helpers and base test classes for MSTest unit testing with fluent extensions, collection comparisons, and CI environment detection.

## Purpose

Ecng.UnitTesting extends MSTest with additional assertion methods, fluent extension syntax, and helpful utilities for writing more expressive and maintainable unit tests. It provides a base test class with built-in CI detection, secrets management, and numerous assertion helpers.

## Key Features

- Fluent assertion syntax with extension methods
- Enhanced collection and enumerable comparisons
- Range and comparison assertions
- String assertion helpers
- Numeric assertions (positive, negative, zero)
- CI environment detection
- Secrets management for tests
- Base test class with common functionality
- Support for deep object comparisons
- Type assertions

## Installation

Add a reference to the `Ecng.UnitTesting` package in your project.

## Base Test Class

The `BaseTestClass` provides common functionality for all tests, including CI detection and secrets management.

### Basic Usage

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MyTests : BaseTestClass
{
    [TestMethod]
    public void SimpleTest()
    {
        int actual = 5;
        int expected = 5;

        AreEqual(expected, actual);
    }

    [TestMethod]
    public void TestWithCancellation()
    {
        // Access test context cancellation token
        var task = SomeAsyncOperation(CancellationToken);
        task.Wait();
    }
}
```

### CI Environment Detection

Skip tests when running in CI environments.

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class IntegrationTests : BaseTestClass
{
    // Skip all tests in this class when running in CI
    protected override bool SkipInCI => true;
    protected override string SkipInCIReason => "Requires local database";

    [TestMethod]
    public void TestWithLocalDatabase()
    {
        // This test only runs on localhost, not in CI
        if (IsLocalHost)
        {
            Console.WriteLine("Running on localhost");
        }
        else
        {
            Console.WriteLine("Running in CI");
        }
    }
}
```

### Secrets Management

Access secrets from environment variables or a secrets.json file.

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ApiTests : BaseTestClass
{
    [TestMethod]
    public void TestApiWithCredentials()
    {
        // Get secret from environment variable or secrets.json
        string apiKey = GetSecret("API_KEY");
        string endpoint = GetSecret("API_ENDPOINT");

        // Use the secrets
        var client = new ApiClient(endpoint, apiKey);
        // ...
    }

    [TestMethod]
    public void TestWithOptionalSecret()
    {
        // Try to get secret without failing
        string optionalKey = TryGetSecret("OPTIONAL_KEY");

        if (optionalKey != null)
        {
            // Use the optional key
        }
        else
        {
            // Skip or use default
        }
    }
}
```

### Custom Secrets File

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestInitializer
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        // Change secrets file location
        BaseTestClass.SecretsFile = "custom-secrets.json";
    }
}
```

## Fluent Assertion Extensions

The `AssertHelper` class provides extension methods for fluent assertions.

### Boolean Assertions

```csharp
using Ecng.UnitTesting;

// Assert true
bool condition = true;
condition.AssertTrue();
condition.AssertTrue("Condition should be true");

// Assert false
bool flag = false;
flag.AssertFalse();
flag.AssertFalse("Flag should be false");
```

### Null Assertions

```csharp
using Ecng.UnitTesting;

// Assert null
object obj = null;
obj.AssertNull();
obj.AssertNull("Object should be null");

// Assert not null
string str = "test";
str.AssertNotNull();
str.AssertNotNull("String should not be null");

// Special case for exceptions
Exception error = null;
error.AssertNull(); // Throws the exception if not null
```

### Equality Assertions

```csharp
using Ecng.UnitTesting;

// Assert equal
int value = 42;
value.AssertEqual(42);
value.AreEqual(42); // Alternative syntax

// Assert equal with message
string name = "John";
name.AssertEqual("John", "Names should match");

// Assert not equal
int x = 5;
x.AssertNotEqual(10);
```

### Floating Point Comparisons

```csharp
using Ecng.UnitTesting;

// Compare doubles with delta
double value = 3.14159;
value.AssertEqual(3.14, delta: 0.01);

// Compare floats with delta
float f = 2.5f;
f.AssertEqual(2.5f, delta: 0.001f);
```

### String Assertions

```csharp
using Ecng.UnitTesting;

// String equality with null-as-empty option
string str1 = "";
string str2 = null;
str1.AssertEqual(str2, nullAsEmpty: true); // Passes

// Contains substring
string message = "Hello World";
message.AssertContains("World");
message.AssertContains("llo", "Should contain 'llo'");
```

### Type Assertions

```csharp
using Ecng.UnitTesting;

// Assert type
object obj = "test";
obj.AssertOfType<string>();

// Assert not of type
object number = 42;
number.AssertNotOfType<string>();
```

### Reference Assertions

```csharp
using Ecng.UnitTesting;

// Same instance
var obj1 = new object();
var obj2 = obj1;
obj1.AssertSame(obj2);

// Not same instance
var obj3 = new object();
var obj4 = new object();
obj3.AssertNotSame(obj4);
```

## Collection Assertions

### Enumerable Equality

```csharp
using Ecng.UnitTesting;
using System.Collections.Generic;

// Compare enumerables element by element
var list1 = new List<int> { 1, 2, 3 };
var list2 = new List<int> { 1, 2, 3 };
list1.AssertEqual(list2);

// Works with arrays
int[] arr1 = { 1, 2, 3 };
int[] arr2 = { 1, 2, 3 };
arr1.AssertEqual(arr2);

// Handles null collections
List<int> nullList = null;
nullList.AssertEqual(null); // Passes
```

### Contains Assertions

```csharp
using Ecng.UnitTesting;
using System.Collections.Generic;

// Assert collection contains element
var numbers = new List<int> { 1, 2, 3, 4, 5 };
numbers.AssertContains(3);
numbers.AssertContains(5, "Should contain 5");
```

## Comparison Assertions

### Greater/Less Than

```csharp
using Ecng.UnitTesting;

// Assert greater than
int value = 10;
value.AssertGreater(5);
value.AssertGreater(5, "Value should be greater than 5");

// Assert less than
int small = 3;
small.AssertLess(10);
small.AssertLess(10, "Value should be less than 10");
```

### Range Assertions

```csharp
using Ecng.UnitTesting;

// Assert value is in range (exclusive)
int value = 5;
value.AssertInRange(min: 1, max: 10);

// Works with any IComparable<T>
double price = 99.99;
price.AssertInRange(50.0, 150.0);

DateTime date = DateTime.Now;
date.AssertInRange(DateTime.Today, DateTime.Today.AddDays(1));
```

## Base Test Class Methods

The `BaseTestClass` provides static methods mirroring standard MSTest assertions.

### Basic Assertions

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CalculatorTests : BaseTestClass
{
    [TestMethod]
    public void TestAddition()
    {
        int result = 2 + 2;
        AreEqual(4, result);
        IsTrue(result == 4);
        IsNotNull(result);
    }

    [TestMethod]
    public void TestDivision()
    {
        double result = 10.0 / 3.0;
        AreEqual(3.333, result, delta: 0.001);
    }
}
```

### Exception Assertions

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class ExceptionTests : BaseTestClass
{
    [TestMethod]
    public void TestThrows()
    {
        // Assert that an exception is thrown
        var ex = Throws<ArgumentNullException>(() =>
        {
            string test = null;
            test.ToString();
        });

        IsNotNull(ex);
    }

    [TestMethod]
    public void TestThrowsExactType()
    {
        // Assert exact exception type (not derived)
        ThrowsExactly<InvalidOperationException>(() =>
        {
            throw new InvalidOperationException("Test");
        });
    }

    [TestMethod]
    public async Task TestThrowsAsync()
    {
        // Assert async exception
        await ThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(10);
            throw new ArgumentException("Test");
        });
    }
}
```

### String Assertions

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class StringTests : BaseTestClass
{
    [TestMethod]
    public void TestStringMethods()
    {
        string text = "Hello World";

        Contains("World", text);
        StartsWith("Hello", text);
        EndsWith("World", text);

        IsNotNullOrEmpty(text);
        IsNotNullOrWhiteSpace(text);
    }

    [TestMethod]
    public void TestEmptyStrings()
    {
        string empty = "";
        IsEmpty(empty);
        IsNullOrEmpty(empty);

        string whitespace = "   ";
        IsNullOrWhiteSpace(whitespace);
    }
}
```

### Regex Assertions

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

[TestClass]
public class RegexTests : BaseTestClass
{
    [TestMethod]
    public void TestEmailPattern()
    {
        var emailPattern = new Regex(@"^[^@]+@[^@]+\.[^@]+$");

        string validEmail = "user@example.com";
        MatchesRegex(emailPattern, validEmail);

        string invalidEmail = "not-an-email";
        DoesNotMatch(emailPattern, invalidEmail);
    }
}
```

### Collection Assertions

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

[TestClass]
public class CollectionTests : BaseTestClass
{
    [TestMethod]
    public void TestCollections()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };

        // Equal collections
        AreEqual(list1, list2);

        // Count assertion
        HasCount(3, list1);

        // Contains
        Contains(list1, 2);
        DoesNotContain(list1, 5);

        // All items not null
        var objects = new List<object> { new(), new(), new() };
        AllItemsAreNotNull(objects);

        // All unique
        var unique = new List<int> { 1, 2, 3 };
        AllItemsAreUnique(unique);

        // All same type
        var strings = new List<object> { "a", "b", "c" };
        AllItemsAreInstancesOfType(strings, typeof(string));
    }

    [TestMethod]
    public void TestSubsets()
    {
        var superset = new List<int> { 1, 2, 3, 4, 5 };
        var subset = new List<int> { 2, 3, 4 };

        IsSubsetOf(subset, superset);
    }

    [TestMethod]
    public void TestEquivalence()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 2, 1 }; // Different order

        AreEquivalent(list1, list2); // Passes - same elements
    }
}
```

### Numeric Assertions

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class NumericTests : BaseTestClass
{
    [TestMethod]
    public void TestNumericValues()
    {
        int positive = 42;
        IsPositive(positive);

        int negative = -5;
        IsNegative(negative);

        int zero = 0;
        IsZero(zero);

        int nonZero = 100;
        IsNotZero(nonZero);
    }

    [TestMethod]
    public void TestComparisons()
    {
        int value = 10;

        IsGreater(value, 5);
        IsGreaterOrEqual(value, 10);
        IsLess(value, 20);
        IsLessOrEqual(value, 10);

        IsInRange(value, min: 5, max: 15);
        IsNotInRange(value, min: 20, max: 30);
    }

    [TestMethod]
    public void TestDoublesAndDecimals()
    {
        double dbl = 3.14;
        IsPositive(dbl);

        decimal dec = -2.5m;
        IsNegative(dec);

        long lng = 0L;
        IsZero(lng);
    }
}
```

## Advanced Examples

### Custom Test Base with Setup

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public abstract class DatabaseTestBase : BaseTestClass
{
    protected Database Database { get; private set; }

    [TestInitialize]
    public void SetupDatabase()
    {
        // Runs before each test
        Database = new Database(GetSecret("DB_CONNECTION"));
        Database.Connect();
    }

    [TestCleanup]
    public void CleanupDatabase()
    {
        // Runs after each test
        Database?.Dispose();
    }
}

[TestClass]
public class UserRepositoryTests : DatabaseTestBase
{
    [TestMethod]
    public void TestCreateUser()
    {
        var repo = new UserRepository(Database);
        var user = repo.CreateUser("test@example.com");

        IsNotNull(user);
        IsPositive(user.Id);
    }
}
```

### Complex Object Comparison

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ObjectTests : BaseTestClass
{
    [TestMethod]
    public void TestComplexObjects()
    {
        var person1 = new Person
        {
            Name = "John",
            Age = 30,
            Emails = new[] { "john@example.com" }
        };

        var person2 = new Person
        {
            Name = "John",
            Age = 30,
            Emails = new[] { "john@example.com" }
        };

        // Deep comparison using AssertEqual
        person1.Name.AssertEqual(person2.Name);
        person1.Age.AssertEqual(person2.Age);
        person1.Emails.AssertEqual(person2.Emails);
    }
}
```

### Environment-Specific Tests

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EnvironmentTests : BaseTestClass
{
    [TestMethod]
    public void TestEnvironmentVariable()
    {
        string value = Env("PATH");
        IsNotNullOrEmpty(value);
    }

    [TestMethod]
    public void TestLocalHostOnly()
    {
        if (!IsLocalHost)
        {
            Inconclusive("This test only runs on localhost");
            return;
        }

        // Test code that should only run locally
        IsTrue(true);
    }

    [TestMethod]
    public void TestCIOnly()
    {
        if (IsLocalHost)
        {
            Inconclusive("This test only runs in CI");
            return;
        }

        // Test code that should only run in CI
        IsTrue(true);
    }
}
```

### Fluent Test Style

```csharp
using Ecng.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FluentTests : BaseTestClass
{
    [TestMethod]
    public void TestFluentStyle()
    {
        var result = Calculate(10, 5);

        result.AssertNotNull()
              .AssertGreater(0)
              .AssertLess(100)
              .AssertEqual(50);
    }

    private int? Calculate(int a, int b)
    {
        return a * b;
    }
}

// Extension to chain assertions
public static class AssertExtensions
{
    public static T AssertNotNull<T>(this T value) where T : class
    {
        value.AssertNotNull();
        return value;
    }

    public static T AssertGreater<T>(this T value, T min) where T : IComparable<T>
    {
        value.AssertGreater(min);
        return value;
    }

    public static T AssertLess<T>(this T value, T max) where T : IComparable<T>
    {
        value.AssertLess(max);
        return value;
    }

    public static T AssertEqual<T>(this T value, T expected)
    {
        value.AssertEqual(expected);
        return value;
    }
}
```

## Secrets File Format

Create a `secrets.json` file in your test project or solution root:

```json
{
  "API_KEY": "your-api-key-here",
  "API_ENDPOINT": "https://api.example.com",
  "DB_CONNECTION": "Server=localhost;Database=test;",
  "BACKUP_AWS_REGION": "us-east-1"
}
```

The file will be automatically discovered by searching up to 8 parent directories from the test assembly location.

## CI Environment Variables Detected

The library detects the following CI environments:

- GitHub Actions (GITHUB_ACTIONS)
- GitLab CI (GITLAB_CI)
- Jenkins (JENKINS_URL)
- Azure Pipelines (TF_BUILD)
- Travis CI (TRAVIS)
- CircleCI (CIRCLECI)
- TeamCity (TEAMCITY_VERSION)
- Buildkite (BUILDKITE)
- Generic CI flag (CI)

## Platform Support

- .NET Standard 2.0+
- .NET 6.0+
- .NET 10.0+

## Dependencies

- Microsoft.VisualStudio.TestTools.UnitTesting (MSTest)
- Ecng.Common
- System.Text.Json (for secrets management)

## Best Practices

1. **Use Fluent Assertions**: Prefer extension methods for more readable tests
2. **Handle Secrets Safely**: Use environment variables or secrets.json, never commit secrets
3. **Skip CI When Needed**: Use SkipInCI for tests requiring local resources
4. **Provide Clear Messages**: Always add descriptive messages to assertions
5. **Use BaseTestClass**: Inherit from BaseTestClass for consistent test infrastructure
6. **Test Cleanup**: Use TestCleanup or implement IDisposable for resource cleanup
7. **Environment Detection**: Use IsLocalHost to conditionally run tests

## See Also

- [MSTest Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [Ecng.Common](../Common/) - Common utilities used by this library
