namespace Ecng.Tests.Security;

using System.IO;

using Ecng.Security;

[TestClass]
public class SecretTests
{
	private const string _correctPwd = "mpoi3e/4nn3(&T(*^R";
	private const string _incorrectPwd = "mpo3e/4nn3(&T(*^R";

	[TestMethod]
	public void EqualsTest()
	{
		var pwd1 = _correctPwd.CreateSecret();
		var pwd2 = _correctPwd.CreateSecret();

		pwd1.Equals(pwd1).AssertTrue();
		pwd1.Equals(pwd2).AssertFalse();

		pwd1.IsValid(_correctPwd).AssertTrue();
		pwd2.IsValid(_correctPwd).AssertTrue();
	}

	[TestMethod]
	public void NonEqualsTest()
	{
		var pwd1 = _correctPwd.CreateSecret();
		var pwd2 = _incorrectPwd.CreateSecret();

		pwd1.Equals(pwd2).AssertFalse();
	}

	[TestMethod]
	public void IsValidTest()
	{
		_correctPwd.CreateSecret().IsValid(_correctPwd).AssertTrue();
		_correctPwd.CreateSecret().IsValid(_incorrectPwd).AssertFalse();
	}

	[TestMethod]
	public void SecureStringTest()
	{
		var correctPwd = _correctPwd.Secure();
		var incorrectPwd = _incorrectPwd.Secure();

		correctPwd.CreateSecret().IsValid(correctPwd).AssertTrue();
		correctPwd.CreateSecret().IsValid(incorrectPwd).AssertFalse();
	}

	[TestMethod]
	public void DecryptReadStreamTest()
	{
		var initVectorBytes = "ss14fgty650h8u82".ASCII();
		var txt = File.ReadAllText("../../../Resources/encrypted_config.bin").Base64().Decrypt("qwerty", initVectorBytes, initVectorBytes).UTF8();

		txt.Length.AssertEqual(65735);
		txt.UTF8().Md5().AssertEqual("96FB3B2D0226B0A18903E929E1238557");
	}

