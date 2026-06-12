namespace Ecng.Security.Cryptographers;

/// <summary>
/// <para>Represents basic cryptography services for a <see cref="HashAlgorithm"/>.</para>
/// </summary>
public class HashCryptographer : Disposable
{
	private readonly HashAlgorithm _algorithm;

	/// <summary>
	/// <para>Initialize a new instance of the <see cref="HashCryptographer"/> with an algorithm type and key.</para>
	/// </summary>
	/// <param name="algorithm">A fully qualifed type name derived from <see cref="HashAlgorithm"/>.</param>
	/// <param name="key"><para>The key for a <see cref="KeyedHashAlgorithm"/>.</para></param>
	/// <remarks>
	/// While this overload will work with a specified <see cref="HashAlgorithm"/>, the protectedKey 
	/// is only relevant when initializing with a specified <see cref="KeyedHashAlgorithm"/>.
	/// </remarks>
	public HashCryptographer(HashAlgorithm algorithm, byte[] key = default)
	{
		if (algorithm is null)
			throw new ArgumentNullException(nameof(algorithm));

		_algorithm = CreateAlgorithm(algorithm, key);
	}

	private static HashAlgorithm CreateAlgorithm(HashAlgorithm algorithm, byte[] key)
	{
		if (key is null || key.Length == 0)
			return algorithm;

		algorithm.Dispose();

		return algorithm switch
		{
			SHA256 => new HMACSHA256(key),
			SHA384 => new HMACSHA384(key),
			SHA512 => new HMACSHA512(key),
			SHA1 => new HMACSHA1(key),
			MD5 => new HMACMD5(key),
			KeyedHashAlgorithm keyed => SetKey(keyed, key),
			_ => throw new NotSupportedException($"Keyed hashing is not supported for algorithm type {algorithm.GetType().Name}"),
		};
	}

	private static KeyedHashAlgorithm SetKey(KeyedHashAlgorithm algorithm, byte[] key)
	{
		algorithm.Key = key;
		return algorithm;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (IsDisposed)
			return;

		_algorithm.Dispose();
	}

	/// <summary>
	/// <para>Computes the hash value of the plaintext.</para>
	/// </summary>
	/// <param name="plaintext"><para>The plaintext in which you wish to hash.</para></param>
	/// <returns><para>The resulting hash.</para></returns>
	public byte[] ComputeHash(byte[] plaintext)
		=> _algorithm.ComputeHash(plaintext);
}
