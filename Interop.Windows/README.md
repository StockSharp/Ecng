# Interop.Windows

A .NET library providing Windows-specific interoperability utilities for working with Windows APIs, DDE (Dynamic Data Exchange), and threading operations.

## Overview

Interop.Windows provides essential Windows platform utilities including:
- Windows API helper methods
- DDE (Dynamic Data Exchange) client and server for Excel integration
- Threading helpers for STA/MTA apartment state management
- Windows security and access control management

## Installation

This library is part of the Ecng framework and targets .NET 6.0+ with Windows-specific features.

## Components

### WinApi

Static utility class providing Windows API helper methods.

#### Get Screen Parameters

Retrieves screen boundaries for a specific window:

```csharp
using Ecng.Interop;

IntPtr windowHandle = // ... your window handle
WinApi.GetScreenParams(windowHandle, out int left, out int top, out int width, out int height);

Console.WriteLine($"Screen: Left={left}, Top={top}, Width={width}, Height={height}");
```

#### Manage Application Auto-Run

Control whether your application starts automatically with Windows:

```csharp
using Ecng.Interop;

// Enable auto-run
string appName = "MyApplication";
string exePath = @"C:\Program Files\MyApp\MyApp.exe";
WinApi.UpdateAutoRun(appName, exePath, enabled: true);

// Disable auto-run
WinApi.UpdateAutoRun(appName, exePath, enabled: false);
```

### WindowsThreadingHelper

Extension methods for managing thread apartment states and executing code in specific threading contexts.

#### Execute Code in STA Thread

Run code that requires Single-Threaded Apartment (e.g., clipboard operations, COM interop):

```csharp
using Ecng.Interop;

// Execute action in STA thread
Action clipboardOperation = () => {
    Clipboard.SetText("Hello from STA thread");
};
clipboardOperation.InvokeAsSTA();

// Execute function in STA thread and get result
Func<string> getClipboardText = () => {
    return Clipboard.GetText();
};
string text = getClipboardText.InvokeAsSTA();
Console.WriteLine($"Clipboard content: {text}");
```

#### Set Thread Apartment State

```csharp
using System.Threading;
using Ecng.Interop;

var thread = new Thread(() => {
    // Your code here
});

// Set to STA
thread.STA().Start();

// Or set to MTA
var mtaThread = new Thread(() => {
    // Your code here
}).MTA();
mtaThread.Start();
```

### WindowsGrandAccess

Manages Windows security permissions for window stations and desktops.

#### Grant Access to Window Station and Desktop

Temporarily grant a user access to the current window station and desktop (useful for service scenarios):

```csharp
using Ecng.Interop;

string username = "DOMAIN\\ServiceAccount";

using (var token = WindowsGrandAccess.GrantAccessToWindowStationAndDesktop(username))
{
    // The specified user now has access to the window station and desktop
    // Perform operations that require this access

    // Access is automatically restored when disposed
}
```

### DDE Integration (Excel)

Classes for integrating with Excel via Dynamic Data Exchange protocol.

#### XlsDdeClient - Send Data to Excel

```csharp
using Ecng.Interop.Dde;

// Configure DDE settings
var settings = new DdeSettings
{
    Server = "EXCEL",
    Topic = "[Book1.xlsx]Sheet1",
    RowOffset = 0,      // Start from row 0
    ColumnOffset = 0,   // Start from column 0
    ShowHeaders = true  // Include header row
};

// Create and start client
using var client = new XlsDdeClient(settings);
client.Start();

// Prepare data
var data = new List<IList<object>>
{
    // Header row
    new List<object> { "Name", "Value", "Date" },
    // Data rows
    new List<object> { "Item 1", 100, DateTime.Now },
    new List<object> { "Item 2", 200, DateTime.Now }
};

// Send data to Excel
client.Poke(data);

// Clean up
client.Stop();
```

#### XlsDdeServer - Receive Data from Excel

