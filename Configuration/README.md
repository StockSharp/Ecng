# Ecng.Configuration

A lightweight and efficient configuration management library for .NET applications, providing typed access to configuration sections, application settings, and a flexible service registry.

## Overview

Ecng.Configuration wraps and extends the .NET `System.Configuration` framework, offering:

- **Type-safe access** to configuration sections and groups
- **Service registry pattern** for dependency injection and service location
- **Event-driven notifications** for service registration
- **AppSettings helpers** with type conversion
- **Thread-safe operations** for concurrent access

## Installation

This library requires:
- .NET Standard 2.0+ or .NET 6.0+
- `System.Configuration.ConfigurationManager` package

```xml
<PackageReference Include="System.Configuration.ConfigurationManager" />
```

## Key Features

### 1. Configuration Sections

Access configuration sections by type or name with built-in caching for performance.

#### Access by Type

```csharp
using Ecng.Configuration;

// Get a custom configuration section by type
var section = ConfigManager.GetSection<MyCustomSection>();

// Section will be null if not found
if (section != null)
{
    // Use the section
    var value = section.SomeProperty;
}
```

#### Access by Name

```csharp
// Get section by name
var connectionStrings = ConfigManager.GetSection<ConnectionStringsSection>("connectionStrings");

// Or without type parameter
var section = ConfigManager.GetSection("mySection");
```

#### Example Configuration File

```xml
<configuration>
  <configSections>
    <section name="mySection" type="MyNamespace.MyCustomSection, MyAssembly"/>
  </configSections>

  <mySection someProperty="value" />
</configuration>
```

### 2. Configuration Section Groups

Organize related configuration sections into groups.

```csharp
// Get a section group by type
var group = ConfigManager.GetGroup<MyConfigurationGroup>();

// Get a section group by name
var systemWeb = ConfigManager.GetGroup<SystemWebSectionGroup>("system.web");
```

### 3. Application Settings

Simple access to AppSettings with type conversion support.

```csharp
// Get a setting with default value
int timeout = ConfigManager.TryGet<int>("RequestTimeout", 30);

// Get a boolean setting
bool enableLogging = ConfigManager.TryGet<bool>("EnableLogging", false);

// Get a string setting
string apiUrl = ConfigManager.TryGet<string>("ApiUrl", "https://default-api.com");

// Direct access to all AppSettings
var allSettings = ConfigManager.AppSettings;
foreach (string key in allSettings.AllKeys)
{
    Console.WriteLine($"{key} = {allSettings[key]}");
}
```

**Configuration File:**

```xml
<configuration>
  <appSettings>
    <add key="RequestTimeout" value="60" />
    <add key="EnableLogging" value="true" />
    <add key="ApiUrl" value="https://api.example.com" />
  </appSettings>
</configuration>
```

### 4. Service Registry

A lightweight service locator pattern for managing application services without heavy DI frameworks.

#### Register Services

```csharp
// Register a service with default name (type's AssemblyQualifiedName)
var securityProvider = new CollectionSecurityProvider(securities);
ConfigManager.RegisterService<ISecurityProvider>(securityProvider);

// Register a service with a custom name
ConfigManager.RegisterService<ILogger>("FileLogger", new FileLogger());
ConfigManager.RegisterService<ILogger>("ConsoleLogger", new ConsoleLogger());
```

#### Retrieve Services

```csharp
// Get a service by type (using default name)
var provider = ConfigManager.GetService<ISecurityProvider>();

// Get a service by name
var fileLogger = ConfigManager.GetService<ILogger>("FileLogger");
var consoleLogger = ConfigManager.GetService<ILogger>("ConsoleLogger");

// Try to get a service (returns default if not found)
var optionalService = ConfigManager.TryGetService<IOptionalFeature>();
if (optionalService != null)
{
    // Service is available
}
```

#### Check Service Registration

```csharp
// Check if a service is registered
if (ConfigManager.IsServiceRegistered<IMessageAdapter>())
{
    var adapter = ConfigManager.GetService<IMessageAdapter>();
}

// Check by name
if (ConfigManager.IsServiceRegistered<ILogger>("FileLogger"))
{
    // Use the file logger
}
```

#### Conditional Registration

