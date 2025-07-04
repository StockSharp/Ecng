# Ecng.Backup.Mega

Backup provider for MEGA cloud.

## Purpose

Backup provider for MEGA cloud.

## Key Features

- No manual protocol handling
- Built-in session management
- Async file transfers

## Usage Example

```csharp
var service = new MegaService(login, password);
await service.UploadAsync(entry, stream);
```