	[TestMethod]
	public void RsaParameters_ConvertToAndFromBytes()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var bytes = rsa.FromRsa();
		var restored = bytes.ToRsa();
		restored.Modulus.AssertEqual(rsa.Modulus);
		restored.Exponent.AssertEqual(rsa.Exponent);
	}

	[TestMethod]
	public void RsaParameters_PublicPart()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var pub = rsa.PublicPart();
		pub.Exponent.AssertEqual(rsa.Exponent);
		pub.Modulus.AssertEqual(rsa.Modulus);
	}

	[TestMethod]
	public void EncryptDecrypt_Aes()
	{
		var plain = "test data".UTF8();
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		var pass = "password";
		var encrypted = plain.Encrypt(pass, salt, iv);
		var decrypted = encrypted.Decrypt(pass, salt, iv);
		decrypted.AssertEqual(plain);
	}

	[TestMethod]
	public void EncryptDecrypt_TransformAes()
	{
		var plain = "test data2".UTF8();
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		var pass = "password2";
		var encrypted = plain.EncryptAes(pass, salt, iv);
		var decrypted = encrypted.DecryptAes(pass, salt, iv);
		decrypted.AssertEqual(plain);
	}

	[TestMethod]
	public void Encrypt_ThrowsOnNulls()
	{
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		Assert.ThrowsExactly<ArgumentNullException>(() => ((byte[])null).Encrypt("pass", salt, iv));
		Assert.ThrowsExactly<ArgumentNullException>(() => "data".UTF8().Encrypt(null, salt, iv));
	}

	[TestMethod]
	public void IsValid_SecureString_ThrowsOnNull()
	{
		Secret secret = _correctPwd.CreateSecret();
		Assert.ThrowsExactly<ArgumentNullException>(() => secret.IsValid((System.Security.SecureString)null));
	}

	[TestMethod]
	public void IsValid_String_ThrowsOnNull()
	{
		Secret secret = _correctPwd.CreateSecret();
		Assert.ThrowsExactly<ArgumentNullException>(() => secret.IsValid((string)null));
	}

	[TestMethod]
	public void CreateSecret_WithSecretArg()
	{
		var secret1 = _correctPwd.CreateSecret();
		var secret2 = _correctPwd.CreateSecret(secret1);
		secret2.IsValid(_correctPwd).AssertTrue();
	}

	[TestMethod]
	public void CreateSecret_ThrowsOnNull()
	{
		System.Security.SecureString s = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => s.CreateSecret());
	}

	[TestMethod]
	public void Md5_Sha256_Sha512_Correctness()
	{
		var data = "abc".UTF8();
		// Проверка корректности хэшей
		data.Md5().AssertEqual("900150983CD24FB0D6963F7D28E17F72");
		data.Sha256().AssertEqual("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
		data.Sha512().AssertEqual("DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F");
	}

	[TestMethod]
	public void CreateSecret_CopiesSaltAndAlgo()
	{
		var salt = TypeHelper.GenerateSalt(Secret.DefaultSaltSize);
		var algo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);
		var secret1 = _correctPwd.CreateSecret(salt, algo);
		var secret2 = _correctPwd.CreateSecret(secret1);
		secret2.Salt.AssertEqual(secret1.Salt);
		(secret2.Algo?.GetType()).AssertEqual(secret1.Algo?.GetType());
	}

	[TestMethod]
	public void CreateSecret_EmptyString_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => "".CreateSecret());
	}

	[TestMethod]
	public void CreateSecret_NullString_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ((string)null).CreateSecret());
	}

	[TestMethod]
	public void CreateSecret_NullSalt_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => _correctPwd.CreateSecret(null));
	}

	[TestMethod]
	public void CreateSecret_NullSecret_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => _correctPwd.CreateSecret((Secret)null));
	}

	[TestMethod]
	public void CreateSecret_SecureString_Correct()
	{
		var s = _correctPwd.Secure();
		var secret = s.CreateSecret();
		secret.IsValid(_correctPwd).AssertTrue();
	}

	[TestMethod]
	public void CreateSecret_SecureString_Null_Throws()
	{
		System.Security.SecureString s = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => s.CreateSecret());
	}

	[TestMethod]
	public void IsValid_NullSecret_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ((Secret)null).IsValid(_correctPwd));
	}

	[TestMethod]
	public void IsValid_NullPassword_Throws()
	{
		var secret = _correctPwd.CreateSecret();
		Assert.ThrowsExactly<ArgumentNullException>(() => secret.IsValid((string)null));
	}

	[TestMethod]
	public void IsValid_NullSecureString_Throws()
	{
		var secret = _correctPwd.CreateSecret();
		Assert.ThrowsExactly<ArgumentNullException>(() => secret.IsValid((System.Security.SecureString)null));
	}

	[TestMethod]
	public void EncryptDecryptAes_RandomData()
	{
		var plain = System.Text.Encoding.UTF8.GetBytes("random data");
		var salt = System.Text.Encoding.ASCII.GetBytes("salt123456789012");
		var iv = System.Text.Encoding.ASCII.GetBytes("iv12345678901234");
		var pass = "password";
		var encrypted = plain.EncryptAes(pass, salt, iv);
		var decrypted = encrypted.DecryptAes(pass, salt, iv);
		decrypted.AssertEqual(plain);
	}

	[TestMethod]
	public void EncryptDecrypt_NullPlain_Throws()
	{
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		Assert.ThrowsExactly<ArgumentNullException>(() => ((byte[])null).Encrypt("pass", salt, iv));
	}

	[TestMethod]
	public void EncryptDecrypt_NullPass_Throws()
	{
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		Assert.ThrowsExactly<ArgumentNullException>(() => "data".UTF8().Encrypt(null, salt, iv));
	}

	[TestMethod]
	public void ToRsa_Null_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ((byte[])null).ToRsa());
	}

	[TestMethod]
	public void FromRsa_ToRsa_Correctness()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var bytes = rsa.FromRsa();
		var restored = bytes.ToRsa();
		restored.Modulus.AssertEqual(rsa.Modulus);
		restored.Exponent.AssertEqual(rsa.Exponent);
	}
}