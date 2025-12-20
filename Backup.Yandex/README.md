# Ecng.Backup.Yandex

Yandex.Disk backup provider implementing `IBackupService`.

## Overview

`YandexDiskService` provides cloud backup capabilities using Yandex.Disk cloud storage service.

## Getting OAuth Token

1. Go to https://oauth.yandex.com/
2. Create an application with "Yandex.Disk REST API" permissions
3. Get the OAuth token for your application

## Usage

### Creating the Service

```csharp
using System.Security;
using Ecng.Backup;
using Ecng.Backup.Yandex;

// Create secure token
var token = new SecureString();
foreach (char c in "your-oauth-token")
    token.AppendChar(c);
token.MakeReadOnly();

var yandex = new YandexDiskService(token);
```

### Features

| Feature | Supported |
|---------|-----------|
| CanPublish | Yes (permanent links only) |
| CanExpirable | No |
| CanFolders | Yes |
| CanPartialDownload | No |

### Creating Folders

```csharp
// Create nested folder structure
var folder = new BackupEntry { Name = "backups" };
var subfolder = new BackupEntry { Name = "2024", Parent = folder };

// Creates /backups/2024
await yandex.CreateFolder(subfolder);
// Parent folders are created automatically if they don't exist
```

### Uploading Files

```csharp
var entry = new BackupEntry { Name = "backups/2024/data.zip" };

await using var stream = File.OpenRead(@"C:\data.zip");
await yandex.UploadAsync(entry, stream, progress =>
{
    // Note: Progress may not be reported for all uploads
    Console.WriteLine($"Upload: {progress}%");
});
```

### Downloading Files

```csharp
var entry = new BackupEntry { Name = "backups/2024/data.zip" };

await using var stream = File.Create(@"C:\downloaded-data.zip");
await yandex.DownloadAsync(entry, stream, null, null, progress =>
{
    Console.WriteLine($"Download: {progress}%");
});
// Note: Partial downloads are not supported
```

### Listing Files

```csharp
// List files in folder (with pagination handled automatically)
var folder = new BackupEntry { Name = "backups/2024" };

await foreach (var item in yandex.FindAsync(folder, criteria: null))
{
    Console.WriteLine($"{item.Name}: {item.Size} bytes");
    Console.WriteLine($"  Modified: {item.LastModified}");
}

// List root folder
await foreach (var item in yandex.FindAsync(null, criteria: null))
{
    Console.WriteLine(item.Name);
}

// Filter by name
await foreach (var item in yandex.FindAsync(folder, criteria: ".zip"))
{
    Console.WriteLine(item.Name); // Only items containing ".zip"
}
```

### Publishing Files

```csharp
var entry = new BackupEntry { Name = "shared/document.pdf" };

// Get public share link
string publicUrl = await yandex.PublishAsync(entry);
Console.WriteLine($"Share link: {publicUrl}");

// Remove public access
await yandex.UnPublishAsync(entry);
```

### Deleting Files

```csharp
var entry = new BackupEntry { Name = "backups/old-backup.zip" };
await yandex.DeleteAsync(entry);
```

### Getting File Info

```csharp
var entry = new BackupEntry { Name = "backups/data.zip" };
await yandex.FillInfoAsync(entry);

Console.WriteLine($"Size: {entry.Size}");
Console.WriteLine($"Modified: {entry.LastModified}");
```

## Path Format

Yandex.Disk uses forward slashes for paths:
- Root: `/`
- Folder: `backups/2024`
- File: `backups/2024/data.zip`

## Error Handling

```csharp
try
{
    await yandex.FillInfoAsync(entry);
}
catch (YandexApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    Console.WriteLine("File not found");
}
```

## Important Notes

1. **OAuth token required**: Get it from Yandex OAuth
2. **No partial downloads**: You must download entire files
3. **No expiring links**: Share links are permanent until unpublished
4. **Folder structure**: Explicit folder creation is required (unlike S3)
5. **Pagination**: Handled automatically when listing files

## NuGet

```
Install-Package Ecng.Backup.Yandex
```

## Dependencies

- Ecng.Backup
- YandexDisk.Client
