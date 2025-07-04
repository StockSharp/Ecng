# Ecng.Security

Cryptography helpers for hashing, encryption and password storage.

## Purpose

Simplify common crypto scenarios such as AES encryption, RSA key handling and password verification.

## Key Features

- AES helpers for symmetric encryption
- RSA utilities and parameter conversions
- Simple hashing extensions (`data.Md5()`, `data.Sha256()`, ...)
- `Secret` class for salted password hashes

## Hashing

Standard .NET:

```csharp
using var md5 = MD5.Create();
var hash = Convert.ToHexString(md5.ComputeHash(data));
```

With Ecng:

```csharp
var hash = data.Md5();
```

## AES encryption

```csharp
var salt = TypeHelper.GenerateSalt(Secret.DefaultSaltSize);
var iv = new byte[16];
byte[] cipher = plain.EncryptAes("secret", salt, iv);
byte[] result = cipher.DecryptAes("secret", salt, iv);
```

## Passwords

```csharp
Secret secret = "qwerty".CreateSecret();
bool ok = secret.IsValid("qwerty");
```