```csharp
// Register only if not already registered
ConfigManager.TryRegisterService<IDefaultService>(new DefaultServiceImpl());

// This won't override the existing registration
ConfigManager.TryRegisterService<IDefaultService>(new AnotherImpl());
```

#### Get All Services of a Type

```csharp
// Get all registered services of a specific type
var allLoggers = ConfigManager.GetServices<ILogger>();

foreach (var logger in allLoggers)
{
    logger.Log("Application started");
}
```

### 5. Service Registration Events

React to service registration with event handlers or subscriptions.

#### Global Event Handler

```csharp
// Subscribe to all service registrations
ConfigManager.ServiceRegistered += (type, service) =>
{
    Console.WriteLine($"Service registered: {type.Name}");
};

// Now register a service
ConfigManager.RegisterService<IMyService>(new MyServiceImpl());
// Output: Service registered: IMyService
```

#### Type-Specific Subscription

```csharp
// Subscribe to a specific service type registration
ConfigManager.SubscribeOnRegister<ISecurityProvider>(provider =>
{
    Console.WriteLine($"SecurityProvider registered with {provider.Count} securities");
    // Initialize or configure the provider
});

// When this service is registered, the callback will be invoked
ConfigManager.RegisterService<ISecurityProvider>(new CollectionSecurityProvider(securities));
```

### 6. Service Fallback

Provide a fallback mechanism to create services on-demand when they're not registered.

```csharp
// Set up a service fallback handler
ConfigManager.ServiceFallback += (type, name) =>
{
    // Create services dynamically based on type
    if (type == typeof(ILogger))
    {
        return new DefaultLogger();
    }

    if (type == typeof(ICache))
    {
        return new MemoryCache();
    }

    // Return null to indicate service cannot be created
    return null;
};

// Now getting an unregistered service will use the fallback
var logger = ConfigManager.GetService<ILogger>(); // Creates DefaultLogger
```

### 7. Access Underlying Configuration

For advanced scenarios, access the underlying .NET Configuration object.

```csharp
// Get the underlying Configuration object
var config = ConfigManager.InnerConfig;

if (config != null)
{
    Console.WriteLine($"Configuration file: {config.FilePath}");

    // Access sections directly
    foreach (ConfigurationSection section in config.Sections)
    {
        Console.WriteLine($"Section: {section.SectionInformation.Name}");
    }
}
```

## Complete Usage Example

Here's a complete example showing typical usage in an application:

```csharp
using Ecng.Configuration;
using System;

namespace MyApplication
{
    public class Program
    {
        static void Main(string[] args)
        {
            // 1. Configure service fallback (optional)
            ConfigManager.ServiceFallback += CreateDefaultServices;

            // 2. Subscribe to service registrations (optional)
            ConfigManager.SubscribeOnRegister<ILogger>(logger =>
            {
                logger.Initialize();
            });

            // 3. Register core services
            ConfigManager.RegisterService<ILogger>(new FileLogger("app.log"));
            ConfigManager.RegisterService<IExchangeInfoProvider>(
                new InMemoryExchangeInfoProvider()
            );

            // 4. Register multiple implementations with names
            ConfigManager.RegisterService<ICache>("Memory", new MemoryCache());
            ConfigManager.RegisterService<ICache>("Distributed", new RedisCache());

            // 5. Read application settings
            var timeout = ConfigManager.TryGet<int>("Timeout", 30);
            var apiUrl = ConfigManager.TryGet<string>("ApiUrl");

            // 6. Get configuration section
            var customSection = ConfigManager.GetSection<CustomAppSettings>();

            // 7. Use services throughout your application
            RunApplication();
        }

        static void RunApplication()
        {
            // Retrieve services when needed
            var logger = ConfigManager.GetService<ILogger>();
            logger.Log("Application started");

            var provider = ConfigManager.GetService<IExchangeInfoProvider>();
            var exchanges = provider.GetExchanges();

            // Use named service
            var cache = ConfigManager.GetService<ICache>("Memory");
            cache.Set("key", "value");
        }

        static object CreateDefaultServices(Type type, string name)
        {
            // Provide default implementations
            if (type == typeof(ILogger))
                return new ConsoleLogger();

            return null;
        }
    }

    // Example interfaces
    public interface ILogger
    {
        void Initialize();
        void Log(string message);
    }

    public interface IExchangeInfoProvider
    {
        IEnumerable<Exchange> GetExchanges();
    }

    public interface ICache
    {
        void Set(string key, string value);
        string Get(string key);
    }
}
```

