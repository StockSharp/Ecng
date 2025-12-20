# Server.Utils

Utilities for hosting and managing background services, providing service path helpers, logging integration, and configuration management.

## Overview

Server.Utils is a library designed to simplify the creation and management of background services and server applications. It provides:
- Service path and directory utilities
- Integration with Microsoft.Extensions.Logging
- Log management and configuration
- Base settings classes for server applications

## Installation

This library is part of the Ecng framework and targets .NET Standard 2.0, .NET 6.0, and .NET 10.0.

## Components

### ServicePath

Static utility class providing service directory paths and logging configuration.

#### Service Directory Properties

Access common service directories:

```csharp
using Ecng.Server.Utils;

// Get the directory where the service executable is located
string serviceDirectory = ServicePath.ServiceDir;
Console.WriteLine($"Service directory: {serviceDirectory}");

// Get the data directory (ServiceDir/Data)
string dataDirectory = ServicePath.DataDir;
Console.WriteLine($"Data directory: {dataDirectory}");
```

#### Create and Configure LogManager

Create a fully configured LogManager with file logging and service logging:

```csharp
using Ecng.Server.Utils;
using Ecng.Logging;
using Microsoft.Extensions.Logging;

public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly LogManager _logManager;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;

        // Create LogManager with automatic configuration
        _logManager = logger.CreateLogManager(
            dataDir: ServicePath.DataDir,
            defaultLevel: LogLevels.Info
        );

        // Use the log manager
        _logManager.Sources.Add(new LogSource { Name = "MyService" });
    }

    public void DoWork()
    {
        // Log through Ecng LogManager
        var source = _logManager.Sources[0];
        source.AddInfoLog("Service is working...");

        // Also logs to Microsoft.Extensions.Logging via ServiceLogListener
    }
}
```

#### Service Restart

Trigger a service restart:

```csharp
using Ecng.Server.Utils;

public void RestartService()
{
    // This will exit with code 1, which can be configured
    // in service configuration to trigger automatic restart
    ServicePath.Restart();
}
```

### ServiceLogListener

Bridges Ecng logging to Microsoft.Extensions.Logging, allowing you to use both logging systems simultaneously.

#### Basic Usage

```csharp
using Ecng.Logging;
using Ecng.Server.Utils;
using Microsoft.Extensions.Logging;

// In your service startup
ILogger logger = loggerFactory.CreateLogger("MyService");

var logManager = new LogManager();

// Add the service log listener
logManager.Listeners.Add(new ServiceLogListener(logger));

// Now all logs written to LogManager will also appear in Microsoft.Extensions.Logging
var source = new LogSource { Name = "MySource" };
logManager.Sources.Add(source);

source.AddInfoLog("This message appears in both logging systems");
```

#### Integration with ASP.NET Core

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ecng.Logging;
using Ecng.Server.Utils;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Get logger from DI
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Create LogManager with ServiceLogListener integration
var logManager = logger.CreateLogManager(
    ServicePath.DataDir,
    LogLevels.Debug
);

// Use LogManager in your application
app.Run();
```

#### Log Level Mapping

The ServiceLogListener maps Ecng log levels to Microsoft.Extensions.Logging levels:

| Ecng LogLevel | Microsoft.Extensions.Logging |
|---------------|------------------------------|
| Verbose       | Trace                        |
| Debug         | Debug                        |
| Info          | Information                  |
| Warning       | Warning                      |
| Error         | Error                        |

### ServiceSettingsBase

Abstract base class for service settings with common configuration properties.

#### Create Custom Settings

```csharp
using Ecng.Server.Utils;
using Ecng.Logging;

public class MyServiceSettings : ServiceSettingsBase
{
    public MyServiceSettings()
    {
        // Set default values
        WebApiAddress = "http://localhost:5000";
        LogLevel = LogLevels.Info;
    }

