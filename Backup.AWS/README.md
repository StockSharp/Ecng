# Ecng.Backup.AWS

Amazon Web Services backup providers implementing `IBackupService`. Supports both Amazon S3 for frequent access and Amazon Glacier for archival storage.

## Services

### AmazonS3Service

High-performance object storage for frequently accessed data.

```csharp
using Ecng.Backup;
using Ecng.Backup.Amazon;

// Create service using region name
var s3 = new AmazonS3Service(
    endpoint: "us-east-1",      // or "eu-west-1", "ap-northeast-1", etc.
    bucket: "my-backup-bucket",
    accessKey: "AKIAIOSFODNN7EXAMPLE",
    secretKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
);

// Or using RegionEndpoint directly
var s3 = new AmazonS3Service(
    endpoint: RegionEndpoint.USEast1,
    bucket: "my-backup-bucket",
    accessKey: "...",
    secretKey: "..."
);
```

#### Features

| Feature | Supported |
|---------|-----------|
| CanPublish | Yes |
| CanExpirable | Yes (up to 7 days) |
| CanFolders | No (virtual via path prefixes) |
| CanPartialDownload | Yes |

#### Upload with Progress

```csharp
var entry = new BackupEntry { Name = "backups/2024/data.zip" };

await using var fileStream = File.OpenRead(@"C:\data.zip");
await s3.UploadAsync(entry, fileStream, progress =>
{
    Console.WriteLine($"Uploaded: {progress}%");
});
```

#### Download with Resume Support

```csharp
var entry = new BackupEntry { Name = "backups/2024/data.zip" };

// Get file info first
await s3.FillInfoAsync(entry);
Console.WriteLine($"File size: {entry.Size} bytes");

// Resume download from offset
long alreadyDownloaded = new FileInfo("data.zip.partial").Length;
long remaining = entry.Size - alreadyDownloaded;

await using var stream = new FileStream("data.zip.partial", FileMode.Append);
await s3.DownloadAsync(entry, stream, alreadyDownloaded, remaining,
    progress => Console.WriteLine($"Progress: {progress}%"));
```

#### Publishing Files

```csharp
var entry = new BackupEntry { Name = "reports/monthly.pdf" };

// Permanent public URL (requires bucket ACL permissions)
string permanentUrl = await s3.PublishAsync(entry);

// Pre-signed URL with expiration (works with any bucket)
string tempUrl = await s3.PublishAsync(entry, TimeSpan.FromHours(24));
// Maximum expiration: 7 days

// Revoke public access
await s3.UnPublishAsync(entry);
```

#### Listing Files

```csharp
// List all files in a "folder"
var folder = new BackupEntry { Name = "backups/2024" };

await foreach (var file in s3.FindAsync(folder, criteria: null))
{
    Console.WriteLine($"{file.Name}: {file.Size} bytes, modified {file.LastModified}");
}

// Search with criteria
await foreach (var file in s3.FindAsync(null, criteria: "*.zip"))
{
    Console.WriteLine(file.GetFullPath());
}
```

### AmazonGlacierService

Low-cost archival storage for infrequently accessed data. Note: Glacier operations are asynchronous and may take hours to complete.

```csharp
var glacier = new AmazonGlacierService(
    endpoint: "us-east-1",
    bucket: "my-archive-vault",
    accessKey: "...",
    secretKey: "..."
);

// Configure job timeout (default: 6 hours)
glacier.JobTimeOut = TimeSpan.FromHours(12);

// Configure poll interval (default: 1 minute)
glacier.PollInterval = TimeSpan.FromMinutes(5);
```

#### Features

| Feature | Supported |
|---------|-----------|
| CanPublish | No |
| CanExpirable | No |
| CanFolders | No |
| CanPartialDownload | Yes |

#### Upload to Glacier

```csharp
var entry = new BackupEntry { Name = "archive/old-data-2020.tar.gz" };

await using var stream = File.OpenRead("old-data-2020.tar.gz");
await glacier.UploadAsync(entry, stream, p => Console.WriteLine($"{p}%"));
// Upload completes immediately
```

#### Download from Glacier

```csharp
var entry = new BackupEntry { Name = "archive/old-data-2020.tar.gz" };

// This initiates a retrieval job and waits for completion
// Can take 3-5 hours for standard retrieval!
await using var stream = File.Create("restored-data.tar.gz");
await glacier.DownloadAsync(entry, stream, null, null,
    p => Console.WriteLine($"Retrieval: {p}%"));
```

## Helper Extensions

```csharp
using Ecng.Backup.Amazon;

// Get RegionEndpoint by name (flexible matching)
RegionEndpoint region = AmazonExtensions.GetEndpoint("us-east-1");
// Also accepts: "useast1", "US East (N. Virginia)", etc.
```

## NuGet

```
Install-Package Ecng.Backup.AWS
```

## Dependencies

- AWSSDK.S3
- AWSSDK.Glacier
- Ecng.Backup