## Thread Safety

All operations in `ConfigManager` are thread-safe:

- Configuration sections and groups are cached on first access
- Service registry operations use locking to prevent race conditions
- Multiple threads can safely register and retrieve services concurrently

```csharp
// Safe to call from multiple threads
Parallel.For(0, 100, i =>
{
    var service = ConfigManager.GetService<IMyService>();
    service.DoWork();
});
```

## Best Practices

### 1. Register Services at Application Startup

```csharp
// Register all services during application initialization
public static void ConfigureServices()
{
    ConfigManager.RegisterService<ILogger>(new Logger());
    ConfigManager.RegisterService<IDataService>(new DataService());
    // ... register other services
}
```

### 2. Use Type-Safe Access

```csharp
// Prefer type-safe access over string-based access
var section = ConfigManager.GetSection<MySection>();  // Good
var section = (MySection)ConfigManager.GetSection("mySection");  // Less safe
```

### 3. Provide Defaults for AppSettings

```csharp
// Always provide sensible defaults
var timeout = ConfigManager.TryGet<int>("Timeout", 30);  // Good
var timeout = ConfigManager.TryGet<int>("Timeout");  // May return 0
```

### 4. Check Service Registration Before Use

```csharp
// For optional services, check registration first
if (ConfigManager.IsServiceRegistered<IOptionalFeature>())
{
    var feature = ConfigManager.GetService<IOptionalFeature>();
    feature.Execute();
}
```

### 5. Use Named Services for Multiple Implementations

```csharp
// Register different implementations with descriptive names
ConfigManager.RegisterService<IRepository>("User", new UserRepository());
ConfigManager.RegisterService<IRepository>("Product", new ProductRepository());

// Retrieve specific implementation
var userRepo = ConfigManager.GetService<IRepository>("User");
```

## Common Patterns

### Singleton Services

```csharp
// Register singleton services
var singletonService = new MySingletonService();
ConfigManager.RegisterService<IMySingletonService>(singletonService);

// Every call returns the same instance
var service1 = ConfigManager.GetService<IMySingletonService>();
var service2 = ConfigManager.GetService<IMySingletonService>();
// service1 == service2
```

### Factory Pattern with Fallback

```csharp
// Use fallback as a factory
ConfigManager.ServiceFallback += (type, name) =>
{
    if (type == typeof(IDataService))
    {
        // Create with dependencies
        var logger = ConfigManager.GetService<ILogger>();
        return new DataService(logger);
    }
    return null;
};

// First call creates and registers the service
var service = ConfigManager.GetService<IDataService>();
```

### Configuration-Driven Service Selection

```csharp
// Select service implementation based on configuration
var loggerType = ConfigManager.TryGet<string>("LoggerType", "Console");

ILogger logger = loggerType switch
{
    "File" => new FileLogger(),
    "Console" => new ConsoleLogger(),
    "Database" => new DatabaseLogger(),
    _ => new ConsoleLogger()
};

ConfigManager.RegisterService<ILogger>(logger);
```

## Migration from .NET ConfigurationManager

If you're migrating from using `ConfigurationManager` directly:

```csharp
// Before:
var setting = ConfigurationManager.AppSettings["MySetting"];

// After:
var setting = ConfigManager.AppSettings["MySetting"];
// Or with type conversion:
var typedSetting = ConfigManager.TryGet<int>("MySetting", 0);

// Before:
var section = (MySection)ConfigurationManager.GetSection("mySection");

// After:
var section = ConfigManager.GetSection<MySection>("mySection");
```

## Limitations

- Configuration file must exist and be valid at application startup
- Configuration sections are cached on first load (not reloaded automatically)
- Service registry is in-memory only (not persisted)
- Not a replacement for full dependency injection frameworks (suitable for simpler scenarios)

## See Also

- [System.Configuration documentation](https://learn.microsoft.com/en-us/dotnet/api/system.configuration)
- [ConfigurationManager class](https://learn.microsoft.com/en-us/dotnet/api/system.configuration.configurationmanager)

## License

Part of the StockSharp/Ecng framework.
