# Ecng.Licensing

A lightweight licensing library for .NET applications supporting license validation, expiration management, and multi-platform licensing.

## Purpose

Ecng.Licensing provides a simple and flexible licensing system for commercial .NET applications. It handles license parsing, validation, feature management, and expiration policies across multiple platforms (Windows, Linux, macOS).

## Key Features

- XML-based license format with digital signatures
- Multi-platform license support (Windows, Linux, macOS)
- Feature-based licensing with granular control
- Hardware ID binding for license protection
- License expiration management with configurable actions
- Account-based licensing
- Support for both file-based and byte-array license loading

## Installation

Add a reference to the `Ecng.Licensing` package in your project.

## Core Concepts

### License Structure

A license contains:
- **License ID**: Unique identifier for the license
- **Issued To**: Email or name of the licensee
- **Issued Date**: When the license was created
- **Features**: Platform-specific features with expiration dates
- **Signature**: Digital signature for validation

### License Features

Each feature has:
- **Name**: Feature identifier (e.g., "Trading", "Analytics")
- **Expiration Date**: When the feature expires
- **Expire Action**: What happens when the feature expires (PreventWork or PreventUpgrade)
- **Hardware ID**: Optional hardware binding
- **Account**: Optional account identifier
- **OneApp ID**: Optional single-application binding

## Usage Examples

### Loading a License from File

```csharp
using Ecng.Licensing;
using System.IO;

// Load license from file
byte[] licenseData = File.ReadAllBytes("license.lic");
var license = new License("license.lic", licenseData);

Console.WriteLine($"License ID: {license.Id}");
Console.WriteLine($"Issued To: {license.IssuedTo}");
Console.WriteLine($"Issued Date: {license.IssuedDate}");
```

### Loading a License from Byte Array

```csharp
using Ecng.Licensing;

// Load license from embedded resource or byte array
byte[] licenseBytes = GetLicenseFromResource();
var license = new License(licenseBytes);
```

### Accessing License Features

```csharp
using Ecng.Licensing;
using System.Runtime.InteropServices;

// Get features for the current platform
var currentPlatform = OSPlatform.Windows; // or OSPlatform.Linux, OSPlatform.OSX

if (license.Features.TryGetValue(currentPlatform, out var features))
{
    foreach (var feature in features)
    {
        Console.WriteLine($"Feature: {feature.Name}");
        Console.WriteLine($"Expires: {feature.ExpirationDate}");
        Console.WriteLine($"Action on expire: {feature.ExpireAction}");

        if (!string.IsNullOrEmpty(feature.HardwareId))
            Console.WriteLine($"Hardware ID: {feature.HardwareId}");

        if (!string.IsNullOrEmpty(feature.Account))
            Console.WriteLine($"Account: {feature.Account}");
    }
}
```

### Checking Feature Availability

```csharp
using Ecng.Licensing;
using System.Linq;
using System.Runtime.InteropServices;

public bool IsFeatureAvailable(License license, string featureName)
{
    var currentPlatform = OSPlatform.Windows;

    if (!license.Features.TryGetValue(currentPlatform, out var features))
        return false;

    var feature = features.FirstOrDefault(f => f.Name == featureName);

    if (feature == null)
        return false;

    // Check if feature has expired
    if (DateTime.UtcNow > feature.ExpirationDate)
    {
        if (feature.ExpireAction == LicenseExpireActions.PreventWork)
            return false;
    }

    return true;
}

// Usage
if (IsFeatureAvailable(license, "Trading"))
{
    Console.WriteLine("Trading feature is available");
}
```

### Validating Hardware ID

```csharp
using Ecng.Licensing;
using System.Linq;
using System.Runtime.InteropServices;

public bool ValidateHardwareId(License license, string currentHardwareId)
{
    var currentPlatform = OSPlatform.Windows;

    if (!license.Features.TryGetValue(currentPlatform, out var features))
        return false;

    // If any feature has a hardware ID, validate it
    var featuresWithHwId = features.Where(f => !string.IsNullOrEmpty(f.HardwareId));

    if (!featuresWithHwId.Any())
        return true; // No hardware binding

    return featuresWithHwId.Any(f => f.HardwareId.Equals(currentHardwareId,
        StringComparison.OrdinalIgnoreCase));
}
```

### Checking License Expiration

```csharp
using Ecng.Licensing;
using System.Linq;

public class LicenseStatus
{
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public LicenseExpireActions? ExpireAction { get; set; }
}

public LicenseStatus GetLicenseStatus(License license, string featureName)
{
    var currentPlatform = System.Runtime.InteropServices.OSPlatform.Windows;
    var status = new LicenseStatus { IsValid = false };

    if (!license.Features.TryGetValue(currentPlatform, out var features))
        return status;

    var feature = features.FirstOrDefault(f => f.Name == featureName);
    if (feature == null)
        return status;

    status.ExpirationDate = feature.ExpirationDate;
    status.ExpireAction = feature.ExpireAction;
    status.IsExpired = DateTime.UtcNow > feature.ExpirationDate;
    status.IsValid = !status.IsExpired ||
                     feature.ExpireAction == LicenseExpireActions.PreventUpgrade;

    return status;
}

// Usage
var status = GetLicenseStatus(license, "Analytics");
if (status.IsExpired)
{
    Console.WriteLine($"License expired on {status.ExpirationDate}");
    Console.WriteLine($"Action: {status.ExpireAction}");
}
```

