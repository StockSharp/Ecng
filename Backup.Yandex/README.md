# Ecng.Backup.Yandex

Yandex Disk backup provider.

## Purpose

Yandex Disk backup provider.

## Key Features

- Ready to use client for Yandex API
- OAuth token support
- Async operations

## Usage Example

```csharp
var service = new YandexDiskService(token);
await service.UploadAsync(entry, stream);
```
