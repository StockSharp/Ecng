//===============================================================================
// Microsoft patterns & practices Enterprise Library
namespace Ecng.Security.Cryptographers
{
	using System;
	using System.Security.Cryptography;

	using Ecng.Common;

	/// <summary>
	/// <para>Represents basic cryptography services for a <see cref="HashAlgorithm"/>.</para>
	/// </summary>
	public class HashCryptographer : Disposable
	{
		private readonly HashAlgorithm _algorithm;

		/// <summary>
		/// <para>Initialize a new instance of the <see cref="HashCryptographer"/> with an algorithm type and key.</para>
		/// </summary>
		/// <param name="algorithmType">A fully qualifed type name derived from <see cref="HashAlgorithm"/>.</param>
		/// <param name="protectedKey"><para>The key for a <see cref="KeyedHashAlgorithm"/>.</para></param>
		/// <remarks>
		/// While this overload will work with a specified <see cref="HashAlgorithm"/>, the protectedKey 
		/// is only relevant when initializing with a specified <see cref="KeyedHashAlgorithm"/>.
		/// </remarks>
		public HashCryptographer(HashAlgorithm algorithm, byte[] key = default)
		{
			_algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
			
			if (algorithm is KeyedHashAlgorithm keyAlgo)
				keyAlgo.Key = key;
		}

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
}
