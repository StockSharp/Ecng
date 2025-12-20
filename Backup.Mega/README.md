# Ecng.Backup.Mega

Mega.nz backup provider implementing `IBackupService`.

## Overview

`MegaService` provides cloud backup capabilities using Mega.nz encrypted cloud storage. All data is end-to-end encrypted automatically.

## Usage

### Creating the Service

```csharp
using System.Security;
using Ecng.Backup;
using Ecng.Backup.Mega;

// Create secure password
var password = new SecureString();
foreach (char c in "your-password")
    password.AppendChar(c);
password.MakeReadOnly();

var mega = new MegaService(
    email: "your-email@example.com",
    password: password
);

// Login happens automatically on first operation
```

### Features

| Feature | Supported |
|---------|-----------|
| CanPublish | Yes (permanent links only) |
| CanExpirable | No |
| CanFolders | Yes |
| CanPartialDownload | No |

### Creating Folders

Unlike S3-like services, Mega requires explicit folder creation:

```csharp
// Create nested folder structure
var folder = new BackupEntry { Name = "backups" };
var subfolder = new BackupEntry { Name = "2024", Parent = folder };
var deepFolder = new BackupEntry { Name = "january", Parent = subfolder };

// Creates: /backups/2024/january
await mega.CreateFolder(deepFolder);
// Parent folders are created automatically if they don't exist
```

### Uploading Files

```csharp
// First ensure parent folder exists
var folder = new BackupEntry { Name = "backups/2024" };
await mega.CreateFolder(folder);

// Upload file to folder
var entry = new BackupEntry
{
    Name = "data.zip",
    Parent = folder
};

await using var stream = File.OpenRead(@"C:\data.zip");
await mega.UploadAsync(entry, stream, progress =>
{
    Console.WriteLine($"Upload: {progress}%");
});
```

### Downloading Files

```csharp
var folder = new BackupEntry { Name = "backups/2024" };
var entry = new BackupEntry { Name = "data.zip", Parent = folder };

await using var stream = File.Create(@"C:\downloaded-data.zip");
await mega.DownloadAsync(entry, stream, null, null, progress =>
{
    Console.WriteLine($"Download: {progress}%");
});
// Note: Partial downloads are not supported
```

### Listing Files

```csharp
// List files in folder
var folder = new BackupEntry { Name = "backups" };

await foreach (var item in mega.FindAsync(folder, criteria: null))
{
    Console.WriteLine($"{item.Name}: {item.Size} bytes");
}

// List root folder
await foreach (var item in mega.FindAsync(null, criteria: null))
{
    Console.WriteLine(item.Name);
}

// Filter by name
await foreach (var item in mega.FindAsync(folder, criteria: "zip"))
{
    Console.WriteLine(item.Name); // Only items containing "zip"
}
```

### Publishing Files

```csharp
var entry = new BackupEntry { Name = "shared-file.pdf", Parent = folder };

// Get public share link
string publicUrl = await mega.PublishAsync(entry);
Console.WriteLine($"Share link: {publicUrl}");
// Example: https://mega.nz/file/abc123#key

// Remove public access
await mega.UnPublishAsync(entry);
```

### Deleting Files

```csharp
var entry = new BackupEntry { Name = "old-backup.zip", Parent = folder };
await mega.DeleteAsync(entry);
```

### Getting File Info

```csharp
var entry = new BackupEntry { Name = "data.zip", Parent = folder };
await mega.FillInfoAsync(entry);

Console.WriteLine($"Size: {entry.Size}");
Console.WriteLine($"Modified: {entry.LastModified}");
```

## Important Notes

1. **Login is automatic**: The first operation triggers login
2. **End-to-end encryption**: All files are encrypted client-side
3. **No partial downloads**: You must download entire files
4. **No expiring links**: Share links are permanent until unpublished
5. **Folder structure**: Explicit folder creation is required

## NuGet

```
Install-Package Ecng.Backup.Mega
```

## Dependencies

- Ecng.Backup
- Ecng.Collections
- Nito.AsyncEx
