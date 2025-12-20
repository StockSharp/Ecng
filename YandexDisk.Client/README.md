# YandexDisk.Client

A .NET client library for the Yandex Disk REST API, providing comprehensive file management, metadata operations, and cloud storage integration.

## Overview

This is an updated fork of the [original YandexDisk.Client](https://github.com/raidenyn/yandexdisk.client/) project, maintained with modern .NET support and updated dependencies.

YandexDisk.Client provides a complete, type-safe wrapper around the Yandex Disk API, supporting:
- File upload and download operations
- Metadata and disk information retrieval
- File and folder management (create, copy, move, delete)
- Trash operations
- Publishing and unpublishing resources
- Custom properties for files and folders
- Asynchronous operations with cancellation support

## Installation

This library is part of the Ecng framework and targets .NET Standard 2.0, .NET 6.0, and .NET 10.0.

## Getting Started

### Authentication

To use the Yandex Disk API, you need an OAuth token. Get one from the [Yandex OAuth page](https://oauth.yandex.com/).

### Creating the API Client

```csharp
using YandexDisk.Client.Http;

// Create the client with your OAuth token
var api = new DiskHttpApi("YOUR_OAUTH_TOKEN");

// The client is thread-safe and should be reused for multiple requests
// Dispose when done
using (api)
{
    // Use the API
}
```

### Custom Logging

```csharp
using YandexDisk.Client.Http;

// Implement ILogSaver for custom logging
public class MyLogger : ILogSaver
{
    public void SaveLog(string message)
    {
        Console.WriteLine($"[YandexDisk] {message}");
    }
}

var api = new DiskHttpApi("YOUR_OAUTH_TOKEN", new MyLogger());
```

## Core Components

The API is divided into three main clients:

### 1. Files Client - Upload/Download Operations

Access via `api.Files`

### 2. MetaInfo Client - Metadata and Information

Access via `api.MetaInfo`

### 3. Commands Client - File/Folder Manipulation

Access via `api.Commands`

## Usage Examples

### File Upload

#### Upload from Stream

```csharp
using System.IO;
using YandexDisk.Client.Clients;

var api = new DiskHttpApi("YOUR_OAUTH_TOKEN");

// Upload a file from a stream
using (var fileStream = File.OpenRead(@"C:\Documents\report.pdf"))
{
    await api.Files.UploadFileAsync(
        path: "/Documents/report.pdf",
        overwrite: true,
        file: fileStream
    );
}

Console.WriteLine("File uploaded successfully!");
```

#### Upload from Local File

```csharp
// Extension method for uploading from local path
await api.Files.UploadFileAsync(
    path: "/Photos/vacation.jpg",
    overwrite: false,
    localFile: @"C:\Photos\vacation.jpg",
    cancellationToken: CancellationToken.None
);
```

#### Manual Upload (Two-Step Process)

```csharp
// Get upload link
var uploadLink = await api.Files.GetUploadLinkAsync(
    path: "/Documents/data.csv",
    overwrite: true
);

// Upload using the link
using (var stream = File.OpenRead(@"C:\Data\data.csv"))
{
    await api.Files.UploadAsync(uploadLink, stream);
}
```

### File Download

#### Download to Stream

```csharp
// Download file to stream
using (var stream = await api.Files.DownloadFileAsync("/Documents/report.pdf"))
{
    // Process the stream
    using (var fileStream = File.Create(@"C:\Downloads\report.pdf"))
    {
        await stream.CopyToAsync(fileStream);
    }
}
```

#### Download to Local File

```csharp
// Extension method for downloading to local path
await api.Files.DownloadFileAsync(
    path: "/Photos/vacation.jpg",
    localFile: @"C:\Downloads\vacation.jpg"
);

Console.WriteLine("File downloaded successfully!");
```

#### Manual Download (Two-Step Process)

```csharp
// Get download link
var downloadLink = await api.Files.GetDownloadLinkAsync("/Documents/report.pdf");

// Download using the link
using (var stream = await api.Files.DownloadAsync(downloadLink))
{
    // Process stream
}
```

### Disk Information

```csharp
using YandexDisk.Client.Protocol;

// Get disk information
Disk diskInfo = await api.MetaInfo.GetDiskInfoAsync();

Console.WriteLine($"Total Space: {diskInfo.TotalSpace / 1024 / 1024} MB");
Console.WriteLine($"Used Space: {diskInfo.UsedSpace / 1024 / 1024} MB");
Console.WriteLine($"Trash Size: {diskInfo.TrashSize / 1024 / 1024} MB");
Console.WriteLine($"Free Space: {(diskInfo.TotalSpace - diskInfo.UsedSpace) / 1024 / 1024} MB");

// System folders
Console.WriteLine($"Downloads folder: {diskInfo.SystemFolders.Downloads}");
Console.WriteLine($"Applications folder: {diskInfo.SystemFolders.Applications}");
```

### Resource Metadata

#### Get File/Folder Information

```csharp
using YandexDisk.Client.Protocol;

// Create request for resource metadata
var request = new ResourceRequest
{
    Path = "/Documents",
    Limit = 100,  // Number of items in folder
    Offset = 0,   // Skip items
    Fields = null // Request all fields
};

// Get metadata
Resource resource = await api.MetaInfo.GetInfoAsync(request);

Console.WriteLine($"Name: {resource.Name}");
Console.WriteLine($"Type: {resource.Type}"); // File or Dir
Console.WriteLine($"Size: {resource.Size} bytes");
Console.WriteLine($"Created: {resource.Created}");
Console.WriteLine($"Modified: {resource.Modified}");
Console.WriteLine($"Path: {resource.Path}");

// For folders, get embedded items
if (resource.Type == ResourceType.Dir && resource.Embedded != null)
{
    foreach (var item in resource.Embedded.Items)
    {
        Console.WriteLine($"  - {item.Name} ({item.Type})");
    }
}
```

#### Get Files List (Flat)

```csharp
// Get all files on disk (flat list, not by folders)
var filesRequest = new FilesResourceRequest
{
    Limit = 50,
    Offset = 0,
    MediaType = null // Or MediaType.Image, MediaType.Video, etc.
};

FilesResourceList files = await api.MetaInfo.GetFilesInfoAsync(filesRequest);

Console.WriteLine($"Total files: {files.Items.Count}");
foreach (var file in files.Items)
{
    Console.WriteLine($"{file.Name}: {file.Size} bytes, {file.MimeType}");
}
```

#### Get Last Uploaded Files

```csharp
var lastUploadedRequest = new LastUploadedResourceRequest
{
    Limit = 20,
    MediaType = null
};

LastUploadedResourceList lastUploaded =
    await api.MetaInfo.GetLastUploadedInfoAsync(lastUploadedRequest);

foreach (var file in lastUploaded.Items)
{
    Console.WriteLine($"{file.Name} uploaded at {file.Modified}");
}
```

### Folder Operations

#### Create Directory

```csharp
// Create a new folder
Link result = await api.Commands.CreateDirectoryAsync("/Projects/NewProject");

Console.WriteLine($"Directory created: {result.HttpStatusCode}");
```

### File Operations

#### Copy File/Folder

```csharp
using YandexDisk.Client.Protocol;

// Copy file or folder
var copyRequest = new CopyFileRequest
{
    From = "/Documents/report.pdf",
    Path = "/Backup/report.pdf",
    Overwrite = false
};

// Simple copy (may return immediately or start async operation)
Link result = await api.Commands.CopyAsync(copyRequest);

// Or copy and wait for completion
await api.Commands.CopyAndWaitAsync(copyRequest);

Console.WriteLine("Copy completed!");
```

#### Move File/Folder

```csharp
// Move file or folder
var moveRequest = new MoveFileRequest
{
    From = "/Documents/old.pdf",
    Path = "/Archive/old.pdf",
    Overwrite = false
};

// Simple move
await api.Commands.MoveAsync(moveRequest);

// Or move and wait for completion
await api.Commands.MoveAndWaitAsync(moveRequest);
```

#### Delete File/Folder

```csharp
// Delete file or folder (moves to trash)
var deleteRequest = new DeleteFileRequest
{
    Path = "/Temp/old_file.txt",
    Permanently = false // false = move to trash, true = permanent delete
};

// Simple delete
await api.Commands.DeleteAsync(deleteRequest);

// Or delete and wait for completion
await api.Commands.DeleteAndWaitAsync(deleteRequest);
```

### Trash Operations

#### Get Trash Information

```csharp
// Get information about resource in trash
var trashRequest = new ResourceRequest
{
    Path = "/old_file.txt" // Path relative to trash root
};

Resource trashedFile = await api.MetaInfo.GetTrashInfoAsync(trashRequest);
Console.WriteLine($"File in trash: {trashedFile.Name}");
```

#### Restore from Trash

```csharp
var restoreRequest = new RestoreFromTrashRequest
{
    Path = "/old_file.txt", // Path in trash
    Name = "restored_file.txt", // Optional: new name
    Overwrite = false
};

// Restore and wait for completion
await api.Commands.RestoreFromTrashAndWaitAsync(restoreRequest);

Console.WriteLine("File restored from trash!");
```

#### Empty Trash

```csharp
// Delete specific path from trash permanently
await api.Commands.EmptyTrashAndWaitAsync("/old_folder");

// Or delete everything from trash
await api.Commands.EmptyTrashAndWaitAsync("/");
```

### Publishing

#### Publish Resource

```csharp
// Make a file or folder publicly accessible
Link publicLink = await api.MetaInfo.PublishFolderAsync("/Photos/Vacation");

Console.WriteLine($"Public URL: {publicLink.Href}");
```

#### Unpublish Resource

```csharp
// Remove public access
await api.MetaInfo.UnpublishFolderAsync("/Photos/Vacation");

Console.WriteLine("Resource is now private");
```

### Custom Properties

```csharp
// Add custom metadata to a resource
var properties = new Dictionary<string, string>
{
    ["author"] = "John Doe",
    ["project"] = "Project X",
    ["version"] = "1.0"
};

Resource resource = await api.MetaInfo.AppendCustomProperties(
    path: "/Documents/report.pdf",
    properties: properties
);

// Read custom properties
foreach (var prop in resource.CustomProperties)
{
    Console.WriteLine($"{prop.Key}: {prop.Value}");
}
```

### Asynchronous Operations

Some operations (copy, move, delete) may be asynchronous. Check the result:

```csharp
using System.Net;

// Start a copy operation
var copyRequest = new CopyFileRequest
{
    From = "/LargeFolder",
    Path = "/Backup/LargeFolder"
};

Link operationLink = await api.Commands.CopyAsync(copyRequest);

// Check if operation is asynchronous
if (operationLink.HttpStatusCode == HttpStatusCode.Accepted)
{
    // Operation is running in background
    // Poll for status
    Operation operation;
    do
    {
        await Task.Delay(3000); // Wait 3 seconds
        operation = await api.Commands.GetOperationStatus(operationLink);
        Console.WriteLine($"Status: {operation.Status}");
    }
    while (operation.Status == OperationStatus.InProgress);

    if (operation.Status == OperationStatus.Success)
    {
        Console.WriteLine("Operation completed successfully!");
    }
    else
    {
        Console.WriteLine($"Operation failed: {operation.Status}");
    }
}
else
{
    // Operation completed immediately
    Console.WriteLine("Copy completed!");
}

// Or use extension methods that handle waiting automatically
await api.Commands.CopyAndWaitAsync(copyRequest);
```

## Complete Application Example

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using YandexDisk.Client.Http;
using YandexDisk.Client.Protocol;

public class YandexDiskManager
{
    private readonly DiskHttpApi _api;

    public YandexDiskManager(string oauthToken)
    {
        _api = new DiskHttpApi(oauthToken);
    }

    public async Task BackupFiles(string localDirectory, string remotePath)
    {
        try
        {
            // Create remote directory if needed
            await _api.Commands.CreateDirectoryAsync(remotePath);

            // Upload all files from local directory
            foreach (var file in Directory.GetFiles(localDirectory))
            {
                var fileName = Path.GetFileName(file);
                var remoteFPath = $"{remotePath}/{fileName}";

                Console.WriteLine($"Uploading {fileName}...");
                await _api.Files.UploadFileAsync(
                    path: remoteFPath,
                    overwrite: true,
                    localFile: file,
                    cancellationToken: default
                );
            }

            Console.WriteLine("Backup completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Backup failed: {ex.Message}");
        }
    }

    public async Task ShowDiskInfo()
    {
        var disk = await _api.MetaInfo.GetDiskInfoAsync();

        var totalGB = disk.TotalSpace / 1024.0 / 1024.0 / 1024.0;
        var usedGB = disk.UsedSpace / 1024.0 / 1024.0 / 1024.0;
        var freeGB = (disk.TotalSpace - disk.UsedSpace) / 1024.0 / 1024.0 / 1024.0;

        Console.WriteLine($"Disk Space:");
        Console.WriteLine($"  Total: {totalGB:F2} GB");
        Console.WriteLine($"  Used: {usedGB:F2} GB ({usedGB / totalGB * 100:F1}%)");
        Console.WriteLine($"  Free: {freeGB:F2} GB");
    }

    public async Task ListFiles(string path = "/")
    {
        var request = new ResourceRequest { Path = path };
        var resource = await _api.MetaInfo.GetInfoAsync(request);

        if (resource.Type == ResourceType.Dir && resource.Embedded != null)
        {
            Console.WriteLine($"Contents of {path}:");
            foreach (var item in resource.Embedded.Items)
            {
                var type = item.Type == ResourceType.Dir ? "DIR " : "FILE";
                var size = item.Size > 0 ? $"{item.Size / 1024} KB" : "";
                Console.WriteLine($"  [{type}] {item.Name} {size}");
            }
        }
    }

    public void Dispose()
    {
        _api?.Dispose();
    }
}

// Usage
class Program
{
    static async Task Main(string[] args)
    {
        var manager = new YandexDiskManager("YOUR_OAUTH_TOKEN");

        try
        {
            await manager.ShowDiskInfo();
            await manager.ListFiles("/");
            await manager.BackupFiles(@"C:\Documents", "/Backup/Documents");
        }
        finally
        {
            manager.Dispose();
        }
    }
}
```

## Error Handling

```csharp
using YandexDisk.Client;

try
{
    await api.Files.UploadFileAsync("/test.txt", true, stream);
}
catch (YandexApiException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    // Handle specific error codes
    if (ex.ErrorDescription != null)
    {
        Console.WriteLine($"Error: {ex.ErrorDescription.Message}");
        Console.WriteLine($"Description: {ex.ErrorDescription.Description}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## API Reference

### IDiskApi Interface

Main entry point with three client properties:

- **Files**: `IFilesClient` - File upload/download operations
- **MetaInfo**: `IMetaInfoClient` - Metadata and disk information
- **Commands**: `ICommandsClient` - File/folder manipulation

### IFilesClient Methods

- `GetUploadLinkAsync(path, overwrite)` - Get upload URL
- `UploadAsync(link, stream)` - Upload file content
- `UploadFileAsync(path, overwrite, stream)` - Upload in one call
- `GetDownloadLinkAsync(path)` - Get download URL
- `DownloadAsync(link)` - Download file content
- `DownloadFileAsync(path)` - Download in one call

### IMetaInfoClient Methods

- `GetDiskInfoAsync()` - Get disk space information
- `GetInfoAsync(request)` - Get file/folder metadata
- `GetTrashInfoAsync(request)` - Get trash metadata
- `GetFilesInfoAsync(request)` - Get flat file list
- `GetLastUploadedInfoAsync(request)` - Get recently uploaded files
- `AppendCustomProperties(path, properties)` - Add custom metadata
- `PublishFolderAsync(path)` - Make resource public
- `UnpublishFolderAsync(path)` - Make resource private

### ICommandsClient Methods

- `CreateDirectoryAsync(path)` - Create folder
- `CopyAsync(request)` - Copy file/folder
- `CopyAndWaitAsync(request)` - Copy and wait for completion
- `MoveAsync(request)` - Move file/folder
- `MoveAndWaitAsync(request)` - Move and wait for completion
- `DeleteAsync(request)` - Delete file/folder
- `DeleteAndWaitAsync(request)` - Delete and wait for completion
- `EmptyTrashAsync(path)` - Delete from trash permanently
- `EmptyTrashAndWaitAsync(path)` - Empty trash and wait
- `RestoreFromTrashAsync(request)` - Restore from trash
- `RestoreFromTrashAndWaitAsync(request)` - Restore and wait
- `GetOperationStatus(link)` - Check async operation status

## Thread Safety

- `DiskHttpApi` is thread-safe and can be used from multiple threads
- Reuse the same instance for all requests (it's designed for this)
- Each client (`Files`, `MetaInfo`, `Commands`) is thread-safe

## Best Practices

1. **Reuse the API client**: Create one instance and use it for all requests
2. **Dispose properly**: Always dispose the API client when done
3. **Use async/await**: All operations are asynchronous
4. **Handle cancellation**: Pass `CancellationToken` for long operations
5. **Check operation status**: Some operations are asynchronous and require polling
6. **Use extension methods**: `*AndWaitAsync` methods handle async operations automatically

## Requirements

- .NET Standard 2.0, .NET 6.0, or .NET 10.0
- Valid Yandex OAuth token
- Internet connection
- Dependencies:
  - Newtonsoft.Json
  - JetBrains.Annotations

## Differences from Original Library

This fork includes:
- Updated dependencies for modern .NET
- Compatibility with .NET 6.0 and .NET 10.0
- Updated to latest Yandex Disk API specifications
- Maintained within the Ecng framework

## Resources

- [Yandex Disk API Documentation](https://yandex.com/dev/disk/api/concepts/)
- [Get OAuth Token](https://oauth.yandex.com/)
- [Original Repository](https://github.com/raidenyn/yandexdisk.client/)

## License

Part of the Ecng framework. See the main repository for licensing information.

Original library by [raidenyn](https://github.com/raidenyn).
