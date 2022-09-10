namespace Ecng.Security
{
	using System;
	using System.Collections.Generic;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Reflection;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public enum AlgorithmTypes
	{
		Symmetric,
		Asymmetric,
		[Obsolete]
		Dpapi,
		Hash,
	}

	[Serializable]
	public class CryptoAlgorithm : Equatable<CryptoAlgorithm>, IDisposable
	{
		#region Private Fields

		private static readonly IDictionary<string, Type> _types;

		private readonly AlgorithmTypes _type;
		private readonly SymmetricCryptographer _symmetric;
		private readonly AsymmetricCryptographer _asymmetric;
		private readonly HashCryptographer _hash;

		#endregion

		#region CryptoAlgorithm.cctor()

		static CryptoAlgorithm()
		{
			_types = new Dictionary<string, Type>();

			foreach (var entry in typeof(CryptoConfig).GetValue<VoidType, Dictionary<string, object>>("DefaultNameHT", null))
			{
				if (entry.Value is Type value)
					_types.Add(entry.Key, value);
			}
		}

		#endregion

		#region CryptoAlgorithm.ctor()

		public CryptoAlgorithm(SymmetricCryptographer symmetric)
		{
			_type = AlgorithmTypes.Symmetric;
			_symmetric = symmetric ?? throw new ArgumentNullException(nameof(symmetric));
		}

		public CryptoAlgorithm(AsymmetricCryptographer asymmetric)
		{
			_type = AlgorithmTypes.Asymmetric;
			_asymmetric = asymmetric ?? throw new ArgumentNullException(nameof(asymmetric));
		}

		public CryptoAlgorithm(HashCryptographer hash)
		{
			_type = AlgorithmTypes.Hash;
			_hash = hash ?? throw new ArgumentNullException(nameof(hash));
		}

		#endregion

		#region Public Constants

		public const string DefaultSymmetricAlgoName = "Rijndael";
		public const string DefaultAsymmetricAlgoName = "RSA";
		public const string DefaultHashAlgoName = "SHA";

		#endregion

		#region GetAlgType

		public static AlgorithmTypes GetAlgType(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			return GetAlgType(_types[name]);
		}

		public static AlgorithmTypes GetAlgType(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (type.Is<SymmetricAlgorithm>())
				return AlgorithmTypes.Symmetric;
			else if (type.Is<AsymmetricAlgorithm>())
				return AlgorithmTypes.Asymmetric;
			else if (type.Is<HashAlgorithm>())
				return AlgorithmTypes.Hash;
			else
				throw new ArgumentException($"Type {type} doesnt't supported.", nameof(type));
		}

		#endregion

		public static Type GetDefaultAlgo(AlgorithmTypes type)
		{
			var name = type switch
			{
				AlgorithmTypes.Symmetric => DefaultSymmetricAlgoName,
				AlgorithmTypes.Asymmetric => DefaultAsymmetricAlgoName,
				AlgorithmTypes.Hash => DefaultHashAlgoName,
				_ => throw new ArgumentOutOfRangeException(nameof(type)),
			};
			return GetAlgo(name);
		}

		public static Type GetAlgo(string name)
		{
			if (_types.TryGetValue(name, out var type))
				return type;

			if (name == "RSA")
				return typeof(RSACryptoServiceProvider);
			else if (name == "SHA")
				return typeof(SHA1);

			throw new ArgumentException($"Algorithm {name} not found", nameof(name));
		}

		#region Create

		public static CryptoAlgorithm CreateAssymetricVerifier(byte[] publicKey) => new(new AsymmetricCryptographer(GetDefaultAlgo(AlgorithmTypes.Asymmetric), publicKey));

		public static CryptoAlgorithm Create(AlgorithmTypes type, params byte[][] keys)
		{
			return Create(GetDefaultAlgo(type), keys);
		}

		public static CryptoAlgorithm Create(Type type, params byte[][] keys)
		{
			if (keys is null)
				throw new ArgumentNullException(nameof(keys));

			var name = GetAlgType(type);

			return name switch
			{
				AlgorithmTypes.Symmetric => new(new SymmetricCryptographer(type, keys[0])),
				AlgorithmTypes.Asymmetric => new(new AsymmetricCryptographer(type, keys[0], keys[1])),
				AlgorithmTypes.Hash => new(keys.Length == 0 ? new HashCryptographer(type) : new HashCryptographer(type, keys[0])),
				_ => throw new ArgumentOutOfRangeException(nameof(name), name.To<string>()),
			};
		}

		#endregion

		#region Encrypt

		public byte[] Encrypt(byte[] data)
		{
			return _type switch
			{
				AlgorithmTypes.Symmetric => _symmetric.Encrypt(data),
				AlgorithmTypes.Asymmetric => _asymmetric.Encrypt(data),
				AlgorithmTypes.Hash => _hash.ComputeHash(data),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		#endregion

		#region Decrypt

		public byte[] Decrypt(byte[] data)
		{
			return _type switch
			{
				AlgorithmTypes.Symmetric => _symmetric.Decrypt(data),
				AlgorithmTypes.Asymmetric => _asymmetric.Decrypt(data),
				AlgorithmTypes.Hash => throw new NotSupportedException(),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		#endregion

		public byte[] CreateSignature(byte[] data)
		{
			return _type switch
			{
				AlgorithmTypes.Asymmetric => _asymmetric.CreateSignature(data),
				_ => throw new NotSupportedException(),
			};
		}

		public bool VerifySignature(byte[] data, byte[] signature)
		{
			return _type switch
			{
				AlgorithmTypes.Asymmetric => _asymmetric.VerifySignature(data, signature),
				_ => throw new NotSupportedException(),
			};
		}

		#region Disposable Members

		public void Dispose()
		{
			if (_symmetric != null)
				_symmetric.Dispose();

			if (_asymmetric != null)
				_asymmetric.Dispose();
		}

		#endregion

		protected override bool OnEquals(CryptoAlgorithm other)
		{
			return _type == other._type;
		}

		public override CryptoAlgorithm Clone()
		{
			if (_symmetric is not null)
				return new(_symmetric);
			else if (_asymmetric is not null)
				return new(_asymmetric);
			else if (_hash is not null)
				return new(_hash);
			else
				throw new InvalidOperationException();
		}
	}
}