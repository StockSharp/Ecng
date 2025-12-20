# Ecng.Localization

A lightweight and flexible localization engine for .NET applications that provides centralized string resource management with support for multiple cultures and easy resource overrides.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Usage Guide](#usage-guide)
- [Advanced Scenarios](#advanced-scenarios)
- [Integration with .NET Attributes](#integration-with-net-attributes)
- [Best Practices](#best-practices)
- [API Reference](#api-reference)

## Overview

Ecng.Localization provides a simple yet powerful localization infrastructure that allows you to:
- Centralize all localizable strings in your application
- Support multiple languages and cultures
- Implement custom translation providers
- Integrate seamlessly with .NET data annotations
- Maintain type-safe resource keys

The library uses a provider pattern through the `ILocalizer` interface, making it easy to plug in custom localization strategies ranging from simple in-memory dictionaries to database-backed translation systems.

## Key Features

- **Pluggable Architecture**: Implement custom localizers via `ILocalizer` interface
- **Two Localization Modes**: Translate by English text or by resource key
- **Resource Type Support**: Works with `DisplayAttribute` for declarative localization
- **Extension Methods**: Convenient string extension methods for inline localization
- **Thread-Safe**: Can be safely used in multi-threaded applications
- **No External Dependencies**: Minimal footprint with no third-party dependencies
- **Fallback Behavior**: Returns original text when translation is not found
- **Type-Safe Keys**: Const keys prevent typos in resource references

## Installation

Add a reference to the `Ecng.Localization` project or NuGet package in your application:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\Ecng.Localization.csproj" />
</ItemGroup>
```

## Quick Start

### Basic Usage

```csharp
using Ecng.Localization;

// Use the default localizer (returns input as-is)
string text = "Hello".Localize();
Console.WriteLine(text); // Output: Hello

// Use predefined resource keys
string name = LocalizedStrings.Name;
Console.WriteLine(name); // Output: Name (in English by default)
```

### Implementing Custom Localization

```csharp
using Ecng.Localization;

// 1. Create a custom localizer
public class RussianLocalizer : ILocalizer
{
    private readonly Dictionary<string, string> _translations = new()
    {
        ["Name"] = "Имя",
        ["Warnings"] = "Предупреждения",
        ["Errors"] = "Ошибки",
        ["Info"] = "Информация"
    };

    public string Localize(string enStr)
    {
        // Translate by English text
        return _translations.GetValueOrDefault(enStr, enStr);
    }

    public string LocalizeByKey(string key)
    {
        // Translate by resource key
        return _translations.GetValueOrDefault(key, key);
    }
}

// 2. Register the localizer globally
LocalizedStrings.Localizer = new RussianLocalizer();

// 3. Use localized strings throughout your application
Console.WriteLine(LocalizedStrings.Name);      // Output: Имя
Console.WriteLine(LocalizedStrings.Warnings);  // Output: Предупреждения
Console.WriteLine("Name".Localize());          // Output: Имя
```

## Core Concepts

### ILocalizer Interface

The `ILocalizer` interface is the foundation of the localization system:

```csharp
public interface ILocalizer
{
    /// <summary>
    /// Localizes a string by its English text.
    /// </summary>
    string Localize(string enStr);

    /// <summary>
    /// Localizes a string by its resource key.
    /// </summary>
    string LocalizeByKey(string key);
}
```

**Two Translation Approaches:**

1. **Localize(enStr)**: Uses English text as the key. Good for dynamic strings.
2. **LocalizeByKey(key)**: Uses a constant key. Better for compile-time safety.

### LocalizedStrings Class

The static `LocalizedStrings` class provides:
- Global localizer instance via `Localizer` property
- Predefined resource keys (e.g., `NameKey`, `IdKey`, `WarningsKey`)
- Predefined localized string properties (e.g., `Name`, `Id`, `Warnings`)
- Extension methods for string localization

### Default Behavior

By default, the library uses a null localizer that returns strings as-is:

```csharp
// Default behavior (no translation)
"Hello".Localize();           // Returns: "Hello"
"MyKey".LocalizeByKey();      // Returns: "MyKey"
LocalizedStrings.Name;        // Returns: "Name"
```

## Usage Guide

### 1. Using Predefined Resource Keys

The library provides predefined keys for common UI elements:

```csharp
using Ecng.Localization;

// Logging-related resources
Console.WriteLine(LocalizedStrings.Inherited);    // "Inherited"
Console.WriteLine(LocalizedStrings.Verbose);      // "Verbose"
Console.WriteLine(LocalizedStrings.Debug);        // "Debug"
Console.WriteLine(LocalizedStrings.Info);         // "Info"
Console.WriteLine(LocalizedStrings.Warnings);     // "Warnings"
Console.WriteLine(LocalizedStrings.Errors);       // "Errors"
Console.WriteLine(LocalizedStrings.Off);          // "Off"

// General purpose resources
Console.WriteLine(LocalizedStrings.Id);           // "Id"
Console.WriteLine(LocalizedStrings.Name);         // "Name"
Console.WriteLine(LocalizedStrings.Logging);      // "Logging"
Console.WriteLine(LocalizedStrings.LogLevel);     // "LogLevel"

// Chart-related resources
Console.WriteLine(LocalizedStrings.Line2);        // "Line2"
Console.WriteLine(LocalizedStrings.Area);         // "Area"
Console.WriteLine(LocalizedStrings.Histogram);    // "Histogram"
Console.WriteLine(LocalizedStrings.Band);         // "Band"
```

### 2. Using String Extension Methods

Localize any string inline using extension methods:

```csharp
using Ecng.Localization;

// Localize by English text
string message = "Connection established".Localize();

// Localize by key
string errorMsg = "ERROR_CONNECTION".LocalizeByKey();

// Use in string interpolation
Console.WriteLine($"Status: {"Active".Localize()}");
```

### 3. Implementing a Database-Backed Localizer

```csharp
using Ecng.Localization;

public class DatabaseLocalizer : ILocalizer
{
    private readonly ITranslationRepository _repository;
    private readonly string _cultureCode;
    private readonly Dictionary<string, string> _cache;

    public DatabaseLocalizer(ITranslationRepository repository, string cultureCode)
    {
        _repository = repository;
        _cultureCode = cultureCode;
        _cache = new Dictionary<string, string>();
        LoadTranslations();
    }

    private void LoadTranslations()
    {
        // Load all translations for the current culture into cache
        var translations = _repository.GetTranslations(_cultureCode);
        foreach (var translation in translations)
        {
            _cache[translation.Key] = translation.Value;
        }
    }

    public string Localize(string enStr)
    {
        if (_cache.TryGetValue(enStr, out var translation))
            return translation;

        // Log missing translation for later addition
        _repository.LogMissingTranslation(enStr, _cultureCode);
        return enStr;
    }

    public string LocalizeByKey(string key)
    {
        return _cache.GetValueOrDefault(key, key);
    }
}

// Usage
var repository = new SqlTranslationRepository(connectionString);
LocalizedStrings.Localizer = new DatabaseLocalizer(repository, "ru-RU");
```

### 4. Implementing a Resource File Localizer

```csharp
using Ecng.Localization;
using System.Resources;
using System.Globalization;

public class ResourceFileLocalizer : ILocalizer
{
    private readonly ResourceManager _resourceManager;
    private readonly CultureInfo _culture;

    public ResourceFileLocalizer(Type resourceType, CultureInfo culture)
    {
        _resourceManager = new ResourceManager(resourceType);
        _culture = culture;
    }

    public string Localize(string enStr)
    {
        try
        {
            // Try to get the resource using the English string as the key
            var result = _resourceManager.GetString(enStr, _culture);
            return result ?? enStr;
        }
        catch
        {
            return enStr;
        }
    }

    public string LocalizeByKey(string key)
    {
        try
        {
            var result = _resourceManager.GetString(key, _culture);
            return result ?? key;
        }
        catch
        {
            return key;
        }
    }
}

// Usage
LocalizedStrings.Localizer = new ResourceFileLocalizer(
    typeof(MyResources),
    new CultureInfo("fr-FR")
);
```

### 5. Culture-Aware Localizer with Fallback

```csharp
using Ecng.Localization;
using System.Globalization;

public class MultiCultureLocalizer : ILocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations;
    private CultureInfo _currentCulture;

    public MultiCultureLocalizer()
    {
        _translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string>(),
            ["ru-RU"] = new Dictionary<string, string>
            {
                ["Name"] = "Имя",
                ["Errors"] = "Ошибки",
                ["Warnings"] = "Предупреждения"
            },
            ["de-DE"] = new Dictionary<string, string>
            {
                ["Name"] = "Name",
                ["Errors"] = "Fehler",
                ["Warnings"] = "Warnungen"
            }
        };

        _currentCulture = CultureInfo.CurrentUICulture;
    }

    public CultureInfo Culture
    {
        get => _currentCulture;
        set => _currentCulture = value ?? CultureInfo.InvariantCulture;
    }

    public string Localize(string enStr)
    {
        return LocalizeByKey(enStr);
    }

    public string LocalizeByKey(string key)
    {
        // Try exact culture match
        if (_translations.TryGetValue(_currentCulture.Name, out var cultureDict))
        {
            if (cultureDict.TryGetValue(key, out var translation))
                return translation;
        }

        // Try neutral culture (e.g., "ru" from "ru-RU")
        if (!_currentCulture.IsNeutralCulture)
        {
            var neutralCulture = _currentCulture.Parent.Name;
            if (_translations.TryGetValue(neutralCulture, out var neutralDict))
            {
                if (neutralDict.TryGetValue(key, out var translation))
                    return translation;
            }
        }

        // Fallback to English or return key
        if (_translations.TryGetValue("en-US", out var enDict))
        {
            if (enDict.TryGetValue(key, out var enTranslation))
                return enTranslation;
        }

        return key;
    }

    public void AddTranslation(string culture, string key, string value)
    {
        if (!_translations.ContainsKey(culture))
            _translations[culture] = new Dictionary<string, string>();

        _translations[culture][key] = value;
    }
}

// Usage
var localizer = new MultiCultureLocalizer();
localizer.Culture = new CultureInfo("ru-RU");
LocalizedStrings.Localizer = localizer;

Console.WriteLine(LocalizedStrings.Name);     // Output: Имя
Console.WriteLine(LocalizedStrings.Warnings); // Output: Предупреждения
```

## Advanced Scenarios

### Thread-Safe Localizer with Caching

```csharp
using Ecng.Localization;
using System.Collections.Concurrent;

public class CachedLocalizer : ILocalizer
{
    private readonly ILocalizer _innerLocalizer;
    private readonly ConcurrentDictionary<string, string> _cache;
    private readonly int _maxCacheSize;

    public CachedLocalizer(ILocalizer innerLocalizer, int maxCacheSize = 1000)
    {
        _innerLocalizer = innerLocalizer;
        _cache = new ConcurrentDictionary<string, string>();
        _maxCacheSize = maxCacheSize;
    }

    public string Localize(string enStr)
    {
        return _cache.GetOrAdd(enStr, key =>
        {
            if (_cache.Count >= _maxCacheSize)
                _cache.Clear();
            return _innerLocalizer.Localize(key);
        });
    }

    public string LocalizeByKey(string key)
    {
        return _cache.GetOrAdd($"KEY_{key}", _ =>
        {
            if (_cache.Count >= _maxCacheSize)
                _cache.Clear();
            return _innerLocalizer.LocalizeByKey(key);
        });
    }

    public void ClearCache() => _cache.Clear();
}
```

### Composite Localizer with Multiple Sources

```csharp
using Ecng.Localization;

public class CompositeLocalizer : ILocalizer
{
    private readonly List<ILocalizer> _localizers;

    public CompositeLocalizer(params ILocalizer[] localizers)
    {
        _localizers = new List<ILocalizer>(localizers);
    }

    public string Localize(string enStr)
    {
        foreach (var localizer in _localizers)
        {
            var result = localizer.Localize(enStr);
            if (result != enStr)
                return result;
        }
        return enStr;
    }

    public string LocalizeByKey(string key)
    {
        foreach (var localizer in _localizers)
        {
            var result = localizer.LocalizeByKey(key);
            if (result != key)
                return result;
        }
        return key;
    }
}

// Usage: Try database first, then fall back to resource files
var dbLocalizer = new DatabaseLocalizer(repository, "ru-RU");
var fileLocalizer = new ResourceFileLocalizer(typeof(Resources), culture);
LocalizedStrings.Localizer = new CompositeLocalizer(dbLocalizer, fileLocalizer);
```

### Logging Localizer (Debugging)

```csharp
using Ecng.Localization;

public class LoggingLocalizer : ILocalizer
{
    private readonly ILocalizer _innerLocalizer;
    private readonly Action<string> _logger;

    public LoggingLocalizer(ILocalizer innerLocalizer, Action<string> logger)
    {
        _innerLocalizer = innerLocalizer;
        _logger = logger;
    }

    public string Localize(string enStr)
    {
        var result = _innerLocalizer.Localize(enStr);
        _logger($"Localize('{enStr}') -> '{result}'");
        return result;
    }

    public string LocalizeByKey(string key)
    {
        var result = _innerLocalizer.LocalizeByKey(key);
        _logger($"LocalizeByKey('{key}') -> '{result}'");
        return result;
    }
}

// Usage
var baseLocalizer = new RussianLocalizer();
LocalizedStrings.Localizer = new LoggingLocalizer(
    baseLocalizer,
    msg => Console.WriteLine($"[LOCALIZATION] {msg}")
);
```

## Integration with .NET Attributes

The localization system integrates seamlessly with .NET's `DisplayAttribute` for declarative localization in data models:

### Using with Enum Types

```csharp
using System.ComponentModel.DataAnnotations;
using Ecng.Localization;

public enum LogLevels
{
    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.InheritedKey))]
    Inherit,

    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.VerboseKey))]
    Verbose,

    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.DebugKey))]
    Debug,

    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.InfoKey))]
    Info,

    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.WarningsKey))]
    Warning,

    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.ErrorsKey))]
    Error
}

