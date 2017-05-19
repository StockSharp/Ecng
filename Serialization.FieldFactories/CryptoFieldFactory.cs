namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using System.Security.Cryptography.X509Certificates;

	using Ecng.Common;
	using Ecng.Security;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	[Serializable]
	public class CryptoFieldFactory : FieldFactory<byte[], byte[]>
	{
		public CryptoFieldFactory(CryptoAlgorithm algorithm, Field field, int order)
			: base(field, order)
		{
			if (algorithm == null)
				throw new ArgumentNullException(nameof(algorithm));

			Algorithm = algorithm;
		}

		public CryptoAlgorithm Algorithm { get; private set; }

		protected override byte[] OnCreateInstance(ISerializer serializer, byte[] source)
		{
			try
			{
				return Algorithm.Decrypt(source);
			}
			catch (CryptographicException ex)
			{
				if (ContinueOnExceptionContext.TryProcess(ex))
					return null;

				throw;
			}
		}

		protected override byte[] OnCreateSource(ISerializer serializer, byte[] instance)
		{
			return Algorithm.Encrypt(instance);
		}

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			serializer.GetSerializer<CryptoAlgorithm>().Serialize(Algorithm, source);
			base.Serialize(serializer, fields, source);
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Algorithm = serializer.GetSerializer<CryptoAlgorithm>().Deserialize(source);
			base.Deserialize(serializer, fields, source);
		}
	}

	public abstract class BaseCryptoAttribute : FieldFactoryAttribute
	{
		public override FieldFactory CreateFactory(Field field)
		{
			return new CryptoFieldFactory(GetAlgorithm(field), field, Order);
		}

		protected abstract CryptoAlgorithm GetAlgorithm(Field field);
	}

	public enum KeyTypes
	{
		Direct,
		File,
	}

	public class CryptoAttribute : BaseCryptoAttribute
	{
		private string _algorithm;

		public string Algorithm
		{
			get
			{
				if (!_algorithm.IsEmpty())
					return _algorithm;
				else
				{
					if (!PublicKey.IsEmpty())
					{
						if (!PrivateKey.IsEmpty())
							return CryptoAlgorithm.DefaultAsymmetricAlgoName;
						else
							return CryptoAlgorithm.DefaultSymmetricAlgoName;
					}
					else
						return CryptoAlgorithm.DefaultDpapiAlgoName;
				}
			}
			set { _algorithm = value; }
		}

		public KeyTypes KeyType { get; set; }
		public string PublicKey { get; set; }
		public string PrivateKey { get; set; }

		protected override CryptoAlgorithm GetAlgorithm(Field field)
		{
			CryptoAlgorithm alg;

			var algType = CryptoAlgorithm.GetAlgo(Algorithm);

			switch (CryptoAlgorithm.GetAlgType(algType))
			{
				case AlgorithmTypes.Symmetric:
					alg = CryptoAlgorithm.Create(algType, GetKey(PublicKey));
					break;
				case AlgorithmTypes.Asymmetric:
					alg = CryptoAlgorithm.Create(algType, GetKey(PublicKey), GetKey(PrivateKey));
					break;
				case AlgorithmTypes.Dpapi:
					alg = CryptoAlgorithm.Create(algType);
					break;
				default:
					throw new InvalidOperationException();
			}

			return alg;
		}

		private ProtectedKey GetKey(string value)
		{
			byte[] plainText;

			switch (KeyType)
			{
				case KeyTypes.Direct:
					plainText = value.Base64();
					break;
				case KeyTypes.File:
					plainText = File.ReadAllBytes(value);
					break;
				default:
					throw new InvalidOperationException();
			}

			return plainText.FromBytes();
		}
	}

	public class X509Attribute : BaseCryptoAttribute
	{
		public X509Attribute()
		{
			StoreLocation = StoreLocation.CurrentUser;
			StoreName = "MY";
		}

		public string StoreName { get; set; }
		public StoreLocation StoreLocation { get; set; }
		public string SerialNumber { get; set; }
		public string FileName { get; set; }

		protected override CryptoAlgorithm GetAlgorithm(Field field)
		{
			var cert = FileName.IsEmpty() ? FindCertificate() : new X509Certificate2(FileName);
			return new CryptoAlgorithm(new X509Cryptographer(cert));
		}

		private X509Certificate2 FindCertificate()
		{
			using (var store = new X509StoreEx(StoreName, StoreLocation, OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly))
			{
				foreach (var cert in store.Certificates)
				{
					if (cert.SerialNumber == SerialNumber)
						return cert;
				}
			}

			throw new InvalidOperationException();
		}
	}
}