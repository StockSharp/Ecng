# Ecng.Backup

Core abstractions for cloud backup services. This library defines the unified interface `IBackupService` that all backup providers implement.

## Overview

The library provides a provider-agnostic API for working with cloud storage services. You can upload, download, delete, and publish files using the same code regardless of whether you're using AWS S3, Azure Blob Storage, Yandex.Disk, or Mega.

## Key Types

### BackupEntry

Represents a file or folder in cloud storage.

```csharp
var folder = new BackupEntry { Name = "my-folder" };
var file = new BackupEntry
{
    Name = "data.zip",
    Parent = folder  // Creates path: my-folder/data.zip
};

// Get full path including all parents
string path = file.GetFullPath(); // "my-folder/data.zip"

// Access metadata (populated after FillInfoAsync or FindAsync)
long size = file.Size;
DateTime modified = file.LastModified;
```

### IBackupService

The main interface for interacting with cloud storage.

```csharp
public interface IBackupService : IDisposable
{
    // Feature detection
    bool CanPublish { get; }        // Supports public URLs
    bool CanExpirable { get; }      // Supports expiring public URLs
    bool CanFolders { get; }        // Supports folder creation
    bool CanPartialDownload { get; } // Supports range downloads

    // Operations
    Task CreateFolder(BackupEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<BackupEntry> FindAsync(BackupEntry parent, string criteria);
    Task FillInfoAsync(BackupEntry entry, CancellationToken ct = default);
    Task DeleteAsync(BackupEntry entry, CancellationToken ct = default);
    Task DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length,
                       Action<int> progress, CancellationToken ct = default);
    Task UploadAsync(BackupEntry entry, Stream stream,
                     Action<int> progress, CancellationToken ct = default);
    Task<string> PublishAsync(BackupEntry entry, TimeSpan? expiresIn = null,
                              CancellationToken ct = default);
    Task UnPublishAsync(BackupEntry entry, CancellationToken ct = default);
}
```

## Usage Examples

### Uploading a File

```csharp
// Works with any IBackupService implementation
async Task UploadFileAsync(IBackupService service, string localPath, string remotePath)
{
    var entry = new BackupEntry { Name = remotePath };

    await using var stream = File.OpenRead(localPath);
    await service.UploadAsync(entry, stream, progress =>
    {
        Console.WriteLine($"Upload progress: {progress}%");
    });
}
```

### Downloading a File

```csharp
async Task DownloadFileAsync(IBackupService service, string remotePath, string localPath)
{
    var entry = new BackupEntry { Name = remotePath };

    await using var stream = File.Create(localPath);
    await service.DownloadAsync(entry, stream, null, null, progress =>
    {
        Console.WriteLine($"Download progress: {progress}%");
    });
}
```

### Partial Download (Resume Support)

```csharp
async Task ResumeDownloadAsync(IBackupService service, BackupEntry entry, string localPath)
{
    if (!service.CanPartialDownload)
        throw new NotSupportedException("Service doesn't support partial downloads");

    var fileInfo = new FileInfo(localPath);
    long existingBytes = fileInfo.Exists ? fileInfo.Length : 0;

    // Get total file size
    await service.FillInfoAsync(entry);
    long remaining = entry.Size - existingBytes;

    if (remaining <= 0)
        return; // Already complete

    await using var stream = new FileStream(localPath, FileMode.Append);
    await service.DownloadAsync(entry, stream, existingBytes, remaining,
        progress => Console.WriteLine($"Resume progress: {progress}%"));
}
```

### Listing Files

```csharp
async Task ListFilesAsync(IBackupService service, string folderPath)
{
    var folder = new BackupEntry { Name = folderPath };

    await foreach (var entry in service.FindAsync(folder, criteria: "*.zip"))
    {
        Console.WriteLine($"{entry.GetFullPath()} - {entry.Size} bytes");
    }
}
```

### Publishing a File

```csharp
async Task<string> ShareFileAsync(IBackupService service, BackupEntry entry)
{
    if (!service.CanPublish)
        throw new NotSupportedException("Service doesn't support publishing");

    // Permanent public URL (if supported)
    string permanentUrl = await service.PublishAsync(entry);

    // Or expiring URL (7 days max for S3)
    if (service.CanExpirable)
    {
        string tempUrl = await service.PublishAsync(entry, TimeSpan.FromHours(24));
    }

    return permanentUrl;
}
```

### Working with Folders

```csharp
async Task OrganizeFilesAsync(IBackupService service)
{
    if (!service.CanFolders)
    {
        // S3-like services use virtual folders via path prefixes
        var file = new BackupEntry { Name = "backups/2024/january/data.zip" };
        // The "folders" are created implicitly
        return;
    }

    // Services like Yandex.Disk require explicit folder creation
    var folder = new BackupEntry { Name = "backups" };
    await service.CreateFolder(folder);

    var subfolder = new BackupEntry { Name = "2024", Parent = folder };
    await service.CreateFolder(subfolder);
}
```

## Available Implementations

| Package | Service | CanPublish | CanFolders | CanPartialDownload |
|---------|---------|------------|------------|-------------------|
| Ecng.Backup.AWS | Amazon S3 | Yes | No | Yes |
| Ecng.Backup.AWS | Amazon Glacier | No | No | No |
| Ecng.Backup.Azure | Azure Blob Storage | Yes | No | Yes |
| Ecng.Backup.Yandex | Yandex.Disk | Yes | Yes | Yes |
| Ecng.Backup.Mega | Mega.nz | Yes | Yes | No |

## NuGet

```
Install-Package Ecng.Backup
```
