# Ecng.Backup.AWS

Amazon S3 and Glacier backup providers.

## Purpose

Amazon S3 and Glacier backup providers.

## Advantages

- Handles AWS authentication
- Simple methods for uploads
- Glacier archival storage support

## Usage Example

```csharp
var service = new AmazonS3Service(credentials, bucket);
await service.UploadAsync(entry, stream);
```