// Usage with Ecng.ComponentModel extensions
using Ecng.ComponentModel;

var level = LogLevels.Warning;
string displayName = level.GetFieldDisplayName(); // Gets localized "Warnings"
```

### Using with Class Properties

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Ecng.Localization;

public class LogSource
{
    [Display(
        ResourceType = typeof(LocalizedStrings),
        Name = nameof(LocalizedStrings.IdKey),
        Description = nameof(LocalizedStrings.IdKey),
        GroupName = nameof(LocalizedStrings.LoggingKey),
        Order = 1)]
    public Guid Id { get; set; }

    [Display(
        ResourceType = typeof(LocalizedStrings),
        Name = nameof(LocalizedStrings.NameKey),
        Description = nameof(LocalizedStrings.LogSourceNameKey),
        GroupName = nameof(LocalizedStrings.LoggingKey),
        Order = 2)]
    public string Name { get; set; }

    [Display(
        ResourceType = typeof(LocalizedStrings),
        Name = nameof(LocalizedStrings.LogLevelKey),
        Description = nameof(LocalizedStrings.LogLevelDescKey),
        GroupName = nameof(LocalizedStrings.LoggingKey),
        Order = 3)]
    public LogLevels LogLevel { get; set; }
}
```

### Custom Resource Keys