    // Add your custom settings
    public string DatabaseConnectionString { get; set; }
    public int MaxConcurrentRequests { get; set; } = 100;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

#### Use Settings in Service

```csharp
using Ecng.Server.Utils;
using Microsoft.Extensions.Configuration;

public class MyService
{
    private readonly MyServiceSettings _settings;

    public MyService(IConfiguration configuration)
    {
        _settings = new MyServiceSettings
        {
            WebApiAddress = configuration["WebApi:Address"],
            LogLevel = Enum.Parse<LogLevels>(configuration["Logging:Level"]),
            DatabaseConnectionString = configuration["Database:ConnectionString"]
        };
    }

    public void Start()
    {
        Console.WriteLine($"Starting web API at {_settings.WebApiAddress}");
        Console.WriteLine($"Log level: {_settings.LogLevel}");
    }
}
```

## Complete Usage Examples

### Windows Service with Logging

```csharp
using Ecng.Logging;
using Ecng.Server.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            });
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private LogManager _logManager;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize LogManager with file logging
        _logManager = _logger.CreateLogManager(
            ServicePath.DataDir,
            LogLevels.Info
        );

        var source = new LogSource { Name = "Worker" };
        _logManager.Sources.Add(source);

        while (!stoppingToken.IsCancellationRequested)
        {
            source.AddInfoLog("Worker running at: {Time}", DateTimeOffset.Now);
            await Task.Delay(10000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _logManager?.Dispose();
        base.Dispose();
    }
}
```

### ASP.NET Core Service with Custom Settings

```csharp
using Ecng.Logging;
using Ecng.Server.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure settings
var settings = new MyServiceSettings
{
    WebApiAddress = builder.Configuration["WebApi:Address"],
    LogLevel = Enum.Parse<LogLevels>(builder.Configuration["Logging:DefaultLevel"] ?? "Info")
};

builder.Services.AddSingleton(settings);

var app = builder.Build();

// Initialize logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var logManager = logger.CreateLogManager(ServicePath.DataDir, settings.LogLevel);

app.MapGet("/", () => "Service is running");
app.MapGet("/restart", () =>
{
    Task.Run(() => ServicePath.Restart());
    return "Restarting...";
});

app.Run(settings.WebApiAddress);
```

### Standalone Console Application

```csharp
using Ecng.Logging;
using Ecng.Server.Utils;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main(string[] args)
    {
        // Setup Microsoft.Extensions.Logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        // Create LogManager with dual logging
        var logManager = logger.CreateLogManager(
            ServicePath.DataDir,
            LogLevels.Debug
        );

        // Add a source
        var source = new LogSource { Name = "MyApp" };
        logManager.Sources.Add(source);

        // Log messages (appears in both console and file)
        source.AddInfoLog("Application started");
        source.AddDebugLog("Debug information");
        source.AddWarningLog("Warning message");

        // Application logic
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        source.AddInfoLog("Application stopped");
        logManager.Dispose();
    }
}
```

## Configuration Files

The LogManager created by `CreateLogManager` automatically saves and loads settings:

```
ServiceDir/
  Data/
    logManager.json  (or .xml depending on serializer)
    Logs/
      2025-12-20/
        logs.txt
```

Example logManager.json:
```json
{
  "Application": {
    "LogLevel": "Info"
  },
  "Listeners": [
    {
      "Type": "FileLogListener",
      "Append": true,
      "FileName": "logs",
      "LogDirectory": "C:\\Service\\Data\\Logs",
      "SeparateByDates": "SubDirectories"
    }
  ]
}
```

## Requirements

- .NET Standard 2.0, .NET 6.0, or .NET 10.0
- Dependencies:
  - Ecng.Logging
  - Ecng.Serialization
  - Microsoft.Extensions.Logging (for ServiceLogListener)

## Notes

- `ServicePath.Restart()` exits with code 1; configure your service manager to restart on this exit code
- Log settings are automatically persisted to the data directory
- File logs are automatically separated by date into subdirectories
- ServiceLogListener does not support persistence (CanSave = false)

## Thread Safety

- `ServicePath`: Thread-safe (static properties and methods)
- `ServiceLogListener`: Thread-safe for logging operations
- `ServiceSettingsBase`: Not thread-safe; properties should be set during initialization

## License

Part of the Ecng framework. See the main repository for licensing information.
