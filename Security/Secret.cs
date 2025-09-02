namespace Ecng.Security;

using System.Linq;

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

	/// <summary>
	/// Gets or sets the salt.
	/// </summary>
	/// <value>The salt.</value>
	public byte[] Salt { get; set; }

	/// <summary>
	/// Gets or sets the hash.
	/// </summary>
	/// <value>The hash.</value>
	public byte[] Hash { get; set; }

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
			else if (!Hash.SequenceEqual(other.Hash))
				return false;
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
			else if (!Salt.SequenceEqual(other.Salt))
				return false;
		}

		return true;
	}

	private int _hashCode;

	private int EnsureGetHashCode()
	{
		if (_hashCode == 0)
			_hashCode = (Hash?.GetHashCodeEx() ?? 0) ^ (Salt?.GetHashCodeEx() ?? 0);

		return _hashCode;
	}

	/// <inheritdoc />
	public override int GetHashCode() => EnsureGetHashCode();

	/// <inheritdoc />
	public override Secret Clone() => new() { Hash = Hash?.ToArray(), Salt = Salt?.ToArray() };
}