Extend `LocalizedStrings` with your own resource keys:

```csharp
using Ecng.Localization;

public static class MyLocalizedStrings
{
    // Define keys
    public const string UserNameKey = nameof(UserName);
    public const string PasswordKey = nameof(Password);
    public const string LoginButtonKey = nameof(LoginButton);

    // Define properties that use the localizer
    public static string UserName => UserNameKey.LocalizeByKey();
    public static string Password => PasswordKey.LocalizeByKey();
    public static string LoginButton => LoginButtonKey.LocalizeByKey();
}

// Usage in attributes
public class LoginModel
{
    [Display(
        ResourceType = typeof(MyLocalizedStrings),
        Name = nameof(MyLocalizedStrings.UserNameKey))]
    public string UserName { get; set; }

    [Display(
        ResourceType = typeof(MyLocalizedStrings),
        Name = nameof(MyLocalizedStrings.PasswordKey))]
    public string Password { get; set; }
}
```

## Best Practices

### 1. Initialize Localizer Early

Set up your localizer during application startup:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Initialize localization before anything else
        InitializeLocalization();

        var app = CreateApplication();
        app.Run();
    }

    private static void InitializeLocalization()
    {
        var culture = GetUserPreferredCulture();
        LocalizedStrings.Localizer = new MyLocalizer(culture);
    }
}
```

### 2. Use Resource Keys for Stability

Prefer `LocalizeByKey()` over `Localize()` for better refactoring support:

```csharp
// Good: Key-based (won't break if English text changes)
public const string ErrorKey = "ERROR_INVALID_INPUT";
string message = ErrorKey.LocalizeByKey();

