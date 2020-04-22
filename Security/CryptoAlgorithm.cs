namespace Ecng.Security
{
	using System;
	using System.Collections.Generic;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Serialization;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public enum AlgorithmTypes
	{
		Symmetric,
		Asymmetric,
		Dpapi,
		Hash,
	}

	[Serializable]
	public class CryptoAlgorithm : Serializable<CryptoAlgorithm>, IDisposable
	{
		#region Private Fields

		private static readonly IDictionary<string, Type> _types;

		private readonly AlgorithmTypes _type;
		private readonly SymmetricCryptographer _symmetric;
		private readonly AsymmetricCryptographer _asymmetric;
		private readonly DpapiCryptographer _dpapi;
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

			_types.Add(DefaultDpapiAlgoName, typeof(DpapiCryptographer));
		}

		#endregion

		#region CryptoAlgorithm.ctor()

		public CryptoAlgorithm(SymmetricCryptographer symmetric)
		{
			if (symmetric == null)
				throw new ArgumentNullException(nameof(symmetric));

			_type = AlgorithmTypes.Symmetric;
			_symmetric = symmetric;
		}

		public CryptoAlgorithm(AsymmetricCryptographer asymmetric)
		{
			if (asymmetric == null)
				throw new ArgumentNullException(nameof(asymmetric));

			_type = AlgorithmTypes.Asymmetric;
			_asymmetric = asymmetric;
		}

		public CryptoAlgorithm(DpapiCryptographer dpapi)
		{
			if (dpapi == null)
				throw new ArgumentNullException(nameof(dpapi));

			_type = AlgorithmTypes.Dpapi;
			_dpapi = dpapi;
		}

		public CryptoAlgorithm(HashCryptographer hash)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));

			_type = AlgorithmTypes.Hash;
			_hash = hash;
		}

		#endregion

		#region Public Constants

		public const string DefaultSymmetricAlgoName = "Rijndael";
		public const string DefaultAsymmetricAlgoName = "RSA";
		public const string DefaultDpapiAlgoName = "Dpapi";
		public const string DefaultHashAlgoName = "SHA";

		public const DataProtectionScope DefaultScope = DataProtectionScope.LocalMachine;

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
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (type == typeof(DpapiCryptographer))
				return AlgorithmTypes.Dpapi;
			else if (typeof(SymmetricAlgorithm).IsAssignableFrom(type))
				return AlgorithmTypes.Symmetric;
			else if (typeof(AsymmetricAlgorithm).IsAssignableFrom(type))
				return AlgorithmTypes.Asymmetric;
			else if (typeof(HashAlgorithm).IsAssignableFrom(type))
				return AlgorithmTypes.Hash;
			else
				throw new ArgumentException("Type {0} doesnt't supported.".Put(type), nameof(type));
		}

		#endregion

		//#region GenerateKeys

		//public static ProtectedKey[] GenerateKeys(AlgorithmTypes type)
		//{
		//	string name;

		//	switch (type)
		//	{
		//		case AlgorithmTypes.Symmetric:
		//			name = DefaultSymmetricAlgoName;
		//			break;
		//		case AlgorithmTypes.Asymmetric:
		//			name = DefaultAsymmetricAlgoName;
		//			break;
		//		case AlgorithmTypes.Dpapi:
		//			name = DefaultDpapiAlgoName;
		//			break;
		//		case AlgorithmTypes.Hash:
		//			name = DefaultHashAlgoName;
		//			break;
		//		default:
		//			throw new ArgumentOutOfRangeException("type");
		//	}

		//	return GenerateKeys(name);
		//}

		//public static ProtectedKey[] GenerateKeys(string name)
		//{
		//	if (name.IsEmpty())
		//		throw new ArgumentNullException("name");

		//	var type = _types[name];

		//	switch (GetAlgType(type))
		//	{
		//		case AlgorithmTypes.Symmetric:
		//			return new[] { new SymmetricKeyGenerator().GenerateKey(type, DefaultScope) };
		//		case AlgorithmTypes.Asymmetric:
		//			var newParams = CryptoHelper.GenerateRsa();
		//			return new[] { newParams.PublicPart().FromRsa(), newParams.FromRsa() };
		//		case AlgorithmTypes.Dpapi:
		//			return new ProtectedKey[0];
		//		case AlgorithmTypes.Hash:
		//			return new[] { new KeyedHashKeyGenerator().GenerateKey(type, DefaultScope) };
		//		default:
		//			throw new ArgumentException("name");
		//	}
		//}

		//#endregion

		public static Type GetDefaultAlgo(AlgorithmTypes type)
		{
			string name;

			switch (type)
			{
				case AlgorithmTypes.Symmetric:
					name = DefaultSymmetricAlgoName;
					break;
				case AlgorithmTypes.Asymmetric:
					name = DefaultAsymmetricAlgoName;
					break;
				case AlgorithmTypes.Dpapi:
					name = DefaultDpapiAlgoName;
					break;
				case AlgorithmTypes.Hash:
					name = DefaultHashAlgoName;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type));
			}

			return GetAlgo(name);
		}

		public static Type GetAlgo(string name)
		{
			if (_types.TryGetValue(name, out var type))
				return type;

			if (name == "RSA")
				return typeof(RSACryptoServiceProvider);
			else if (name == "SHA")
				return typeof(SHA1Managed);

			throw new ArgumentException("Algorithm {0} not found".Put(name), nameof(name));
		}

		#region Create

		public static CryptoAlgorithm CreateAssymetricVerifier(byte[] publicKey) => new CryptoAlgorithm(new AsymmetricCryptographer(GetDefaultAlgo(AlgorithmTypes.Asymmetric), publicKey));

		public static CryptoAlgorithm Create(AlgorithmTypes type, params ProtectedKey[] keys)
		{
			return Create(GetDefaultAlgo(type), keys);
		}

		public static CryptoAlgorithm Create(Type type, params ProtectedKey[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));

			switch (GetAlgType(type))
			{
				case AlgorithmTypes.Symmetric:
					return new CryptoAlgorithm(new SymmetricCryptographer(type, keys[0]));
				case AlgorithmTypes.Asymmetric:
					return new CryptoAlgorithm(new AsymmetricCryptographer(type, keys[0], keys[1]));
				case AlgorithmTypes.Dpapi:
					return new CryptoAlgorithm(new DpapiCryptographer(DefaultScope));
				case AlgorithmTypes.Hash:
					return new CryptoAlgorithm(keys.Length == 0 ? new HashCryptographer(type) : new HashCryptographer(type, keys[0]));
				default:
					throw new ArgumentException("name");
			}
		}

		#endregion

		#region Encrypt

		public byte[] Encrypt(byte[] data)
		{
			switch (_type)
			{
				case AlgorithmTypes.Symmetric:
					return _symmetric.Encrypt(data);
				case AlgorithmTypes.Asymmetric:
					return _asymmetric.Encrypt(data);
				case AlgorithmTypes.Dpapi:
					return _dpapi.Encrypt(data);
				case AlgorithmTypes.Hash:
					return _hash.ComputeHash(data);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Decrypt

		public byte[] Decrypt(byte[] data)
		{
			switch (_type)
			{
				case AlgorithmTypes.Symmetric:
					return _symmetric.Decrypt(data);
				case AlgorithmTypes.Asymmetric:
					return _asymmetric.Decrypt(data);
				case AlgorithmTypes.Dpapi:
					return _dpapi.Decrypt(data);
				case AlgorithmTypes.Hash:
					throw new NotSupportedException();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		public byte[] CreateSignature(byte[] data)
		{
			switch (_type)
			{
				case AlgorithmTypes.Asymmetric:
					return _asymmetric.CreateSignature(data);
				default:
					throw new NotSupportedException();
			}
		}

		public bool VerifySignature(byte[] data, byte[] signature)
		{
			switch (_type)
			{
				case AlgorithmTypes.Asymmetric:
					return _asymmetric.VerifySignature(data, signature);
				default:
					throw new NotSupportedException();
			}
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

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			throw new NotImplementedException();
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			throw new NotImplementedException();
		}
	}
}