```csharp
using Ecng.Interop.Dde;

// Create server with callbacks
var server = new XlsDdeServer(
    service: "MyDdeService",
    poke: (topic, rows) => {
        Console.WriteLine($"Received data for topic: {topic}");
        foreach (var row in rows)
        {
            foreach (var cell in row)
            {
                Console.Write($"{cell}\t");
            }
            Console.WriteLine();
        }
    },
    error: (ex) => {
        Console.WriteLine($"DDE Error: {ex.Message}");
    }
);

// Start the server
server.Start();

// Server is now listening for Excel to send data
// Keep application running...

// When done
server.Dispose();
```

#### DdeSettings Configuration

```csharp
using Ecng.Interop.Dde;
using Ecng.Serialization;

var settings = new DdeSettings
{
    Server = "EXCEL",              // DDE server name
    Topic = "[Book1.xlsx]Sheet1",  // Excel workbook and sheet
    RowOffset = 2,                 // Skip first 2 rows
    ColumnOffset = 1,              // Skip first column
    ShowHeaders = false            // Don't include headers
};

// Settings can be persisted
var storage = new SettingsStorage();
settings.Save(storage);

// And loaded later
var loadedSettings = new DdeSettings();
loadedSettings.Load(storage);

// Clone settings
var clonedSettings = settings.Clone();
```

## Usage Examples

### Complete Excel DDE Data Export

```csharp
using Ecng.Interop.Dde;

public class ExcelExporter
{
    private XlsDdeClient _client;

    public void Initialize()
    {
        var settings = new DdeSettings
        {
            Server = "EXCEL",
            Topic = "[Report.xlsx]Data",
            ShowHeaders = true
        };

        _client = new XlsDdeClient(settings);
        _client.Start();
    }

    public void ExportData(IEnumerable<DataRow> dataRows)
    {
        var excelData = new List<IList<object>>
        {
            // Headers
            new List<object> { "ID", "Name", "Price", "Quantity" }
        };

        // Add data rows
        foreach (var row in dataRows)
        {
            excelData.Add(new List<object>
            {
                row.Id,
                row.Name,
                row.Price,
                row.Quantity
            });
        }

        _client.Poke(excelData);
    }

    public void Cleanup()
    {
        _client?.Stop();
        _client?.Dispose();
    }
}
```

### Windows Service with Clipboard Access

```csharp
using Ecng.Interop;

public class WindowsService
{
    public string GetClipboardContent()
    {
        // Services run in non-interactive sessions
        // Use STA thread to access clipboard
        return new Func<string>(() =>
        {
            try
            {
                return Clipboard.GetText();
            }
            catch
            {
                return string.Empty;
            }
        }).InvokeAsSTA();
    }

    public void SetClipboardContent(string text)
    {
        new Action(() =>
        {
            Clipboard.SetText(text);
        }).InvokeAsSTA();
    }
}
```

## Requirements

- .NET 6.0 or later
- Windows operating system
- For DDE functionality: Microsoft Excel or compatible DDE server
- Dependencies:
  - Ecng.Common
  - Ecng.Collections
  - Ecng.Serialization
  - NDde (for DDE support)
  - Windows.Win32 (for P/Invoke)

## Platform Support

This library is Windows-specific and requires:
- Target framework: `net6.0-windows` or `net10.0-windows`
- Windows Forms references for some functionality
- Windows Registry access for auto-run features

## Notes

- DDE is a legacy protocol; consider modern alternatives for new applications
- STA thread invocation creates new threads; use sparingly for performance-critical code
- Window station access modifications require appropriate Windows permissions
- Always dispose of DDE clients and servers properly to release resources

## Thread Safety

- `WinApi`: Thread-safe (static methods)
- `WindowsThreadingHelper`: Thread-safe (creates new threads)
- `XlsDdeClient`: Not thread-safe; use one instance per thread or synchronize access
- `XlsDdeServer`: Thread-safe internally; callbacks are dispatched on dedicated threads

## License

Part of the Ecng framework. See the main repository for licensing information.
