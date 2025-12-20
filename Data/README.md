# Ecng.Data

A lightweight library providing common abstractions for data access layers, including database connection management, caching, and provider abstractions.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Key Features](#key-features)
- [Core Components](#core-components)
- [Usage Examples](#usage-examples)
  - [Basic Connection Management](#basic-connection-management)
  - [Connection Caching](#connection-caching)
  - [Event Handling](#event-handling)
  - [Persistence](#persistence)
  - [Provider Registry](#provider-registry)
- [API Reference](#api-reference)
- [Advanced Scenarios](#advanced-scenarios)

## Overview

Ecng.Data provides a simple yet powerful abstraction layer for managing database connections in .NET applications. It focuses on:

- **Reusable connection definitions** - Define connections once, use them everywhere
- **Connection caching** - Automatically manage and deduplicate connection configurations
- **Persistence** - Save and load connection configurations
- **Provider abstraction** - Work with multiple database providers through a unified interface
- **Event-driven architecture** - React to connection lifecycle events

This library is designed to be database-agnostic and works seamlessly with various data access technologies, including Linq2Db, ADO.NET, and other ORMs.

## Installation

Add a reference to the `Ecng.Data` project or NuGet package in your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\Ecng\Data\Data.csproj" />
</ItemGroup>
```

For Linq2Db integration, also add:

```xml
<ItemGroup>
  <ProjectReference Include="..\Ecng\Data.Linq2db\Data.Linq2db.csproj" />
</ItemGroup>
```

## Key Features

- **DatabaseConnectionPair** - Encapsulates provider type and connection string
- **DatabaseConnectionCache** - Thread-safe cache for managing connection definitions
- **IDatabaseProviderRegistry** - Interface for provider discovery and connection verification
- **IPersistable** - Built-in serialization support for saving/loading configurations
- **Event notifications** - Track connection creation, deletion, and cache updates
- **Equality comparison** - Case-insensitive comparison of connection pairs

## Core Components

### DatabaseConnectionPair

Represents a database connection configuration with a provider and connection string.

```csharp
public class DatabaseConnectionPair : NotifiableObject, IPersistable
{
    public string Provider { get; set; }
    public string ConnectionString { get; set; }
    public string Title { get; }  // Read-only: "({Provider}) {ConnectionString}"
}
```

### DatabaseConnectionCache

A thread-safe cache for managing database connection pairs with automatic deduplication.

```csharp
public class DatabaseConnectionCache : IPersistable
{
    public IEnumerable<DatabaseConnectionPair> Connections { get; }
    public DatabaseConnectionPair GetOrAdd(string provider, string connectionString);
    public bool DeleteConnection(DatabaseConnectionPair connection);

    public event Action<DatabaseConnectionPair> ConnectionCreated;
    public event Action<DatabaseConnectionPair> ConnectionDeleted;
    public event Action Updated;
}
```

### IDatabaseProviderRegistry

Interface for database provider management and connection verification.

```csharp
public interface IDatabaseProviderRegistry
{
    string[] Providers { get; }
    void Verify(DatabaseConnectionPair connection);
}
```

## Usage Examples

### Basic Connection Management

#### Creating a Connection Pair

```csharp
using Ecng.Data;

// Create a connection pair
var pair = new DatabaseConnectionPair
{
    Provider = "SqlServer",
    ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=True;"
};

// Display the connection
Console.WriteLine(pair.Title);
// Output: (SqlServer) Server=localhost;Database=MyDb;Trusted_Connection=True;
```

#### Using with Linq2Db

```csharp
using Ecng.Data;
using LinqToDB;
using LinqToDB.Data;

// Create connection pair
var pair = new DatabaseConnectionPair
{
    Provider = ProviderName.SqlServer,
    ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=True;"
};

// Create and use a database connection
using (var connection = pair.CreateConnection())
{
    // Execute queries using Linq2Db
    var users = connection.GetTable<User>()
        .Where(u => u.IsActive)
        .ToList();

    // Execute SQL commands
    connection.Execute("UPDATE Users SET LastLogin = GETDATE() WHERE Id = @id",
        new { id = 123 });
}
```

### Connection Caching

#### Basic Cache Operations

```csharp
using Ecng.Data;
using LinqToDB;

// Create a connection cache
var cache = new DatabaseConnectionCache();

// Add connections to cache (or retrieve if already exists)
var sqlServerConn = cache.GetOrAdd(
    ProviderName.SqlServer,
    "Server=localhost;Database=MyDb;Trusted_Connection=True;"
);

var sqliteConn = cache.GetOrAdd(
    ProviderName.SQLite,
    "Data Source=mydb.sqlite;Version=3;"
);

var mysqlConn = cache.GetOrAdd(
    ProviderName.MySql,
    "Server=localhost;Database=MyDb;Uid=root;Pwd=password;"
);

// Retrieve all cached connections
foreach (var conn in cache.Connections)
{
    Console.WriteLine($"Cached: {conn.Title}");
}

// Remove a connection
cache.DeleteConnection(sqliteConn);
```

#### Automatic Deduplication

```csharp
var cache = new DatabaseConnectionCache();

// Adding the same connection twice returns the same instance
var conn1 = cache.GetOrAdd("SqlServer", "Server=localhost;Database=MyDb");
var conn2 = cache.GetOrAdd("SqlServer", "Server=localhost;Database=MyDb");

Console.WriteLine(ReferenceEquals(conn1, conn2)); // True
Console.WriteLine(cache.Connections.Count()); // 1

// Case-insensitive comparison
var conn3 = cache.GetOrAdd("SQLSERVER", "server=localhost;database=mydb");
Console.WriteLine(ReferenceEquals(conn1, conn3)); // True
```

### Event Handling

#### Monitoring Connection Lifecycle

```csharp
var cache = new DatabaseConnectionCache();

// Subscribe to events
cache.ConnectionCreated += conn =>
{
    Console.WriteLine($"Connection created: {conn.Title}");
    // Log to file, notify UI, etc.
};

cache.ConnectionDeleted += conn =>
{
    Console.WriteLine($"Connection deleted: {conn.Title}");
    // Clean up resources, update UI, etc.
};

cache.Updated += () =>
{
    Console.WriteLine($"Cache updated. Total connections: {cache.Connections.Count()}");
    // Refresh UI, trigger backup, etc.
};

// Trigger events
var pair = cache.GetOrAdd("SqlServer", "Server=localhost");
// Output: Connection created: (SqlServer) Server=localhost
//         Cache updated. Total connections: 1

cache.DeleteConnection(pair);
// Output: Connection deleted: (SqlServer) Server=localhost
//         Cache updated. Total connections: 0
```

#### Building a Connection Manager UI

```csharp
public class ConnectionManager
{
    private readonly DatabaseConnectionCache _cache;

    public ConnectionManager()
    {
        _cache = new DatabaseConnectionCache();

        // Wire up events to update UI
        _cache.ConnectionCreated += OnConnectionCreated;
        _cache.ConnectionDeleted += OnConnectionDeleted;
        _cache.Updated += OnCacheUpdated;
    }

    public void AddConnection(string provider, string connectionString)
    {
        _cache.GetOrAdd(provider, connectionString);
    }

    public void RemoveConnection(DatabaseConnectionPair connection)
    {
        _cache.DeleteConnection(connection);
    }

    public IEnumerable<DatabaseConnectionPair> GetAllConnections()
    {
        return _cache.Connections;
    }

    private void OnConnectionCreated(DatabaseConnectionPair conn)
    {
        // Update UI list
        // _listView.Items.Add(conn);
    }

    private void OnConnectionDeleted(DatabaseConnectionPair conn)
    {
        // Update UI list
        // _listView.Items.Remove(conn);
    }

    private void OnCacheUpdated()
    {
        // Refresh status bar
        // _statusLabel.Text = $"{_cache.Connections.Count()} connections";
    }
}
```

### Persistence

#### Saving and Loading Connections

```csharp
using Ecng.Data;
using Ecng.Serialization;
using LinqToDB;

// Create and populate cache
var cache = new DatabaseConnectionCache();
cache.GetOrAdd(ProviderName.SqlServer, "Server=localhost;Database=MyDb");
cache.GetOrAdd(ProviderName.SQLite, "Data Source=mydb.sqlite");

// Save to JSON
var serializer = new JsonSerializer<DatabaseConnectionCache>();
var json = serializer.Serialize(cache);
File.WriteAllText("connections.json", json);

// Load from JSON
var jsonContent = File.ReadAllText("connections.json");
var loadedCache = serializer.Deserialize(jsonContent);

// Verify loaded data
foreach (var conn in loadedCache.Connections)
{
    Console.WriteLine($"Loaded: {conn.Title}");
}
```

#### Custom Persistence Storage

```csharp
using Ecng.Serialization;

// Save to custom storage
var cache = new DatabaseConnectionCache();
cache.GetOrAdd("SqlServer", "Server=localhost");

var storage = cache.Save();
// storage is a SettingsStorage object that can be serialized to various formats

// Load from custom storage
var newCache = new DatabaseConnectionCache();
newCache.Load(storage);
```

#### Application Settings Integration

```csharp
public class AppSettings
{
    private readonly DatabaseConnectionCache _connectionCache = new();
    private readonly string _settingsFile = "appsettings.json";

    public DatabaseConnectionCache Connections => _connectionCache;

    public void Load()
    {
        if (File.Exists(_settingsFile))
        {
            var serializer = new JsonSerializer<SettingsStorage>();
            var storage = serializer.Deserialize(File.ReadAllText(_settingsFile));
            _connectionCache.Load(storage);
        }
    }

    public void Save()
    {
        var storage = new SettingsStorage();
        _connectionCache.Save(storage);

        var serializer = new JsonSerializer<SettingsStorage>();
        File.WriteAllText(_settingsFile, serializer.Serialize(storage));
    }
}

// Usage
var settings = new AppSettings();
settings.Load();

// Add connections
settings.Connections.GetOrAdd("SqlServer", "Server=localhost");

// Save changes
settings.Save();
```

### Provider Registry

#### Implementing a Provider Registry

```csharp
using Ecng.Data;
using LinqToDB;

public class CustomDatabaseProviderRegistry : IDatabaseProviderRegistry
{
    public string[] Providers { get; } =
    [
        ProviderName.SqlServer,
        ProviderName.SQLite,
        ProviderName.MySql,
        ProviderName.PostgreSQL,
        ProviderName.Oracle
    ];

    public void Verify(DatabaseConnectionPair connection)
    {
        try
        {
            using var db = connection.CreateConnection();
            using var conn = db.DataProvider.CreateConnection(db.ConnectionString);
            conn.Open();
            Console.WriteLine($"Connection to {connection.Provider} verified successfully");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to verify connection to {connection.Provider}: {ex.Message}",
                ex
            );
        }
    }
}
```

#### Using the Provider Registry

```csharp
var registry = new CustomDatabaseProviderRegistry();

// Display available providers
Console.WriteLine("Available providers:");
foreach (var provider in registry.Providers)
{
    Console.WriteLine($"  - {provider}");
}

// Verify a connection
var connection = new DatabaseConnectionPair
{
    Provider = ProviderName.SqlServer,
    ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=True;"
};

try
{
    registry.Verify(connection);
    Console.WriteLine("Connection is valid!");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection verification failed: {ex.Message}");
}
```

## API Reference

### DatabaseConnectionPair

| Member | Type | Description |
|--------|------|-------------|
| `Provider` | `string` | The database provider type (e.g., "SqlServer", "SQLite", "MySql") |
| `ConnectionString` | `string` | The connection string for the database |
| `Title` | `string` | Read-only formatted string: "({Provider}) {ConnectionString}" |
| `Load(SettingsStorage)` | `void` | Loads the pair from settings storage |
| `Save(SettingsStorage)` | `void` | Saves the pair to settings storage |
| `Equals(object)` | `bool` | Compares pairs using case-insensitive comparison |
| `GetHashCode()` | `int` | Returns case-insensitive hash code |

### DatabaseConnectionCache

| Member | Type | Description |
|--------|------|-------------|
| `Connections` | `IEnumerable<DatabaseConnectionPair>` | Gets all cached connection pairs |
| `GetOrAdd(string, string)` | `DatabaseConnectionPair` | Gets existing or adds new connection pair |
| `DeleteConnection(DatabaseConnectionPair)` | `bool` | Removes connection from cache |
| `Load(SettingsStorage)` | `void` | Loads connections from storage |
| `Save(SettingsStorage)` | `void` | Saves connections to storage |
| `ConnectionCreated` | `event Action<DatabaseConnectionPair>` | Raised when a new connection is added |
| `ConnectionDeleted` | `event Action<DatabaseConnectionPair>` | Raised when a connection is removed |
| `Updated` | `event Action` | Raised when the cache is modified |

### IDatabaseProviderRegistry

| Member | Type | Description |
|--------|------|-------------|
| `Providers` | `string[]` | Gets the list of available database providers |
| `Verify(DatabaseConnectionPair)` | `void` | Verifies a connection by attempting to open it |

### Extension Methods (Linq2Db Integration)

| Method | Return Type | Description |
|--------|-------------|-------------|
| `CreateConnection(this DatabaseConnectionPair)` | `DataConnection` | Creates a Linq2Db DataConnection from the pair |

## Advanced Scenarios

### Pooling Multiple Database Connections

```csharp
public class MultiDatabaseService
{
    private readonly DatabaseConnectionCache _cache;

    public MultiDatabaseService()
    {
        _cache = new DatabaseConnectionCache();

        // Set up connections for different databases
        _cache.GetOrAdd(ProviderName.SqlServer, "Server=localhost;Database=MainDb;...");
        _cache.GetOrAdd(ProviderName.SQLite, "Data Source=cache.sqlite;...");
        _cache.GetOrAdd(ProviderName.MySql, "Server=localhost;Database=Analytics;...");
    }

    public void ExecuteOnAll(Action<DatabaseConnectionPair> action)
    {
        foreach (var connection in _cache.Connections)
        {
            action(connection);
        }
    }

    public T ExecuteQuery<T>(string provider, Func<DataConnection, T> query)
    {
        var pair = _cache.Connections.FirstOrDefault(c =>
            c.Provider.EqualsIgnoreCase(provider));

        if (pair == null)
            throw new InvalidOperationException($"Provider {provider} not found");

        using var connection = pair.CreateConnection();
        return query(connection);
    }
}

// Usage
var service = new MultiDatabaseService();

var sqlServerUsers = service.ExecuteQuery(ProviderName.SqlServer, db =>
    db.GetTable<User>().ToList()
);

service.ExecuteOnAll(pair =>
{
    using var conn = pair.CreateConnection();
    Console.WriteLine($"Testing {pair.Provider}...");
    conn.Execute("SELECT 1");
});
```

### Connection Testing and Health Checks

```csharp
public class ConnectionHealthChecker
{
    private readonly DatabaseConnectionCache _cache;
    private readonly IDatabaseProviderRegistry _registry;

    public ConnectionHealthChecker(
        DatabaseConnectionCache cache,
        IDatabaseProviderRegistry registry)
    {
        _cache = cache;
        _registry = registry;
    }

    public Dictionary<DatabaseConnectionPair, bool> CheckAllConnections()
    {
        var results = new Dictionary<DatabaseConnectionPair, bool>();

        foreach (var connection in _cache.Connections)
        {
            try
            {
                _registry.Verify(connection);
                results[connection] = true;
                Console.WriteLine($"✓ {connection.Title}");
            }
            catch (Exception ex)
            {
                results[connection] = false;
                Console.WriteLine($"✗ {connection.Title}: {ex.Message}");
            }
        }

        return results;
    }

    public async Task<bool> MonitorConnectionAsync(
        DatabaseConnectionPair connection,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _registry.Verify(connection);
                await Task.Delay(interval, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection lost: {ex.Message}");
                return false;
            }
        }

        return true;
    }
}
```

### Dynamic Configuration Management

```csharp
public class DynamicConnectionManager
{
    private readonly DatabaseConnectionCache _cache;
    private readonly IConfiguration _configuration;

    public DynamicConnectionManager(IConfiguration configuration)
    {
        _configuration = configuration;
        _cache = new DatabaseConnectionCache();
        LoadFromConfiguration();
    }

    private void LoadFromConfiguration()
    {
        var connections = _configuration
            .GetSection("DatabaseConnections")
            .GetChildren();

        foreach (var connConfig in connections)
        {
            var provider = connConfig["Provider"];
            var connectionString = connConfig["ConnectionString"];

            if (!string.IsNullOrEmpty(provider) &&
                !string.IsNullOrEmpty(connectionString))
            {
                _cache.GetOrAdd(provider, connectionString);
            }
        }
    }

    public void ReloadConfiguration()
    {
        // Clear and reload
        foreach (var conn in _cache.Connections.ToList())
        {
            _cache.DeleteConnection(conn);
        }

        LoadFromConfiguration();
    }

    public DatabaseConnectionPair GetConnection(string name)
    {
        return _cache.Connections.FirstOrDefault(c =>
            c.Title.Contains(name, StringComparison.OrdinalIgnoreCase)
        );
    }
}

// appsettings.json example:
// {
//   "DatabaseConnections": [
//     {
//       "Provider": "SqlServer",
//       "ConnectionString": "Server=localhost;Database=MyDb;..."
//     },
//     {
//       "Provider": "SQLite",
//       "ConnectionString": "Data Source=mydb.sqlite"
//     }
//   ]
// }
```

### Thread-Safe Operations

```csharp
public class ConcurrentConnectionManager
{
    private readonly DatabaseConnectionCache _cache;
    private readonly object _lock = new();

    public ConcurrentConnectionManager()
    {
        _cache = new DatabaseConnectionCache();
    }

    public DatabaseConnectionPair SafeGetOrAdd(string provider, string connectionString)
    {
        lock (_lock)
        {
            return _cache.GetOrAdd(provider, connectionString);
        }
    }

    public async Task<List<TResult>> ExecuteInParallel<TResult>(
        Func<DatabaseConnectionPair, TResult> operation)
    {
        var tasks = _cache.Connections
            .Select(conn => Task.Run(() => operation(conn)))
            .ToList();

        return (await Task.WhenAll(tasks)).ToList();
    }
}

// Usage
var manager = new ConcurrentConnectionManager();

// Execute queries in parallel across all databases
var results = await manager.ExecuteInParallel(pair =>
{
    using var conn = pair.CreateConnection();
    return conn.GetTable<User>().Count();
});

Console.WriteLine($"Total users across all databases: {results.Sum()}");
```

## Best Practices

1. **Always dispose connections**: Use `using` statements when creating connections
2. **Validate connection strings**: Verify connections before using them in production
3. **Use caching**: Leverage `DatabaseConnectionCache` to avoid duplicate connection definitions
4. **Handle events**: Subscribe to cache events for logging and monitoring
5. **Persist configurations**: Save connection cache to disk for application state management
6. **Thread safety**: The cache is thread-safe, but always consider synchronization in multi-threaded scenarios
7. **Security**: Never hard-code connection strings; use configuration files or secure storage
8. **Provider names**: Use constants from `LinqToDB.ProviderName` for consistency

## License

Part of the StockSharp project. See the main repository for license information.

## Related Projects

- **Ecng.ComponentModel** - Base component model and notification support
- **Ecng.Serialization** - Serialization infrastructure used by this library
- **Ecng.Data.Linq2db** - Linq2Db integration extensions
