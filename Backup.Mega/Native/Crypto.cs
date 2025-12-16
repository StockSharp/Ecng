namespace Ecng.Backup.Mega.Native;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class Crypto
{
	private static readonly byte[] _aesZeroIv = new byte[16];

	public static byte[] CreateAesKey()
	{
		using var aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.KeySize = 128;
		aes.Padding = PaddingMode.None;
		aes.GenerateKey();
		return aes.Key;
	}

	public static ICryptoTransform CreateAesEncryptor(byte[] key)
	{
		var aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.None;
		return aes.CreateEncryptor(key, _aesZeroIv);
	}

	public static byte[] EncryptAes(byte[] data, byte[] key)
	{
		using var aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.None;
		using var enc = aes.CreateEncryptor(key, _aesZeroIv);
		return enc.TransformFinalBlock(data, 0, data.Length);
	}

	public static byte[] EncryptAes(byte[] data, ICryptoTransform encryptor)
		=> encryptor.TransformFinalBlock(data, 0, data.Length);

	public static byte[] DecryptAes(byte[] data, byte[] key)
	{
		using var aes = Aes.Create();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.None;
		using var dec = aes.CreateDecryptor(key, _aesZeroIv);
		return dec.TransformFinalBlock(data, 0, data.Length);
	}

	public static byte[] EncryptKey(byte[] data, byte[] key)
	{
		var output = new byte[data.Length];
		using var enc = CreateAesEncryptor(key);

		for (var i = 0; i < data.Length; i += 16)
		{
			var block = new byte[16];
			Array.Copy(data, i, block, 0, 16);
			var encrypted = EncryptAes(block, enc);
			Array.Copy(encrypted, 0, output, i, 16);
		}

		return output;
	}

	public static byte[] DecryptKey(byte[] data, byte[] key)
	{
		var output = new byte[data.Length];

		for (var i = 0; i < data.Length; i += 16)
		{
			var block = new byte[16];
			Array.Copy(data, i, block, 0, 16);
			var decrypted = DecryptAes(block, key);
			Array.Copy(decrypted, 0, output, i, 16);
		}

		return output;
	}

	public static void GetPartsFromDecryptedKey(byte[] decryptedKey, out byte[] iv, out byte[] metaMac, out byte[] fileKey)
	{
		iv = new byte[8];
		metaMac = new byte[8];
		Array.Copy(decryptedKey, 16, iv, 0, 8);
		Array.Copy(decryptedKey, 24, metaMac, 0, 8);

		fileKey = new byte[16];
		for (var i = 0; i < 16; i++)
			fileKey[i] = (byte)(decryptedKey[i] ^ decryptedKey[i + 16]);
	}

	public static string ToBase64Url(this byte[] data)
	{
		var s = Convert.ToBase64String(data);
		return s.Replace('+', '-').Replace('/', '_').TrimEnd('=');
	}

	public static byte[] FromBase64Url(this string data)
	{
		var s = data.Replace('-', '+').Replace('_', '/').Replace(",", string.Empty);
		s = s.PadRight((s.Length + 3) / 4 * 4, '=');
		return Convert.FromBase64String(s);
	}

	public static byte[] ToBytesUtf8(this string s) => Encoding.UTF8.GetBytes(s);

	public static byte[] ToBytesPassword(this string s)
	{
		var words = new uint[(s.Length + 3) >> 2];

		for (var i = 0; i < s.Length; i++)
			words[i >> 2] |= (uint)s[i] << (24 - (i & 3) * 8);

		var ret = new byte[words.Length * 4];

		for (var i = 0; i < words.Length; i++)
			BinaryPrimitives.WriteUInt32BigEndian(ret.AsSpan(i * 4, 4), words[i]);

		return ret;
	}

	public static byte[] PrepareKey(byte[] data)
	{
		var key = new byte[]
		{
			147, 196, 103, 227, 125, 176, 199, 164, 209, 190,
			63, 129, 1, 82, 203, 86
		};

		for (var i = 0; i < 65536; i++)
		{
			for (var j = 0; j < data.Length; j += 16)
			{
				var k = new byte[16];
				Array.Copy(data, j, k, 0, Math.Min(16, data.Length - j));
				key = EncryptAes(key, k);
			}
		}

		return key;
	}

	public static string GenerateHash(string emailLower, byte[] passwordAesKey)
	{
		var emailBytes = emailLower.ToBytesUtf8();
		var hashInput = new byte[16];

		for (var i = 0; i < emailBytes.Length; i++)
			hashInput[i % 16] ^= emailBytes[i];

		using var enc = CreateAesEncryptor(passwordAesKey);

		for (var j = 0; j < 16384; j++)
			hashInput = EncryptAes(hashInput, enc);

		var output = new byte[8];
		Array.Copy(hashInput, 0, output, 0, 4);
		Array.Copy(hashInput, 8, output, 4, 4);
		return output.ToBase64Url();
	}

	public static string GenerateHashcashToken(string challenge)
	{
		var parts = challenge.Split(':');

		if (parts.Length < 4)
			throw new ArgumentException("Invalid challenge format.", nameof(challenge));

		if (int.Parse(parts[0]) != 1)
			throw new ArgumentException("Hashcash challenge is not version 1.", nameof(challenge));

		var num = int.Parse(parts[1]);
		var value = parts[3];

		var num2 = ((num & 0x3F) << 1) + 1;
		var num3 = (num >> 6) * 7 + 3;
		var num4 = num2 << num3;

		var bytes = value.FromBase64Url();

		const int blocks = 262144;
		const int width = 48;
		const int prefix = 4;

		var data = new byte[prefix + blocks * width];

		for (var i = 0; i < blocks; i++)
			Buffer.BlockCopy(bytes, 0, data, prefix + i * width, width);

		using var sha = SHA256.Create();

		while (true)
		{
			var hash = sha.ComputeHash(data);
			var first = BinaryPrimitives.ReadUInt32BigEndian(hash);

			if (first <= num4)
				break;

			var idx = 0;
			do
			{
				data[idx]++;
			}
			while (data[idx++] == 0);
		}

		var proof = new byte[4];
		Array.Copy(data, 0, proof, 0, 4);
		return "1:" + value + ":" + proof.ToBase64Url();
	}

	public static BigInteger[] GetRsaPrivateKeyComponents(byte[] encodedRsaPrivateKey, byte[] masterKey)
	{
		encodedRsaPrivateKey = Pad16(encodedRsaPrivateKey);

		var decrypted = DecryptKey(encodedRsaPrivateKey, masterKey);
		var span = new ReadOnlySpan<byte>(decrypted);

		var parts = new BigInteger[4];

		for (var i = 0; i < 4; i++)
			parts[i] = ReadMpi(ref span);

		return parts;
	}

	public static byte[] RsaDecrypt(BigInteger data, BigInteger p, BigInteger q, BigInteger d)
	{
		var n = p * q;
		var x = BigInteger.ModPow(data, d, n);
		return ToBigEndianUnsigned(x);
	}

	public static BigInteger FromMpiNumber(byte[] data)
	{
		var span = new ReadOnlySpan<byte>(data);
		return ReadMpi(ref span);
	}

	private static BigInteger ReadMpi(ref ReadOnlySpan<byte> data)
	{
		if (data.Length < 2)
			throw new ArgumentException("Invalid MPI.");

		var bitLen = (data[0] << 8) | data[1];
		var byteLen = (bitLen + 7) / 8;

		if (data.Length < 2 + byteLen)
			throw new ArgumentException("Invalid MPI.");

		var mpi = data.Slice(2, byteLen);
		data = data.Slice(2 + byteLen);

		return FromBigEndianUnsigned(mpi);
	}

	private static BigInteger FromBigEndianUnsigned(ReadOnlySpan<byte> bytes)
	{
		// BigInteger expects little-endian two's complement.
		var tmp = new byte[bytes.Length + 1];
		for (var i = 0; i < bytes.Length; i++)
			tmp[i] = bytes[bytes.Length - 1 - i];
		tmp[tmp.Length - 1] = 0;
		return new BigInteger(tmp);
	}

	private static byte[] ToBigEndianUnsigned(BigInteger value)
	{
		if (value.Sign < 0)
			throw new ArgumentOutOfRangeException(nameof(value));

		var le = value.ToByteArray(); // little-endian two's complement

		// Remove sign byte if present.
		if (le.Length > 1 && le[^1] == 0)
			Array.Resize(ref le, le.Length - 1);

		Array.Reverse(le);
		return le;
	}

	private static byte[] Pad16(byte[] data)
	{
		var pad = (16 - data.Length % 16) % 16;
		if (pad == 0)
			return data;

		var ret = new byte[data.Length + pad];
		Array.Copy(data, ret, data.Length);
		return ret;
	}

	public static byte[] EncryptAttributes(MegaAttributes attributes, byte[] nodeKey)
	{
		var json = JsonSerializer.Serialize(attributes);
		var payload = ("MEGA" + json).ToBytesUtf8();
		payload = Pad16(payload);
		return EncryptAes(payload, nodeKey);
	}

	public static MegaAttributes DecryptAttributes(byte[] encryptedAttributes, byte[] nodeKey)
	{
		var data = DecryptAes(encryptedAttributes, nodeKey);

		try
		{
			var text = Encoding.UTF8.GetString(data);
			if (text.StartsWith("MEGA", StringComparison.Ordinal))
				text = text.Substring(4);

			var nullPos = text.IndexOf('\0');
			if (nullPos >= 0)
				text = text.Substring(0, nullPos);

			return JsonSerializer.Deserialize<MegaAttributes>(text);
		}
		catch (Exception ex)
		{
			return new MegaAttributes { Name = $"Attribute deserialization failed: {ex.Message}" };
		}
	}

	public static long ToEpochSeconds(this DateTime dt)
	{
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return (long)(dt.ToUniversalTime() - epoch).TotalSeconds;
	}

	public static DateTime FromEpochSeconds(this long seconds)
	{
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds(seconds).ToLocalTime();
	}

	public static byte[] SerializeToBytes(this long value)
	{
		var buf = new byte[9];
		byte len = 0;
		var x = value;

		while (x != 0)
		{
			buf[++len] = (byte)x;
			x >>= 8;
		}

		buf[0] = len;
		Array.Resize(ref buf, len + 1);
		return buf;
	}

	public static long DeserializeToLong(this byte[] data, int index, int length)
	{
		var len = data[index];
		long ret = 0;

		if (len > 8 || len >= length)
			throw new ArgumentException("Invalid value.");

		while (len > 0)
			ret = (ret << 8) + data[index + len--];

		return ret;
	}
}

