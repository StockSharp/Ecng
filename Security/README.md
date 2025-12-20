# Ecng.Security

A comprehensive .NET library providing cryptography helpers for hashing, encryption, and secure password storage. This library simplifies common cryptographic scenarios such as AES encryption, RSA key handling, digital signatures, and password verification.

## Table of Contents

- [Installation](#installation)
- [Features](#features)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
  - [Hashing](#hashing)
  - [AES Encryption](#aes-encryption)
  - [Password Storage](#password-storage)
  - [RSA Encryption](#rsa-encryption)
  - [Digital Signatures](#digital-signatures)
  - [X.509 Certificates](#x509-certificates)
  - [Authorization](#authorization)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)

## Installation

Add a reference to the `Ecng.Security` assembly in your project.

```xml
<PackageReference Include="Ecng.Security" Version="x.x.x" />
```

## Features

- **Simple Hashing Extensions**: MD5, SHA256, SHA512 with one-liner syntax
- **AES Symmetric Encryption**: Easy-to-use AES encryption with PBKDF2 key derivation
- **RSA Asymmetric Encryption**: RSA encryption/decryption with parameter conversion utilities
- **Digital Signatures**: Create and verify RSA/DSA signatures
- **Secure Password Storage**: Salted password hashing with the `Secret` class
- **X.509 Certificate Support**: Simplified cryptography using X.509 certificates
- **Authorization Helpers**: Built-in authorization modules for login validation
- **Cross-Platform**: Supports .NET Standard 2.0, .NET 6.0, and .NET 10.0

## Quick Start

### Hash a String

```csharp
using Ecng.Security;
using Ecng.Common;

byte[] data = "Hello, World!".UTF8();
string hash = data.Sha256();
Console.WriteLine(hash); // Outputs the SHA256 hash as a hex string
```

### Encrypt and Decrypt Data

```csharp
using Ecng.Security;
using Ecng.Common;

// Prepare encryption parameters
byte[] plainText = "Sensitive data".UTF8();
string password = "MySecretPassword";
byte[] salt = TypeHelper.GenerateSalt(CryptoHelper.DefaultSaltSize);
byte[] iv = new byte[16]; // 16 bytes for AES

// Encrypt
byte[] encrypted = plainText.EncryptAes(password, salt, iv);

// Decrypt
byte[] decrypted = encrypted.DecryptAes(password, salt, iv);
string result = decrypted.UTF8();
Console.WriteLine(result); // Outputs: "Sensitive data"
```

### Store and Validate Passwords

```csharp
using Ecng.Security;

// Create a password hash
Secret secret = "MyPassword123".CreateSecret(CryptoAlgorithm.Create(AlgorithmTypes.Hash));

// Validate password
bool isValid = secret.IsValid("MyPassword123", CryptoAlgorithm.Create(AlgorithmTypes.Hash));
Console.WriteLine(isValid); // Outputs: True

bool isInvalid = secret.IsValid("WrongPassword", CryptoAlgorithm.Create(AlgorithmTypes.Hash));
Console.WriteLine(isInvalid); // Outputs: False
```

## API Reference

### Hashing

The library provides extension methods for common hashing algorithms:

#### `Md5(byte[] value)`

Computes the MD5 hash of the input data.

```csharp
using Ecng.Security;
using Ecng.Common;

byte[] data = "Hello".UTF8();
string md5Hash = data.Md5();
```

#### `Sha256(byte[] value)`

Computes the SHA256 hash of the input data.

```csharp
byte[] data = "Hello".UTF8();
string sha256Hash = data.Sha256();
```

#### `Sha512(byte[] value)`

Computes the SHA512 hash of the input data.

```csharp
byte[] data = "Hello".UTF8();
string sha512Hash = data.Sha512();
```

**Comparison with Standard .NET:**

Standard .NET approach:
```csharp
using var md5 = MD5.Create();
var hash = Convert.ToHexString(md5.ComputeHash(data));
```

With Ecng.Security:
```csharp
var hash = data.Md5();
```

### AES Encryption

AES (Advanced Encryption Standard) is a symmetric encryption algorithm. The library uses PBKDF2 for key derivation and CBC mode with PKCS7 padding.

#### `EncryptAes(byte[] plain, string passPhrase, byte[] salt, byte[] iv)`

Encrypts data using AES with a password-based key.

**Parameters:**
- `plain`: The plaintext data to encrypt
- `passPhrase`: The password/passphrase for key derivation
- `salt`: Salt for PBKDF2 (recommended: 128 bytes)
- `iv`: Initialization vector (16 bytes for AES)

**Returns:** Encrypted bytes

```csharp
using Ecng.Security;
using Ecng.Common;

byte[] plainText = "Confidential information".UTF8();
string password = "StrongPassword!";
byte[] salt = TypeHelper.GenerateSalt(128);
byte[] iv = new byte[16];

byte[] encrypted = plainText.EncryptAes(password, salt, iv);
```

#### `DecryptAes(byte[] cipherText, string passPhrase, byte[] salt, byte[] iv)`

Decrypts AES-encrypted data.

**Parameters:**
- `cipherText`: The encrypted data
- `passPhrase`: The password/passphrase used for encryption
- `salt`: Salt used during encryption
- `iv`: Initialization vector used during encryption

**Returns:** Decrypted bytes

```csharp
byte[] decrypted = encrypted.DecryptAes(password, salt, iv);
string original = decrypted.UTF8();
```

**Important Notes:**
- Store the `salt` and `iv` securely alongside the encrypted data
- Use `TypeHelper.GenerateSalt(size)` to generate cryptographically secure random salt
- The IV must be exactly 16 bytes for AES
- The same `salt`, `iv`, and `passPhrase` must be used for both encryption and decryption

**Complete Example:**

```csharp
using Ecng.Security;
using Ecng.Common;

// Setup
byte[] plainText = "Top Secret Data".UTF8();
string password = "SecureP@ssw0rd";
byte[] salt = TypeHelper.GenerateSalt(CryptoHelper.DefaultSaltSize); // 128 bytes
byte[] iv = new byte[16];

// Encrypt
byte[] encrypted = plainText.EncryptAes(password, salt, iv);

// In real applications, store encrypted, salt, and iv
// For example, in a database or file

// Decrypt
byte[] decrypted = encrypted.DecryptAes(password, salt, iv);
Console.WriteLine(decrypted.UTF8()); // Outputs: "Top Secret Data"
```

### Password Storage

The `Secret` class provides secure password storage using salted hashing.

#### `CreateSecret(string plainText, CryptoAlgorithm algo)`

Creates a new `Secret` from a plaintext password.

**Parameters:**
- `plainText`: The password to hash
- `algo`: The cryptographic algorithm to use (typically a hash algorithm)

**Returns:** A `Secret` object containing the salt and hash

```csharp
using Ecng.Security;

// Create hash algorithm
var hashAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);

// Create secret from password
Secret secret = "UserPassword123".CreateSecret(hashAlgo);

// Store secret.Salt and secret.Hash in your database
```

#### `IsValid(Secret secret, string password, CryptoAlgorithm algo)`

Validates a password against a stored `Secret`.

**Parameters:**
- `secret`: The stored secret
- `password`: The password to validate
- `algo`: The algorithm used to create the secret

**Returns:** `true` if the password is valid, `false` otherwise

```csharp
var hashAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);

// Later, when validating login
bool isValid = secret.IsValid("UserPassword123", hashAlgo);

if (isValid)
{
    Console.WriteLine("Login successful!");
}
else
{
    Console.WriteLine("Invalid password!");
}
```

**Overloads with SecureString:**

```csharp
using System.Security;

SecureString securePassword = new SecureString();
foreach (char c in "password")
    securePassword.AppendChar(c);
securePassword.MakeReadOnly();

Secret secret = securePassword.CreateSecret(hashAlgo);
bool isValid = secret.IsValid(securePassword, hashAlgo);
```

**Complete Password Storage Example:**

```csharp
using Ecng.Security;

public class UserService
{
    private readonly CryptoAlgorithm _hashAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);

    public void RegisterUser(string username, string password)
    {
        // Create secret from password
        Secret secret = password.CreateSecret(_hashAlgo);

        // Store in database
        // db.Save(new User
        // {
        //     Username = username,
        //     PasswordHash = secret.Hash,
        //     PasswordSalt = secret.Salt
        // });
    }

    public bool ValidateLogin(string username, string password)
    {
        // Retrieve from database
        // var user = db.GetUser(username);
        // var storedSecret = new Secret
        // {
        //     Hash = user.PasswordHash,
        //     Salt = user.PasswordSalt
        // };

        // Validate password
        // return storedSecret.IsValid(password, _hashAlgo);

        return false; // Placeholder
    }
}
```

### RSA Encryption

RSA is an asymmetric encryption algorithm using public/private key pairs.

#### `GenerateRsa()`

Generates a new RSA key pair.

```csharp
using Ecng.Security;
using System.Security.Cryptography;

// Generate new RSA key pair (includes both public and private keys)
RSAParameters keyPair = CryptoHelper.GenerateRsa();
```

#### `FromRsa(RSAParameters param)` / `ToRsa(byte[] key)`

Converts RSA parameters to/from byte arrays for storage.

```csharp
using Ecng.Security;

// Generate key pair
RSAParameters privateKey = CryptoHelper.GenerateRsa();

// Convert to bytes for storage
byte[] privateKeyBytes = privateKey.FromRsa();

// Later, restore from bytes
RSAParameters restoredKey = privateKeyBytes.ToRsa();
```

#### `PublicPart(RSAParameters param)`

Extracts only the public key portion from an RSA key pair.

```csharp
// Generate private key (which includes public key)
RSAParameters privateKey = CryptoHelper.GenerateRsa();

// Extract public key only
RSAParameters publicKey = privateKey.PublicPart();

// Convert to bytes for sharing
byte[] publicKeyBytes = publicKey.FromRsa();
```

#### RSA Encryption/Decryption

```csharp
using Ecng.Security;
using Ecng.Security.Cryptographers;
using System.Security.Cryptography;
using Ecng.Common;

// Generate key pair
RSAParameters privateKey = CryptoHelper.GenerateRsa();
RSAParameters publicKey = privateKey.PublicPart();

// Convert to bytes
byte[] publicKeyBytes = publicKey.FromRsa();
byte[] privateKeyBytes = privateKey.FromRsa();

// Encrypt with public key
using (var encryptor = new AsymmetricCryptographer(RSA.Create(), publicKeyBytes, null))
{
    byte[] plainText = "Secret message".UTF8();
    byte[] encrypted = encryptor.Encrypt(plainText);

    // Decrypt with private key
    using (var decryptor = new AsymmetricCryptographer(RSA.Create(), null, privateKeyBytes))
    {
        byte[] decrypted = decryptor.Decrypt(encrypted);
        Console.WriteLine(decrypted.UTF8()); // Outputs: "Secret message"
    }
}
```

**Using CryptoAlgorithm:**

```csharp
using Ecng.Security;
using Ecng.Common;

// Generate keys
RSAParameters keyPair = CryptoHelper.GenerateRsa();
byte[] publicKey = keyPair.PublicPart().FromRsa();
byte[] privateKey = keyPair.FromRsa();

// Create algorithm instance
using (var algo = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, publicKey, privateKey))
{
    byte[] plainText = "Sensitive data".UTF8();

    // Encrypt
    byte[] encrypted = algo.Encrypt(plainText);

    // Decrypt
    byte[] decrypted = algo.Decrypt(encrypted);

    Console.WriteLine(decrypted.UTF8()); // Outputs: "Sensitive data"
}
```

### Digital Signatures

Digital signatures verify data authenticity and integrity.

#### Creating a Signature

```csharp
using Ecng.Security;
using Ecng.Common;
using System.Security.Cryptography;

// Generate key pair
RSAParameters keyPair = CryptoHelper.GenerateRsa();
byte[] privateKeyBytes = keyPair.FromRsa();

// Data to sign
byte[] data = "Important document".UTF8();

// Create signature with private key
using (var algo = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, null, privateKeyBytes))
{
    byte[] signature = algo.CreateSignature(data, () => SHA256.Create());
}
```

#### Verifying a Signature

```csharp
using Ecng.Security;

// Extract public key
RSAParameters publicKey = keyPair.PublicPart();
byte[] publicKeyBytes = publicKey.FromRsa();

// Verify signature with public key
using (var verifier = CryptoAlgorithm.CreateAsymmetricVerifier(publicKeyBytes))
{
    bool isValid = verifier.VerifySignature(data, signature);

    if (isValid)
        Console.WriteLine("Signature is valid!");
    else
        Console.WriteLine("Signature is invalid!");
}
```

**Complete Signature Example:**

```csharp
using Ecng.Security;
using Ecng.Common;
using System.Security.Cryptography;

// Generate key pair
RSAParameters keyPair = CryptoHelper.GenerateRsa();
byte[] publicKeyBytes = keyPair.PublicPart().FromRsa();
byte[] privateKeyBytes = keyPair.FromRsa();

// Document to sign
byte[] document = "This is an important contract.".UTF8();

// Sign the document
byte[] signature;
using (var signer = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, null, privateKeyBytes))
{
    signature = signer.CreateSignature(document, () => SHA256.Create());
}

// Verify the signature (can be done by anyone with the public key)
using (var verifier = CryptoAlgorithm.CreateAsymmetricVerifier(publicKeyBytes))
{
    bool isAuthentic = verifier.VerifySignature(document, signature);
    Console.WriteLine($"Document is authentic: {isAuthentic}");
}

// Try to verify tampered document
byte[] tamperedDocument = "This is an modified contract.".UTF8();
using (var verifier = CryptoAlgorithm.CreateAsymmetricVerifier(publicKeyBytes))
{
    bool isAuthentic = verifier.VerifySignature(tamperedDocument, signature);
    Console.WriteLine($"Tampered document is authentic: {isAuthentic}"); // False
}
```

### X.509 Certificates

Use X.509 certificates for encryption and signing.

#### `X509Cryptographer`

Wrapper around X.509 certificates for cryptographic operations.

```csharp
using Ecng.Security.Cryptographers;
using System.Security.Cryptography.X509Certificates;
using Ecng.Common;

// Load certificate from file
X509Certificate2 cert = new X509Certificate2("mycert.pfx", "password");

// Create cryptographer
using (var cryptographer = new X509Cryptographer(cert))
{
    byte[] plainText = "Encrypted with certificate".UTF8();

    // Encrypt
    byte[] encrypted = cryptographer.Encrypt(plainText);

    // Decrypt
    byte[] decrypted = cryptographer.Decrypt(encrypted);

    Console.WriteLine(decrypted.UTF8());
}
```

**Signing with Certificates:**

```csharp
using Ecng.Security.Cryptographers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Ecng.Common;

X509Certificate2 cert = new X509Certificate2("mycert.pfx", "password");

using (var cryptographer = new X509Cryptographer(cert))
{
    byte[] data = "Document to sign".UTF8();

    // Create signature
    byte[] signature = cryptographer.CreateSignature(data, () => SHA256.Create());

    // Verify signature
    bool isValid = cryptographer.VerifySignature(data, signature);
    Console.WriteLine($"Signature valid: {isValid}");
}
```

### Authorization

The library includes authorization interfaces and implementations for login validation.

#### `IAuthorization`

Interface for implementing custom authorization logic.

```csharp
public interface IAuthorization
{
    ValueTask<string> ValidateCredentials(
        string login,
        SecureString password,
        IPAddress clientAddress,
        CancellationToken cancellationToken);
}
```

#### `AnonymousAuthorization`

Allows unrestricted access (useful for testing).

```csharp
using Ecng.Security;

var auth = new AnonymousAuthorization();
string sessionId = await auth.ValidateCredentials(
    "anyuser",
    null,
    IPAddress.Loopback,
    CancellationToken.None);

Console.WriteLine($"Session ID: {sessionId}"); // Always succeeds
```

#### `SimpleAuthorization`

Validates against a single username/password pair.

```csharp
using Ecng.Security;
using System.Security;
using System.Net;

// Create secure password
var securePassword = new SecureString();
foreach (char c in "MyPassword")
    securePassword.AppendChar(c);
securePassword.MakeReadOnly();

// Setup authorization
var auth = new SimpleAuthorization
{
    Login = "admin",
    Password = securePassword
};

// Validate credentials
try
{
    string sessionId = await auth.ValidateCredentials(
        "admin",
        securePassword,
        IPAddress.Parse("192.168.1.1"),
        CancellationToken.None);

    Console.WriteLine($"Login successful! Session: {sessionId}");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Invalid credentials!");
}
```

#### `UnauthorizedAuthorization`

Denies all access (useful for disabling endpoints).

```csharp
using Ecng.Security;

var auth = new UnauthorizedAuthorization();

try
{
    await auth.ValidateCredentials("user", null, null, CancellationToken.None);
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Access denied!"); // Always throws
}
```

## Usage Examples

### Example 1: Secure File Encryption

```csharp
using Ecng.Security;
using Ecng.Common;
using System.IO;

public class SecureFileManager
{
    private readonly string _password;
    private readonly byte[] _salt;

    public SecureFileManager(string password)
    {
        _password = password;
        _salt = TypeHelper.GenerateSalt(CryptoHelper.DefaultSaltSize);
    }

    public void EncryptFile(string inputPath, string outputPath)
    {
        byte[] plainText = File.ReadAllBytes(inputPath);
        byte[] iv = new byte[16];

        byte[] encrypted = plainText.EncryptAes(_password, _salt, iv);

        // Store salt and IV with the encrypted data
        using (var fs = File.Create(outputPath))
        {
            fs.Write(_salt, 0, _salt.Length);
            fs.Write(iv, 0, iv.Length);
            fs.Write(encrypted, 0, encrypted.Length);
        }
    }

    public void DecryptFile(string inputPath, string outputPath)
    {
        byte[] fileData = File.ReadAllBytes(inputPath);

        // Extract salt, IV, and encrypted data
        byte[] salt = new byte[CryptoHelper.DefaultSaltSize];
        byte[] iv = new byte[16];
        byte[] encrypted = new byte[fileData.Length - salt.Length - iv.Length];

        Buffer.BlockCopy(fileData, 0, salt, 0, salt.Length);
        Buffer.BlockCopy(fileData, salt.Length, iv, 0, iv.Length);
        Buffer.BlockCopy(fileData, salt.Length + iv.Length, encrypted, 0, encrypted.Length);

        // Decrypt
        byte[] decrypted = encrypted.DecryptAes(_password, salt, iv);
        File.WriteAllBytes(outputPath, decrypted);
    }
}
```

### Example 2: API Request Signing

```csharp
using Ecng.Security;
using Ecng.Common;
using System.Security.Cryptography;

public class ApiClient
{
    private readonly RSAParameters _privateKey;
    private readonly byte[] _publicKeyBytes;

    public ApiClient()
    {
        // Generate or load keys
        _privateKey = CryptoHelper.GenerateRsa();
        _publicKeyBytes = _privateKey.PublicPart().FromRsa();
    }

    public (byte[] data, byte[] signature) SignRequest(string requestBody)
    {
        byte[] data = requestBody.UTF8();

        using (var signer = CryptoAlgorithm.Create(
            AlgorithmTypes.Asymmetric,
            null,
            _privateKey.FromRsa()))
        {
            byte[] signature = signer.CreateSignature(data, () => SHA256.Create());
            return (data, signature);
        }
    }

    public byte[] GetPublicKey() => _publicKeyBytes;
}

public class ApiServer
{
    public bool VerifyRequest(byte[] data, byte[] signature, byte[] clientPublicKey)
    {
        using (var verifier = CryptoAlgorithm.CreateAsymmetricVerifier(clientPublicKey))
        {
            return verifier.VerifySignature(data, signature);
        }
    }
}
```

### Example 3: User Authentication System

```csharp
using Ecng.Security;
using System;
using System.Collections.Generic;

public class User
{
    public string Username { get; set; }
    public Secret PasswordSecret { get; set; }
}

public class AuthenticationService
{
    private readonly Dictionary<string, User> _users = new();
    private readonly CryptoAlgorithm _hashAlgo;

    public AuthenticationService()
    {
        _hashAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);
    }

    public void RegisterUser(string username, string password)
    {
        if (_users.ContainsKey(username))
            throw new InvalidOperationException("User already exists");

        var user = new User
        {
            Username = username,
            PasswordSecret = password.CreateSecret(_hashAlgo)
        };

        _users[username] = user;
        Console.WriteLine($"User '{username}' registered successfully");
    }

    public bool Login(string username, string password)
    {
        if (!_users.TryGetValue(username, out var user))
        {
            Console.WriteLine("User not found");
            return false;
        }

        bool isValid = user.PasswordSecret.IsValid(password, _hashAlgo);

        if (isValid)
            Console.WriteLine($"User '{username}' logged in successfully");
        else
            Console.WriteLine("Invalid password");

        return isValid;
    }
}

// Usage
var authService = new AuthenticationService();
authService.RegisterUser("alice", "SecurePass123!");
authService.RegisterUser("bob", "AnotherPass456@");

authService.Login("alice", "SecurePass123!");  // Success
authService.Login("alice", "WrongPassword");    // Failure
```

### Example 4: Data Integrity Verification

```csharp
using Ecng.Security;
using Ecng.Common;

public class DataIntegrityChecker
{
    public string ComputeChecksum(byte[] data, string algorithm = "sha256")
    {
        return algorithm.ToLowerInvariant() switch
        {
            "md5" => data.Md5(),
            "sha256" => data.Sha256(),
            "sha512" => data.Sha512(),
            _ => throw new ArgumentException("Unsupported algorithm")
        };
    }

    public bool VerifyChecksum(byte[] data, string expectedChecksum, string algorithm = "sha256")
    {
        string actualChecksum = ComputeChecksum(data, algorithm);
        return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}

// Usage
var checker = new DataIntegrityChecker();
byte[] originalData = "Important data".UTF8();

// Compute checksum
string checksum = checker.ComputeChecksum(originalData, "sha256");
Console.WriteLine($"Checksum: {checksum}");

// Verify data hasn't been modified
bool isValid = checker.VerifyChecksum(originalData, checksum, "sha256");
Console.WriteLine($"Data is valid: {isValid}"); // True

// Verify modified data
byte[] modifiedData = "Modified data".UTF8();
bool isModified = checker.VerifyChecksum(modifiedData, checksum, "sha256");
Console.WriteLine($"Modified data is valid: {isModified}"); // False
```

## Best Practices

### Security Recommendations

1. **Password Storage**
   - Always use `Secret` with salted hashing for password storage
   - Never store passwords in plain text
   - Use `SecureString` when handling passwords in memory

2. **Salt Generation**
   - Use `TypeHelper.GenerateSalt()` to generate cryptographically secure random salts
   - Recommended salt size: 128 bytes (`CryptoHelper.DefaultSaltSize`)
   - Never reuse salts across different passwords

3. **AES Encryption**
   - Always generate a new random salt for each encryption operation
   - Store the salt and IV alongside the encrypted data
   - Use strong passwords/passphrases for key derivation
   - Consider using RSA for encrypting the AES key itself (hybrid encryption)

4. **RSA Key Management**
   - Keep private keys secure and never expose them
   - Use at least 2048-bit RSA keys (default in modern .NET)
   - Share only the public key for encryption and signature verification
   - Store private keys encrypted when persisting to disk

5. **Digital Signatures**
   - Use SHA256 or stronger hash algorithms for signatures
   - Verify signatures before processing signed data
   - Sign data before encryption for non-repudiation

6. **Hashing**
   - For password hashing, use `Secret` class, not direct hash functions
   - For data integrity, SHA256 or SHA512 are recommended
   - MD5 is provided for compatibility but should be avoided for security-critical applications

### Performance Tips

1. **Dispose Cryptographic Objects**
   - Always dispose `CryptoAlgorithm` instances (use `using` statements)
   - This ensures keys are properly cleared from memory

2. **Reuse Algorithm Instances**
   - When performing multiple operations, reuse the same `CryptoAlgorithm` instance
   - Create once and dispose when all operations are complete

3. **Async Operations**
   - The `IAuthorization` interface is async-friendly
   - Use async/await for authorization operations in web applications

4. **Memory Management**
   - Use `SymmetricCryptographer.ZeroOutBytes()` to clear sensitive data from memory
   - Be mindful of byte array allocations when processing large files

### Common Pitfalls to Avoid

1. **Don't hardcode passwords or keys** in source code
2. **Don't use the same IV** for multiple AES encryption operations
3. **Don't forget to store salt and IV** when encrypting data
4. **Don't confuse public and private keys** in RSA operations
5. **Don't skip signature verification** when receiving signed data
6. **Don't use MD5 for security-critical applications**
7. **Don't share private keys** or expose them in logs/error messages

## License

This library is part of the Ecng toolkit. Please refer to the main repository for licensing information.

## Support

For issues, questions, or contributions, please visit the main Ecng repository.
