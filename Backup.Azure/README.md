# Ecng.Backup.Azure

Azure Blob Storage backup provider implementing `IBackupService`.

## Overview

`AzureBlobService` provides cloud backup capabilities using Microsoft Azure Blob Storage. It uses block blobs with chunked uploads for reliable large file transfers.

## Usage

### Creating the Service

```csharp
using Ecng.Backup;
using Ecng.Backup.Azure;

var azure = new AzureBlobService(
    connectionString: "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=...;EndpointSuffix=core.windows.net",
    container: "my-backup-container"
);

// Container is created automatically if it doesn't exist
```

### Features

| Feature | Supported |
|---------|-----------|
| CanPublish | No |
| CanExpirable | No |
| CanFolders | No (virtual via path prefixes) |
| CanPartialDownload | Yes |

### Uploading Files

```csharp
var entry = new BackupEntry { Name = "backups/database/2024-01-15.bak" };

await using var stream = File.OpenRead(@"C:\backups\database.bak");
await azure.UploadAsync(entry, stream, progress =>
{
    Console.WriteLine($"Upload: {progress}%");
});
// Uses 4MB block chunks for reliable uploads
```

### Downloading Files

```csharp
var entry = new BackupEntry { Name = "backups/database/2024-01-15.bak" };

await using var stream = File.Create(@"C:\restore\database.bak");
await azure.DownloadAsync(entry, stream, null, null, progress =>
{
    Console.WriteLine($"Download: {progress}%");
});
```

### Partial Download (Resume Support)

```csharp
var entry = new BackupEntry { Name = "large-file.zip" };

// Get total size
await azure.FillInfoAsync(entry);

// Download specific range
long offset = 1024 * 1024 * 100; // Start at 100MB
long length = 1024 * 1024 * 50;  // Download 50MB

await using var stream = new MemoryStream();
await azure.DownloadAsync(entry, stream, offset, length, p => { });
```

### Listing Blobs

```csharp
// List all blobs with prefix
var folder = new BackupEntry { Name = "backups/2024" };

await foreach (var blob in azure.FindAsync(folder, criteria: null))
{
    Console.WriteLine($"{blob.GetFullPath()}: {blob.Size} bytes");
    Console.WriteLine($"  Last modified: {blob.LastModified}");
}

// Search with filter
await foreach (var blob in azure.FindAsync(null, criteria: ".bak"))
{
    Console.WriteLine(blob.Name);
}
```

### Deleting Blobs

```csharp
var entry = new BackupEntry { Name = "backups/old-backup.zip" };
await azure.DeleteAsync(entry);
```

### Getting Blob Info

```csharp
var entry = new BackupEntry { Name = "backups/data.zip" };
await azure.FillInfoAsync(entry);

Console.WriteLine($"Size: {entry.Size}");
Console.WriteLine($"Modified: {entry.LastModified}");
```

## Connection String

Get your connection string from Azure Portal:
1. Go to your Storage Account
2. Select "Access keys" under Security + networking
3. Copy the "Connection string"

Example format:
```
DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=abc123...==;EndpointSuffix=core.windows.net
```

## NuGet

```
Install-Package Ecng.Backup.Azure
```

## Dependencies

- Azure.Storage.Blobs
- Ecng.Backup
