namespace Ecng.Security.Cryptographers;

using System;
using System.IO;
using System.Security.Cryptography;

using Ecng.Common;

/// <summary>
/// <para>Represents basic cryptography services for a <see cref="SymmetricAlgorithm"/>.</para>
/// </summary>
/// <remarks>
/// <para>
/// Because the IV (Initialization Vector) has the same distribution as the resulting 
/// ciphertext, the IV is randomly generated and prepended to the ciphertext.
/// </para>
/// </remarks>
public class SymmetricCryptographer : Disposable
{
	private readonly SymmetricAlgorithm _algorithm;

	/// <summary>
	/// <para>Initalize a new instance of the <see cref="SymmetricCryptographer"/> class with an algorithm type and a key.</para>
	/// </summary>
	/// <param name="algorithm"><para>The qualified assembly name of a <see cref="SymmetricAlgorithm"/>.</para></param>
	/// <param name="key"><para>The key for the algorithm.</para></param>
	public SymmetricCryptographer(SymmetricAlgorithm algorithm, byte[] key)
	{
		_algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
		_algorithm.Key = key;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (IsDisposed)
			return;

		_algorithm.Dispose();
	}

	/// <summary>
	/// <para>Encrypts bytes with the initialized algorithm and key.</para>
	/// </summary>
	/// <param name="plaintext"><para>The plaintext in which you wish to encrypt.</para></param>
	/// <returns><para>The resulting ciphertext.</para></returns>
	public byte[] Encrypt(byte[] plaintext)
	{
		byte[] cipherText = null;

		using (ICryptoTransform transform = _algorithm.CreateEncryptor())
		{
			cipherText = Transform(transform, plaintext);
		}

		var output = new byte[IVLength + cipherText.Length];
		Buffer.BlockCopy(_algorithm.IV, 0, output, 0, IVLength);
		Buffer.BlockCopy(cipherText, 0, output, IVLength, cipherText.Length);

		return output;
	}

	/// <summary>
	/// <para>Decrypts bytes with the initialized algorithm and key.</para>
	/// </summary>
	/// <param name="encryptedText"><para>The text which you wish to decrypt.</para></param>
	/// <returns><para>The resulting plaintext.</para></returns>
	public byte[] Decrypt(byte[] encryptedText)
	{
		byte[] output = null;
		byte[] data = ExtractIV(encryptedText);

		using (ICryptoTransform transform = _algorithm.CreateDecryptor())
		{
			output = Transform(transform, data);
		}

		return output;
	}

	private static byte[] Transform(ICryptoTransform transform, byte[] buffer)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		byte[] transformBuffer = null;

		using (MemoryStream ms = new())
		{
			CryptoStream cs = null;
			try
			{
				cs = new CryptoStream(ms, transform, CryptoStreamMode.Write);
				cs.Write(buffer, 0, buffer.Length);
				cs.FlushFinalBlock();
				transformBuffer = ms.ToArray();
			}
			finally
			{
				if (cs != null)
				{
					cs.Close();
					((IDisposable)cs).Dispose();
				} // Close is not called by Dispose
			}
		}

		return transformBuffer;
	}

	private int IVLength
	{
		get
		{
			if (_algorithm.IV is null)
			{
				_algorithm.GenerateIV();
			}

			return _algorithm.IV.Length;
		}
	}

	private byte[] ExtractIV(byte[] encryptedText)
	{
		byte[] initVector = new byte[IVLength];

		if (encryptedText.Length < IVLength + 1)
		{
			throw new CryptographicException("Unable to decrypt data.");
		}

		byte[] data = new byte[encryptedText.Length - IVLength];

		Buffer.BlockCopy(encryptedText, 0, initVector, 0, IVLength);
		Buffer.BlockCopy(encryptedText, IVLength, data, 0, data.Length);

		_algorithm.IV = initVector;

		return data;
	}
}