// Less ideal: Text-based (fragile to typos and changes)
string message = "Invalid input".Localize();
```

### 3. Provide Fallback Behavior

Always return meaningful fallback text:

```csharp
public string Localize(string enStr)
{
    // Try translation
    if (_translations.TryGetValue(enStr, out var result))
        return result;

    // Fallback to original (better than throwing exception)
    return enStr;
}
```

### 4. Don't Set Localizer to Null

The library explicitly prevents null localizers:

```csharp
// This will throw ArgumentNullException
LocalizedStrings.Localizer = null; // ERROR!

// Instead, use a pass-through localizer if needed
LocalizedStrings.Localizer = new PassThroughLocalizer();
```

### 5. Cache Translations When Possible

Avoid repeated lookups for frequently used strings:

```csharp
// Cache in a field or property
private readonly string _errorMessage = LocalizedStrings.Errors;

public void LogError()
{
    // Reuse cached translation
    Console.WriteLine(_errorMessage);
}
```

### 6. Test Missing Translations

Implement tests to catch missing translations:

```csharp
[Test]
public void AllKeysHaveTranslations()
{
    var localizer = new RussianLocalizer();
    LocalizedStrings.Localizer = localizer;

    // Test all predefined keys
    Assert.AreNotEqual("Name", LocalizedStrings.Name);
    Assert.AreNotEqual("Errors", LocalizedStrings.Errors);
    Assert.AreNotEqual("Warnings", LocalizedStrings.Warnings);
}
```

## API Reference

### ILocalizer Interface

```csharp
public interface ILocalizer
{
    /// <summary>
    /// Localizes a string using the English text as the lookup key.
    /// </summary>
    /// <param name="enStr">The English string to localize.</param>
    /// <returns>The localized string, or the original if not found.</returns>
    string Localize(string enStr);