### Working with Multiple Features

```csharp
using Ecng.Licensing;
using System.Linq;
using System.Runtime.InteropServices;

public class LicenseManager
{
    private readonly License _license;
    private readonly OSPlatform _platform;

    public LicenseManager(License license)
    {
        _license = license;
        _platform = GetCurrentPlatform();
    }

    private OSPlatform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;

        throw new PlatformNotSupportedException();
    }

    public bool HasFeature(string featureName)
    {
        if (!_license.Features.TryGetValue(_platform, out var features))
            return false;

        return features.Any(f => f.Name.Equals(featureName,
            StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> GetActiveFeatures()
    {
        if (!_license.Features.TryGetValue(_platform, out var features))
            return Enumerable.Empty<string>();

        return features
            .Where(f => DateTime.UtcNow <= f.ExpirationDate ||
                       f.ExpireAction == LicenseExpireActions.PreventUpgrade)
            .Select(f => f.Name);
    }

    public IEnumerable<LicenseFeature> GetExpiringFeatures(int daysAhead)
    {
        if (!_license.Features.TryGetValue(_platform, out var features))
            return Enumerable.Empty<LicenseFeature>();

        var threshold = DateTime.UtcNow.AddDays(daysAhead);

        return features.Where(f =>
            f.ExpirationDate > DateTime.UtcNow &&
            f.ExpirationDate <= threshold);
    }
}

// Usage
var manager = new LicenseManager(license);

// Check if feature exists
if (manager.HasFeature("Trading"))
{
    Console.WriteLine("Trading feature is licensed");
}

// Get all active features
foreach (var feature in manager.GetActiveFeatures())
{
    Console.WriteLine($"Active feature: {feature}");
}

// Get features expiring in next 30 days
foreach (var feature in manager.GetExpiringFeatures(30))
{
    Console.WriteLine($"Feature {feature.Name} expires on {feature.ExpirationDate}");
}
```

### Accessing License Properties

```csharp
using Ecng.Licensing;

// Access basic license information
Console.WriteLine($"License Version: {license.Version}");
Console.WriteLine($"License ID: {license.Id}");
Console.WriteLine($"Issued To: {license.IssuedTo}");
Console.WriteLine($"Issued Date: {license.IssuedDate:yyyy-MM-dd}");

// Access raw license data
byte[] originalBody = license.Body;
byte[] bodyWithoutSignature = license.BodyWithoutSignature;
byte[] signature = license.Signature;

// Get string representation
string licenseInfo = license.ToString(); // Returns "N{Id} ({HardwareId})"
```

## License Expire Actions

The `LicenseExpireActions` enum defines what happens when a license expires:

- **PreventWork**: The feature becomes completely unavailable after expiration
- **PreventUpgrade**: The feature continues to work but cannot be upgraded to newer versions

```csharp
using Ecng.Licensing;

public void HandleExpiration(LicenseFeature feature)
{
    switch (feature.ExpireAction)
    {
        case LicenseExpireActions.PreventWork:
            Console.WriteLine("Feature will stop working after expiration");
            // Disable feature completely
            break;

        case LicenseExpireActions.PreventUpgrade:
            Console.WriteLine("Feature will continue working but no updates allowed");
            // Allow current version usage only
            break;
    }
}
```

## Best Practices

1. **Validate Licenses Early**: Check license validity during application startup
2. **Handle Expiration Gracefully**: Show warnings before license expiration
3. **Secure License Storage**: Store license files securely and validate signatures
4. **Hardware Binding**: Use hardware IDs for additional security when needed
5. **Multi-Platform Support**: Check platform-specific features appropriately
6. **Regular Validation**: Periodically re-validate licenses during application runtime

## Platform Support

- .NET Standard 2.0+
- .NET 6.0+
- .NET 10.0+

Supports Windows, Linux, and macOS platforms.

## Dependencies

- Ecng.Common
- Ecng.Collections
- Ecng.Localization

## License Format

Licenses are stored in XML format with the following structure:

```xml
<license>
  <ver>1.0</ver>
  <id>12345</id>
  <issuedTo>user@example.com</issuedTo>
  <issuedDate>20240101 00:00:00</issuedDate>
  <platforms>
    <platform name="Windows">
      <feature name="Trading" expire="20251231 23:59:59" expireAction="PreventWork" hardwareId="" account="" />
      <feature name="Analytics" expire="20251231 23:59:59" expireAction="PreventUpgrade" hardwareId="" account="" />
    </platform>
  </platforms>
  <signature>BASE64_SIGNATURE</signature>
</license>
```

## See Also

- [Ecng.Common](../Common/) - Common utilities and extensions
- [Ecng.Collections](../Collections/) - Collection utilities
- [Ecng.Localization](../Localization/) - Localization support