internal sealed class MegaAttributes
{
	[System.Text.Json.Serialization.JsonPropertyName("n")]
	public string Name { get; set; }

	[System.Text.Json.Serialization.JsonPropertyName("c")]
	public string SerializedFingerprint { get; set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public DateTime? ModificationDate { get; private set; }

	public static MegaAttributes Create(string name, Stream stream, DateTime? modificationDate)
	{
		var attrs = new MegaAttributes { Name = name };

		if (modificationDate is null)
			return attrs;

		var fingerprint = new byte[25];
		Buffer.BlockCopy(Crc32.ComputeMegaCrc(stream), 0, fingerprint, 0, 16);

		var epochBytes = modificationDate.Value.ToEpochSeconds().SerializeToBytes();
		Buffer.BlockCopy(epochBytes, 0, fingerprint, 16, epochBytes.Length);

		Array.Resize(ref fingerprint, fingerprint.Length - 9 + epochBytes.Length);
		attrs.SerializedFingerprint = fingerprint.ToBase64Url();
		attrs.ModificationDate = modificationDate.Value;
		return attrs;
	}

	public void HydrateAfterDeserialize()
	{
		if (SerializedFingerprint is null)
			return;

		var bytes = SerializedFingerprint.FromBase64Url();
		ModificationDate = bytes.DeserializeToLong(16, bytes.Length - 16).FromEpochSeconds();
	}
}
