# Ecng.Logging

A flexible and powerful logging framework for .NET applications providing multiple log outputs, severity filtering, and hierarchical log sources.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Getting Started](#getting-started)
- [Core Concepts](#core-concepts)
- [Log Levels](#log-levels)
- [Logging Infrastructure](#logging-infrastructure)
- [Log Listeners](#log-listeners)
- [Advanced Features](#advanced-features)
- [Configuration](#configuration)
- [Best Practices](#best-practices)

## Overview

Ecng.Logging provides a comprehensive logging solution with support for multiple output targets (console, file, email, debug output), configurable log levels, hierarchical logging sources, and both synchronous and asynchronous operations.

## Key Features

- **Multiple Log Targets**: Console, File, Debug Output, Email
- **Severity Filtering**: Verbose, Debug, Info, Warning, Error levels
- **Hierarchical Logging**: Parent-child log source relationships with inherited settings
- **Async Support**: Asynchronous message processing with configurable flush intervals
- **File Management**: Rolling file support, date-based separation, compression, and archival
- **Flexible Formatting**: Customizable date/time formats
- **Message Filtering**: Custom filters for selective message processing
- **Performance Optimized**: Lazy message evaluation and efficient batch processing

## Getting Started

### Basic Setup

```csharp
using Ecng.Logging;

// Create log manager
var logManager = new LogManager();

// Add console listener
logManager.Listeners.Add(new ConsoleLogListener());

// Get application log source
var log = logManager.Application;

// Write messages
log.LogInfo("Application started");
log.LogWarning("This is a warning");
log.LogError("An error occurred");
```

### Quick Example

```csharp
// Create and configure logging
var logManager = new LogManager();
logManager.Listeners.Add(new FileLogListener("app.log"));
logManager.Listeners.Add(new ConsoleLogListener());

// Set log level
logManager.Application.LogLevel = LogLevels.Info;

// Log messages
logManager.Application.LogInfo("Processing data...");
logManager.Application.LogWarning("Cache miss detected");
logManager.Application.LogError("Failed to connect: {0}", ex.Message);

// Cleanup
logManager.Dispose();
```

## Core Concepts

### ILogSource

The base interface for any object that can produce log messages. Every log source has:

- **Id**: Unique identifier
- **Name**: Display name for the source
- **Parent**: Optional parent source for hierarchical logging
- **LogLevel**: Minimum severity level to log
- **IsRoot**: Whether this source is a root source
- **Log Event**: Raised when a message is logged

```csharp
public interface ILogSource : IDisposable
{
    Guid Id { get; }
    string Name { get; set; }
    ILogSource Parent { get; set; }
    LogLevels LogLevel { get; set; }
    DateTime CurrentTimeUtc { get; }
    bool IsRoot { get; }
    event Action<LogMessage> Log;
}
```

### ILogReceiver

Extends ILogSource with methods to write log messages at different severity levels.

```csharp
public interface ILogReceiver : ILogSource
{
    void AddLog(LogMessage message);
    void LogVerbose(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
}
```

### Creating Custom Log Sources

```csharp
// Using the built-in implementation
var logSource = new LogReceiver("MyComponent");
logSource.LogLevel = LogLevels.Debug;
logSource.LogInfo("Component initialized");

// Creating a custom log receiver
public class MyService : BaseLogReceiver
{
    public MyService()
    {
        Name = "MyService";
        LogLevel = LogLevels.Info;
    }

    public void ProcessData()
    {
        LogDebug("Starting data processing");
        try
        {
            // ... processing logic ...
            LogInfo("Processed {0} records", recordCount);
        }
        catch (Exception ex)
        {
            LogError(ex);
        }
    }
}
```

### Hierarchical Logging

Create parent-child relationships between log sources:

```csharp
var application = new LogReceiver("MyApp");
application.LogLevel = LogLevels.Info;

var database = new LogReceiver("Database");
database.Parent = application;
database.LogLevel = LogLevels.Inherit; // Inherits Info from parent

var network = new LogReceiver("Network");
network.Parent = application;
network.LogLevel = LogLevels.Debug; // Override to Debug

// Messages propagate up to parent
database.LogInfo("Connection established"); // Will be logged
database.LogDebug("Query executed"); // Won't be logged (level is Info)
network.LogDebug("Packet sent"); // Will be logged (level is Debug)
```

## Log Levels

The framework supports six severity levels plus inheritance:

```csharp
public enum LogLevels
{
    Inherit,   // Use parent's log level
    Verbose,   // Most detailed: Verbose, Debug, Info, Warning, Error
    Debug,     // Debug, Info, Warning, Error
    Info,      // Info, Warning, Error
    Warning,   // Warning, Error only
    Error,     // Errors only
    Off        // Logging disabled
}
```

### Using Log Levels

```csharp
var log = new LogReceiver("Service");

// Set minimum level to log
log.LogLevel = LogLevels.Info;

// These will be logged
log.LogInfo("Service started");
log.LogWarning("Low memory detected");
log.LogError("Service failed");

// These will be ignored
log.LogVerbose("Detailed trace information");
log.LogDebug("Variable x = {0}", x);

// Dynamically check effective level
var effectiveLevel = log.GetLogLevel();
```

## Logging Infrastructure

### LogManager

The central logging hub that manages log sources and listeners.

```csharp
public class LogManager : IDisposable
{
    public ILogReceiver Application { get; set; }
    public IList<ILogListener> Listeners { get; }
    public IList<ILogSource> Sources { get; }
    public TimeSpan FlushInterval { get; set; }
    public bool ClearPendingOnDispose { get; set; }
}
```

#### Basic Configuration

```csharp
// Create with async mode (default)
var logManager = new LogManager();

// Or create with synchronous mode
var logManager = new LogManager(asyncMode: false);

// Configure flush interval (async mode only)
logManager.FlushInterval = TimeSpan.FromMilliseconds(200);

// Add listeners
logManager.Listeners.Add(new ConsoleLogListener());
logManager.Listeners.Add(new FileLogListener("app.log"));

// Add custom log sources
var service = new LogReceiver("MyService");
logManager.Sources.Add(service);

// Access application-level logger
logManager.Application.LogInfo("Application initialized");

// Configure behavior on dispose
logManager.ClearPendingOnDispose = true; // Clear pending messages
```

#### Global Instance

```csharp
// LogManager.Instance is automatically set to the first instance created
var logManager = new LogManager();

// Access from anywhere in the application
LogManager.Instance.Application.LogInfo("Global log message");

// Extension method for exceptions
try
{
    // ... code ...
}
catch (Exception ex)
{
    ex.LogError(); // Logs to LogManager.Instance.Application
}
```

### LogMessage

Represents a single log entry.

```csharp
public class LogMessage
{
    public ILogSource Source { get; set; }
    public DateTime TimeUtc { get; set; }
    public LogLevels Level { get; }
    public string Message { get; }
    public bool IsDispose { get; }
}
```

#### Creating Log Messages

```csharp
// Direct message
var message = new LogMessage(
    source: myLogSource,
    time: DateTime.UtcNow,
    level: LogLevels.Info,
    message: "User logged in",
    args: Array.Empty<object>()
);

// With lazy message evaluation (for expensive operations)
var message = new LogMessage(
    source: myLogSource,
    time: DateTime.UtcNow,
    level: LogLevels.Debug,
    getMessage: () => GenerateExpensiveDebugInfo()
);

// Using extension methods (recommended)
receiver.AddInfoLog("Processing {0} items", count);
receiver.AddErrorLog(exception);
```

## Log Listeners

### Console Listener

Outputs log messages to the console with colored output.

```csharp
var consoleListener = new ConsoleLogListener();
consoleListener.IsLocalTime = true; // Convert UTC to local time
consoleListener.TimeFormat = "HH:mm:ss.fff";
consoleListener.DateFormat = "yyyy/MM/dd";

logManager.Listeners.Add(consoleListener);

// Output format:
// 2025/12/20 14:30:45.123 | MyService      | User logged in
```

### File Listener

Writes log messages to text files with extensive configuration options.

#### Basic File Logging

```csharp
// Log to a single file
var fileListener = new FileLogListener("application.log");
logManager.Listeners.Add(fileListener);
```

#### Advanced File Configuration

```csharp
var fileListener = new FileLogListener();

// Basic settings
fileListener.FileName = "app";
fileListener.Extension = ".log";
fileListener.LogDirectory = @"C:\Logs";
fileListener.Append = true;

// Encoding
fileListener.Encoding = Encoding.UTF8;

// Time formatting
fileListener.IsLocalTime = true;
fileListener.TimeFormat = "HH:mm:ss.fff";
fileListener.DateFormat = "yyyy/MM/dd";

// Include source ID in logs
fileListener.WriteSourceId = true;

// Child data handling
fileListener.WriteChildDataToRootFile = true;

logManager.Listeners.Add(fileListener);
```

#### Rolling Files

Automatically create new files when size limit is reached:

```csharp
var fileListener = new FileLogListener("app.log");

// Enable rolling when file reaches 10 MB
fileListener.MaxLength = 10 * 1024 * 1024; // 10 MB

// Keep only the last 5 rolled files
fileListener.MaxCount = 5;

logManager.Listeners.Add(fileListener);

// Creates: app.log, app.1.log, app.2.log, app.3.log, app.4.log, app.5.log
// Oldest files are deleted when MaxCount is reached
```

#### Date-Based File Separation

```csharp
var fileListener = new FileLogListener();
fileListener.FileName = "app";
fileListener.LogDirectory = @"C:\Logs";

// Option 1: Separate by filename
fileListener.SeparateByDates = SeparateByDateModes.FileName;
fileListener.DirectoryDateFormat = "yyyy_MM_dd";
// Creates: app.log, 2025_12_20_app.log, 2025_12_21_app.log, etc.

// Option 2: Separate by subdirectories
fileListener.SeparateByDates = SeparateByDateModes.SubDirectories;
fileListener.DirectoryDateFormat = "yyyy_MM_dd";
// Creates: C:\Logs\2025_12_20\app.log, C:\Logs\2025_12_21\app.log, etc.

logManager.Listeners.Add(fileListener);
```

#### Log History Management

Automatically manage old log files:

```csharp
var fileListener = new FileLogListener();
fileListener.SeparateByDates = SeparateByDateModes.SubDirectories;

// Delete logs older than 7 days
fileListener.HistoryPolicy = FileLogHistoryPolicies.Delete;
fileListener.HistoryAfter = TimeSpan.FromDays(7);

// Or compress logs older than 7 days
fileListener.HistoryPolicy = FileLogHistoryPolicies.Compression;
fileListener.HistoryAfter = TimeSpan.FromDays(7);
fileListener.HistoryCompressionLevel = CompressionLevel.Optimal;

// Or move logs to archive directory
fileListener.HistoryPolicy = FileLogHistoryPolicies.Move;
fileListener.HistoryAfter = TimeSpan.FromDays(7);
fileListener.HistoryMove = @"C:\Archive\Logs";

logManager.Listeners.Add(fileListener);
```

### Debug Listener

Outputs to the debug window (System.Diagnostics.Trace).

```csharp
var debugListener = new DebugLogListener();
logManager.Listeners.Add(debugListener);

// Messages appear in Visual Studio Output window or other debug output viewers
```

### Email Listener

Sends log messages via email (typically for critical errors).

```csharp
public class CustomEmailListener : EmailLogListener
{
    protected override SmtpClient CreateClient()
    {
        return new SmtpClient("smtp.example.com", 587)
        {
            Credentials = new NetworkCredential("user@example.com", "password"),
            EnableSsl = true
        };
    }
}

var emailListener = new CustomEmailListener();
emailListener.From = "app@example.com";
emailListener.To = "admin@example.com";

// Add filter to only send errors
emailListener.Filters.Add(LoggingHelper.OnlyError);

logManager.Listeners.Add(emailListener);
```

### Custom Listener

Create custom log listeners:

```csharp
// Synchronous listener
public class DatabaseLogListener : LogListener
{
    private readonly string _connectionString;

    public DatabaseLogListener(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnWriteMessage(LogMessage message)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = connection.CreateCommand();

        command.CommandText =
            "INSERT INTO Logs (TimeUtc, Level, Source, Message) " +
            "VALUES (@Time, @Level, @Source, @Message)";

        command.Parameters.AddWithValue("@Time", message.TimeUtc);
        command.Parameters.AddWithValue("@Level", message.Level.ToString());
        command.Parameters.AddWithValue("@Source", message.Source.Name);
        command.Parameters.AddWithValue("@Message", message.Message);

        connection.Open();
        command.ExecuteNonQuery();
    }
}

// Asynchronous listener
public class AsyncDatabaseLogListener : LogListener
{
    private readonly string _connectionString;

    public AsyncDatabaseLogListener(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override async ValueTask OnWriteMessagesAsync(
        IEnumerable<LogMessage> messages,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var message in messages)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO Logs (TimeUtc, Level, Source, Message) " +
                "VALUES (@Time, @Level, @Source, @Message)";

            command.Parameters.AddWithValue("@Time", message.TimeUtc);
            command.Parameters.AddWithValue("@Level", message.Level.ToString());
            command.Parameters.AddWithValue("@Source", message.Source.Name);
            command.Parameters.AddWithValue("@Message", message.Message);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
```

## Advanced Features

### Message Filtering

Filter messages before they are processed:

```csharp
var fileListener = new FileLogListener("errors.log");

// Only log errors
fileListener.Filters.Add(LoggingHelper.OnlyError);

// Only log warnings
fileListener.Filters.Add(LoggingHelper.OnlyWarning);

// Custom filter
fileListener.Filters.Add(msg =>
    msg.Source.Name.StartsWith("Database") &&
    msg.Level >= LogLevels.Warning
);

// Multiple filters (OR logic - any filter that returns true)
fileListener.Filters.Add(LoggingHelper.OnlyError);
fileListener.Filters.Add(LoggingHelper.OnlyWarning);
// Logs warnings OR errors

logManager.Listeners.Add(fileListener);
```

### Lazy Message Evaluation

Avoid expensive string operations when messages won't be logged:

```csharp
// Bad: String is always formatted, even if not logged
log.LogDebug(string.Format("Data: {0}", ExpensiveOperation()));

// Good: Only evaluated if debug logging is enabled
log.AddDebugLog(() => $"Data: {ExpensiveOperation()}");

// Also good: Uses extension method with format args
log.LogDebug("Data: {0}", ExpensiveOperation());

// Best: Check level first for very expensive operations
if (log.LogLevel <= LogLevels.Debug)
{
    var data = VeryExpensiveOperation();
    log.LogDebug("Complex data: {0}", data);
}
```

### Error Handling Helpers

Wrap operations with automatic error logging:

```csharp
// Synchronous
Action action = () => RiskyOperation();
action.DoWithLog(); // Logs exception if thrown

Func<int> func = () => GetValue();
int result = func.DoWithLog(); // Returns default(int) on exception

// Asynchronous
Func<CancellationToken, Task> asyncAction =
    async ct => await RiskyAsyncOperation(ct);
await asyncAction.DoWithLogAsync();

Func<CancellationToken, Task<string>> asyncFunc =
    async ct => await GetValueAsync(ct);
string result = await asyncFunc.DoWithLogAsync();

// Direct exception logging
try
{
    DangerousOperation();
}
catch (Exception ex)
{
    ex.LogError(); // Logs to LogManager.Instance.Application
    ex.LogError("Custom format: {0}"); // With custom message
}
```

### Multiple Listeners with Different Levels

```csharp
var logManager = new LogManager();
var service = new LogReceiver("MyService");
logManager.Sources.Add(service);

// Console: Only errors and warnings
var consoleListener = new ConsoleLogListener();
consoleListener.Filters.Add(msg =>
    msg.Level >= LogLevels.Warning
);
logManager.Listeners.Add(consoleListener);

// File: Everything at Info and above
var fileListener = new FileLogListener("app.log");
logManager.Listeners.Add(fileListener);

// Email: Only critical errors
var emailListener = new CustomEmailListener
{
    From = "app@example.com",
    To = "admin@example.com"
};
emailListener.Filters.Add(LoggingHelper.OnlyError);
logManager.Listeners.Add(emailListener);

service.LogLevel = LogLevels.Info;
service.LogInfo("Info message");      // File only
service.LogWarning("Warning");        // Console + File
service.LogError("Critical error");   // Console + File + Email
```

### Per-Source File Logging

```csharp
// Create listener without specific filename
var fileListener = new FileLogListener();
fileListener.LogDirectory = @"C:\Logs";
fileListener.WriteChildDataToRootFile = false; // Each source gets own file

logManager.Listeners.Add(fileListener);

// Each source creates its own log file
var database = new LogReceiver("Database");
var network = new LogReceiver("Network");
var cache = new LogReceiver("Cache");

logManager.Sources.Add(database);
logManager.Sources.Add(network);
logManager.Sources.Add(cache);

// Creates: C:\Logs\Database.txt, C:\Logs\Network.txt, C:\Logs\Cache.txt

database.LogInfo("Connection opened");  // -> Database.txt
network.LogInfo("Request sent");        // -> Network.txt
cache.LogInfo("Cache hit");             // -> Cache.txt
```

## Configuration

### Saving and Loading Configuration

```csharp
// Save configuration
var storage = new SettingsStorage();
logManager.Save(storage);
var json = storage.Serialize(); // Serialize to JSON or other format

// Load configuration
var storage = SettingsStorage.Deserialize(json);
var logManager = new LogManager();
logManager.Load(storage);
```

### Example Configuration

```csharp
var logManager = new LogManager();

// Application logger
logManager.Application.Name = "MyApplication";
logManager.Application.LogLevel = LogLevels.Info;

// Console listener
var consoleListener = new ConsoleLogListener
{
    IsLocalTime = true,
    TimeFormat = "HH:mm:ss",
    DateFormat = "yyyy-MM-dd"
};
logManager.Listeners.Add(consoleListener);

// File listener with rolling
var fileListener = new FileLogListener
{
    FileName = "app",
    Extension = ".log",
    LogDirectory = @"C:\Logs",
    MaxLength = 10 * 1024 * 1024, // 10 MB
    MaxCount = 10,
    SeparateByDates = SeparateByDateModes.SubDirectories,
    DirectoryDateFormat = "yyyy_MM_dd",
    HistoryPolicy = FileLogHistoryPolicies.Compression,
    HistoryAfter = TimeSpan.FromDays(30),
    HistoryCompressionLevel = CompressionLevel.Optimal,
    IsLocalTime = true
};
logManager.Listeners.Add(fileListener);

// Flush configuration
logManager.FlushInterval = TimeSpan.FromMilliseconds(500);
```

## Best Practices

### 1. Use Appropriate Log Levels

```csharp
// Verbose: Extremely detailed trace information
log.LogVerbose("Loop iteration {0}, variable state: {1}", i, state);

// Debug: Diagnostic information useful for debugging
log.LogDebug("Cache miss for key: {0}", key);

// Info: General informational messages
log.LogInfo("Service started successfully");

// Warning: Potentially harmful situations
log.LogWarning("Retry attempt {0} of {1}", attempt, maxAttempts);

// Error: Error events that might still allow the application to continue
log.LogError("Failed to process record {0}: {1}", id, ex.Message);
```

### 2. Use Hierarchical Logging

```csharp
var app = new LogReceiver("Application");
app.LogLevel = LogLevels.Info;

var dataLayer = new LogReceiver("DataLayer") { Parent = app };
var businessLayer = new LogReceiver("BusinessLayer") { Parent = app };
var apiLayer = new LogReceiver("ApiLayer") { Parent = app };

// Override level for specific component
dataLayer.LogLevel = LogLevels.Debug;

// All inherit Info except dataLayer (Debug)
```

### 3. Dispose Properly

```csharp
// Use using statement
using (var logManager = new LogManager())
{
    logManager.Listeners.Add(new FileLogListener("app.log"));
    logManager.Application.LogInfo("Application started");

    // ... application logic ...

} // Automatically flushes and disposes

// Or dispose explicitly
var logManager = new LogManager();
try
{
    // ... use logger ...
}
finally
{
    logManager.Dispose(); // Ensures all messages are flushed
}
```

### 4. Avoid String Concatenation in Log Calls

```csharp
// Bad: String is always created
log.LogInfo("Processing " + count + " items at " + DateTime.Now);

// Good: Uses format string (only formatted if logged)
log.LogInfo("Processing {0} items at {1}", count, DateTime.Now);

// Better: For expensive operations
if (log.LogLevel <= LogLevels.Info)
{
    log.LogInfo("Processing {0} items at {1}", count, DateTime.Now);
}
```

### 5. Use Filters Wisely

```csharp
// Create specialized listeners
var errorFileListener = new FileLogListener("errors.log");
errorFileListener.Filters.Add(LoggingHelper.OnlyError);

var warningFileListener = new FileLogListener("warnings.log");
warningFileListener.Filters.Add(LoggingHelper.OnlyWarning);

var allFileListener = new FileLogListener("all.log");
// No filters - logs everything

logManager.Listeners.Add(errorFileListener);
logManager.Listeners.Add(warningFileListener);
logManager.Listeners.Add(allFileListener);
```

### 6. Configure for Production

```csharp
#if DEBUG
    logManager.Application.LogLevel = LogLevels.Debug;
    logManager.Listeners.Add(new ConsoleLogListener());
    logManager.Listeners.Add(new DebugLogListener());
#else
    logManager.Application.LogLevel = LogLevels.Info;

    var fileListener = new FileLogListener
    {
        FileName = "production",
        LogDirectory = @"C:\ProgramData\MyApp\Logs",
        MaxLength = 50 * 1024 * 1024, // 50 MB
        MaxCount = 20,
        SeparateByDates = SeparateByDateModes.SubDirectories,
        HistoryPolicy = FileLogHistoryPolicies.Compression,
        HistoryAfter = TimeSpan.FromDays(90)
    };
    logManager.Listeners.Add(fileListener);
#endif
```

### 7. Thread Safety

The framework is thread-safe. Multiple threads can log simultaneously:

```csharp
var logManager = new LogManager();
var log = logManager.Application;

// Safe to call from multiple threads
Parallel.For(0, 100, i =>
{
    log.LogInfo("Processing item {0} on thread {1}",
        i, Thread.CurrentThread.ManagedThreadId);
});
```

### 8. Monitor Performance

```csharp
// Async mode with appropriate flush interval
var logManager = new LogManager(asyncMode: true);
logManager.FlushInterval = TimeSpan.FromMilliseconds(500); // Adjust as needed

// For high-volume logging, increase flush interval
logManager.FlushInterval = TimeSpan.FromSeconds(1);

// For low-latency requirements, decrease flush interval
logManager.FlushInterval = TimeSpan.FromMilliseconds(100);

// For maximum performance, use synchronous mode only for low-volume scenarios
var syncLogManager = new LogManager(asyncMode: false);
```

## License

This is part of the Ecng framework. Refer to the project root for license information.
