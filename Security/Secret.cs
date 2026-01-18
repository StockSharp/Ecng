namespace Ecng.Security;

using System.Linq;
using System.Security.Cryptography;

using Ecng.Common;
using Ecng.Collections;

/// <summary>
/// Secret.
/// </summary>
public class Secret : Equatable<Secret>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Secret"/> class.
	/// </summary>
	public Secret()
	{
	}

	private byte[] _salt;
	private byte[] _hash;

	/// <summary>
	/// Gets or sets the salt.
	/// </summary>
	/// <value>The salt.</value>
	public byte[] Salt
	{
		get => _salt;
		set
		{
			_salt = value;
			_hashCode = null;
		}
	}

	/// <summary>
	/// Gets or sets the hash.
	/// </summary>
	/// <value>The hash.</value>
	public byte[] Hash
	{
		get => _hash;
		set
		{
			_hash = value;
			_hashCode = null;
		}
	}

	/// <inheritdoc />
	protected override bool OnEquals(Secret other)
	{
		if (EnsureGetHashCode() != other.EnsureGetHashCode())
			return false;

		if (Hash is null)
		{
			if (other.Hash != null)
				return false;
		}
		else
		{
			if (other.Hash is null)
				return false;
#if NET6_0_OR_GREATER
			else if (!CryptographicOperations.FixedTimeEquals(Hash, other.Hash))
				return false;
#else
			else if (!Hash.SequenceEqual(other.Hash))
				return false;
#endif
		}

		if (Salt is null)
		{
			if (other.Salt != null)
				return false;
		}
		else
		{
			if (other.Salt is null)
				return false;
#if NET6_0_OR_GREATER
			else if (!CryptographicOperations.FixedTimeEquals(Salt, other.Salt))
				return false;
#else
			else if (!Salt.SequenceEqual(other.Salt))
				return false;
#endif
		}

		return true;
	}

	private int? _hashCode;

	private int EnsureGetHashCode()
		=> _hashCode ??= (Hash?.GetHashCodeEx() ?? 0) ^ (Salt?.GetHashCodeEx() ?? 0);

	/// <inheritdoc />
	public override int GetHashCode() => EnsureGetHashCode();

	/// <inheritdoc />
	public override Secret Clone()
		=> new()
		{
			_hash = _hash?.ToArray(),
			_salt = _salt?.ToArray(),
			_hashCode = _hashCode,
		};
}