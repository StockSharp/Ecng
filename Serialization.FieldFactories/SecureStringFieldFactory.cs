namespace Ecng.Serialization
{
	using System;
	using System.Security;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Security;

	public class SecureStringFieldFactory : FieldFactory<SecureString, byte[]>
	{
		static string Key = "RClVEDn0O3EUsKqym1qd";
		static readonly byte[] Salt = "12345678".To<byte[]>();
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
				if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt != true)
				{
					var salt = Entropy ?? Salt;
					if(salt.Length != 16)
						throw new InvalidOperationException("salt/entropy must be 16 bytes");

					try
					{
						source = source.DecryptAes(Key, salt, salt);
					}
					catch (CryptographicException)
					{
						try
						{
							source = _cryptographer.Decrypt(source, Entropy);
						}
						catch (PlatformNotSupportedException)
						{
							return null;
						}
					}
				}

				return source.To<string>().Secure();
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
			var plainText = instance.UnSecure().To<byte[]>();

			if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt == true)
				return plainText;

			var salt = Entropy ?? Salt;
			if(salt.Length != 16)
				throw new InvalidOperationException("salt/entropy must be 16 bytes");

			return plainText.EncryptAes(Key, salt, salt);
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