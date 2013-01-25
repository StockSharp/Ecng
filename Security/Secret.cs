
namespace Ecng.Security
{
	using System;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	public class Secret : Equatable<Secret>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Secret"/> class.
		/// </summary>
		public Secret()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Secret"/> class.
		/// </summary>
		/// <param name="passwordBytes"></param>
		/// <param name="salt">The salt.</param>
		public Secret(byte[] passwordBytes, byte[] salt)
			: this(passwordBytes, CryptoAlgorithm.Create(AlgorithmTypes.Hash), salt)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Secret"/> class.
		/// </summary>
		/// <param name="passwordBytes"></param>
		/// <param name="algo"> </param>
		/// <param name="salt">The salt.</param>
		public Secret(byte[] passwordBytes, CryptoAlgorithm algo, byte[] salt)
		{
			if (passwordBytes == null)
				throw new ArgumentNullException("passwordBytes");

			if (algo == null)
				throw new ArgumentNullException("algo");

			if (salt == null)
				throw new ArgumentNullException("salt");

			Algo = algo;
			Hash = algo.Encrypt(passwordBytes);
			Salt = salt;
		}

		public const int DefaultPasswordSize = 128;
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

		[Ignore]
		public CryptoAlgorithm Algo { get; private set; }

		public bool IsValid(byte[] passwordBytes)
		{
			return this == new Secret(passwordBytes, Algo, Salt);
		}

		protected override bool OnEquals(Secret other)
		{
			return
					Hash.SequenceEqual(other.Hash) &&
					Salt.SequenceEqual(other.Salt);
		}
		
		public override int GetHashCode()
		{
			return Hash.GetHashCodeEx() ^ Salt.GetHashCodeEx();
		}

		public override Secret Clone()
		{
			return new Secret { Hash = Hash.ToArray(), Salt = Salt.ToArray() };
		}
	}
}