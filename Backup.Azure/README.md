# Ecng.Backup.Azure

Azure Blob Storage backup provider.

## Purpose

Azure Blob Storage backup provider.

## Key Features

- Works with blobs using one interface
- Automatic container creation
- Async uploads and downloads

## Usage Example

```csharp
var service = new AzureBackupService(connectionString, container);
await service.UploadAsync(entry, stream);
```
