namespace Ecng.Security
{
	using System;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	public class Secret : Equatable<Secret>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Secret"/> class.
		/// </summary>
		public Secret()
			: this(CryptoAlgorithm.Create(AlgorithmTypes.Hash))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Secret"/> class.
		/// </summary>
		/// <param name="passwordBytes"></param>
		/// <param name="salt">The salt.</param>
		public Secret(byte[] passwordBytes, byte[] salt)
			: this(passwordBytes, salt, CryptoAlgorithm.Create(AlgorithmTypes.Hash))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Secret"/> class.
		/// </summary>
		/// <param name="passwordBytes"></param>
		/// <param name="salt">The salt.</param>
		/// <param name="algo"> </param>
		public Secret(byte[] passwordBytes, byte[] salt, CryptoAlgorithm algo)
			: this(algo)
		{
			Salt = salt ?? throw new ArgumentNullException(nameof(salt));
			Hash = algo.Encrypt(passwordBytes ?? throw new ArgumentNullException(nameof(passwordBytes)));
		}

		private Secret(CryptoAlgorithm algo)
		{
			Algo = algo ?? throw new ArgumentNullException(nameof(algo));
		}

		public const int DefaultSaltSize = 128;

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

		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public CryptoAlgorithm Algo { get; }

		public bool IsValid(byte[] passwordBytes)
		{
			return this == new Secret(passwordBytes, Salt, Algo);
		}

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
		
		public override int GetHashCode()
		{
			return EnsureGetHashCode();
		}

		public override Secret Clone()
		{
			return new Secret { Hash = Hash?.ToArray(), Salt = Salt?.ToArray() };
		}
	}
}