    /// <summary>
    /// Localizes a string using a resource key for lookup.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string LocalizeByKey(string key);
}
```

### LocalizedStrings Class

#### Properties

```csharp
public static class LocalizedStrings
{
    /// <summary>
    /// Gets or sets the global localizer instance.
    /// Cannot be set to null.
    /// </summary>
    public static ILocalizer Localizer { get; set; }

    // Predefined localized string properties
    public static string Inherited { get; }
    public static string Verbose { get; }
    public static string Debug { get; }
    public static string Info { get; }
    public static string Warnings { get; }
    public static string Errors { get; }
    public static string Off { get; }
    public static string Id { get; }
    public static string Logging { get; }
    public static string Name { get; }
    public static string LogSourceName { get; }
    public static string LogLevel { get; }
    public static string LogLevelDesc { get; }
    // ... and more
}
```

#### Resource Keys

```csharp
public static class LocalizedStrings
{
    // Resource key constants for use with DisplayAttribute
    public const string InheritedKey = "Inherited";
    public const string VerboseKey = "Verbose";
    public const string DebugKey = "Debug";
    public const string InfoKey = "Info";
    public const string WarningsKey = "Warnings";
    public const string ErrorsKey = "Errors";
    public const string OffKey = "Off";
    public const string IdKey = "Id";
    public const string LoggingKey = "Logging";
    public const string NameKey = "Name";
    // ... and more
}
```

#### Extension Methods

```csharp
public static class LocalizedStrings
{
    /// <summary>
    /// Localizes a string using English text as the key.
    /// </summary>
    public static string Localize(this string enStr);

    /// <summary>
    /// Localizes a string using a resource key.
    /// </summary>
    public static string LocalizeByKey(this string key);
}
```

### Usage Examples

```csharp
// Get localized string via property
string name = LocalizedStrings.Name;

// Get localized string via extension method
string custom = "My Text".Localize();

// Get localized string via key
string byKey = "CustomKey".LocalizeByKey();

// Use in DisplayAttribute
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NameKey)]
public string UserName { get; set; }
```

## License

This project is part of the Ecng framework. Please refer to the main repository for licensing information.

## Contributing

Contributions are welcome! When adding new localization features:

1. Ensure backward compatibility
2. Add resource keys to `LocalizedStrings` class
3. Provide both key constants and localized properties
4. Update documentation with examples
5. Add unit tests for new functionality

## Support

For issues, questions, or contributions, please refer to the main Ecng repository.
