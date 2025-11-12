using System;
using System.IO;
using System.Linq;
using System.Text;
using Ecng.Security;
using Ecng.Common;

namespace Test;

public static class SecurityCompatibilityTest
{
	public static void GenerateReferenceData()
	{
		Console.WriteLine("=== Генерация эталонных данных для тестирования обратной совместимости ===");
		Console.WriteLine($"Target Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
		Console.WriteLine();

		var output = new StringBuilder();
		output.AppendLine("# Эталонные данные для проверки обратной совместимости Security");
		output.AppendLine($"# Сгенерировано: {DateTime.Now}");
		output.AppendLine($"# Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
		output.AppendLine();

		// Тест 1: Rfc2898DeriveBytes - шифрование и дешифрование
		output.AppendLine("## Тест 1: Encrypt/Decrypt с Rfc2898DeriveBytes");
		var password = "MySecretPassword123!";
		var plainText = "Hello, World! This is a test message.";
		var plainBytes = Encoding.UTF8.GetBytes(plainText);
		var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
		var iv = new byte[] { 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

		output.AppendLine($"Password: {password}");
		output.AppendLine($"PlainText: {plainText}");
		output.AppendLine($"Salt: {Convert.ToBase64String(salt)}");
		output.AppendLine($"IV: {Convert.ToBase64String(iv)}");

		var encrypted = plainBytes.Encrypt(password, salt, iv);
		output.AppendLine($"Encrypted: {Convert.ToBase64String(encrypted)}");

		var decrypted = encrypted.Decrypt(password, salt, iv);
		var decryptedText = Encoding.UTF8.GetString(decrypted);
		output.AppendLine($"Decrypted: {decryptedText}");
		output.AppendLine($"Match: {plainText == decryptedText}");
		output.AppendLine();

		// Тест 2: EncryptAes/DecryptAes
		output.AppendLine("## Тест 2: EncryptAes/DecryptAes");
		var encryptedAes = plainBytes.EncryptAes(password, salt, iv);
		output.AppendLine($"EncryptedAes: {Convert.ToBase64String(encryptedAes)}");

		var decryptedAes = encryptedAes.DecryptAes(password, salt, iv);
		var decryptedAesText = Encoding.UTF8.GetString(decryptedAes);
		output.AppendLine($"DecryptedAes: {decryptedAesText}");
		output.AppendLine($"Match: {plainText == decryptedAesText}");
		output.AppendLine();

		// Тест 3: CreateSecret с различными алгоритмами
		output.AppendLine("## Тест 3: CreateSecret с хешированием паролей");
		var testPassword = "TestPassword123";
		var testSalt = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 };

		output.AppendLine($"TestPassword: {testPassword}");
		output.AppendLine($"TestSalt: {Convert.ToBase64String(testSalt)}");

		// Тест с SHA256 (DefaultHashAlgoName)
		var sha256Algo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);
		var secretSha256 = testPassword.CreateSecret(testSalt, sha256Algo);
		output.AppendLine($"Secret.Hash (SHA256): {Convert.ToBase64String(secretSha256.Hash)}");
		output.AppendLine($"Secret.Salt (SHA256): {Convert.ToBase64String(secretSha256.Salt)}");

		// Проверка валидации
		var isValid = secretSha256.IsValid(testPassword, sha256Algo);
		output.AppendLine($"IsValid (correct password): {isValid}");
		var isInvalid = secretSha256.IsValid("WrongPassword", sha256Algo);
		output.AppendLine($"IsValid (wrong password): {isInvalid}");
		output.AppendLine();

		// Тест 4: Hash функции (MD5, SHA256, SHA512)
		output.AppendLine("## Тест 4: Hash функции");
		var testData = Encoding.UTF8.GetBytes("Test data for hashing");
		output.AppendLine($"TestData: {Encoding.UTF8.GetString(testData)}");
		output.AppendLine($"MD5: {testData.Md5()}");
		output.AppendLine($"SHA256: {testData.Sha256()}");
		output.AppendLine($"SHA512: {testData.Sha512()}");
		output.AppendLine();

		// Тест 5: RSA GenerateRsa
		output.AppendLine("## Тест 5: RSA ключи");
		var rsaParams = CryptoHelper.GenerateRsa();
		output.AppendLine($"RSA Modulus Length: {rsaParams.Modulus?.Length ?? 0}");
		output.AppendLine($"RSA Exponent Length: {rsaParams.Exponent?.Length ?? 0}");
		output.AppendLine($"RSA D Length: {rsaParams.D?.Length ?? 0}");

		// Сохраняем RSA ключи для дальнейшего тестирования
		if (rsaParams.Modulus != null)
		{
			output.AppendLine($"RSA Modulus (first 32 bytes): {Convert.ToBase64String(rsaParams.Modulus.Take(32).ToArray())}");
		}
		output.AppendLine();

		// Тест 6: Symmetric алгоритм через CryptoAlgorithm.Create
		output.AppendLine("## Тест 6: CryptoAlgorithm.Create проверка (Symmetric)");
		var symmetricKey = new byte[32]; // 256 bit key for AES
		for (int i = 0; i < symmetricKey.Length; i++) symmetricKey[i] = (byte)i;

		var symmetricAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Symmetric, symmetricKey);
		var symEncrypted = symmetricAlgo.Encrypt(plainBytes);
		output.AppendLine($"Symmetric Encrypted (base64): {Convert.ToBase64String(symEncrypted)}");
		output.AppendLine($"Symmetric Encrypted Length: {symEncrypted.Length}");
		var symDecrypted = symmetricAlgo.Decrypt(symEncrypted);
		var symDecryptedText = Encoding.UTF8.GetString(symDecrypted);
		output.AppendLine($"Symmetric Decrypted: {symDecryptedText}");
		output.AppendLine($"Symmetric Match: {plainText == symDecryptedText}");
		symmetricAlgo.Dispose();
		sha256Algo.Dispose();
		output.AppendLine();

		// Сохраняем в файл
		var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crypto_reference_data.txt");
		File.WriteAllText(outputPath, output.ToString());

		Console.WriteLine(output.ToString());
		Console.WriteLine();
		Console.WriteLine($"Данные сохранены в: {outputPath}");
	}
}
