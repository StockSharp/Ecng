# Ecng.Backup

Abstractions for implementing backup services and strategies.

## Purpose

Abstractions for implementing backup services and strategies.

## Key Features

- Unified API for different storage backends
- Async helpers to upload and download data
- Partial download and upload support

## Usage Example

```csharp
await backupService.UploadAsync(entry, stream);
```
