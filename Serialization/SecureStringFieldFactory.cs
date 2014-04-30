namespace Ecng.Serialization
{
	using System;
	using System.Security;
	using System.Security.Cryptography;

	using Ecng.Collections;
	using Ecng.Common;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public class SecureStringFieldFactory : FieldFactory<SecureString, byte[]>
	{
		private static readonly DpapiCryptographer _cryptographer;

		static SecureStringFieldFactory()
		{
			_cryptographer = new DpapiCryptographer(DataProtectionScope.CurrentUser);
		}

		public static byte[] Entropy { get; set; }

		public SecureStringFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override SecureString OnCreateInstance(ISerializer serializer, byte[] source)
		{
			return _cryptographer.Decrypt(source.To<byte[]>(), Entropy).To<string>().To<SecureString>();
		}

		protected internal override byte[] OnCreateSource(ISerializer serializer, SecureString instance)
		{
			var plainText = instance.To<string>().To<byte[]>();
			return plainText.IsEmpty() ? ProtectedData.Protect(plainText, Entropy, _cryptographer.StoreScope) : _cryptographer.Encrypt(plainText, Entropy);
		}
	}

	public sealed class SecureStringAttribute : ReflectionFieldFactoryAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(SecureStringFieldFactory);
		}
	}
}