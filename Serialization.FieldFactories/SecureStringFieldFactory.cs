namespace Ecng.Serialization
{
	using System;
	using System.Security;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Security;

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

		protected override SecureString OnCreateInstance(ISerializer serializer, byte[] source)
		{
			try
			{
				return _cryptographer.Decrypt(source, Entropy).To<string>().To<SecureString>();
			}
			catch (CryptographicException ex)
			{
				if (ContinueOnExceptionContext.TryProcess(ex))
					return null;
				
				throw;
			}
		}

		protected override byte[] OnCreateSource(ISerializer serializer, SecureString instance)
		{
			var plainText = instance.To<string>().To<byte[]>();
			return _cryptographer.Encrypt(plainText, Entropy);
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