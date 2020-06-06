namespace Ecng.Serialization
{
	using System;
	using System.Security;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Security;

	public class SecureStringFieldFactory : FieldFactory<SecureString, byte[]>
	{
		private static readonly SecureString _key = "RClVEDn0O3EUsKqym1qd".Secure();
		private static readonly byte[] _salt = "3hj67-!3".To<byte[]>();

		private static readonly DpapiCryptographer _dpapi;

		static SecureStringFieldFactory()
		{
			try
			{
				_dpapi = new DpapiCryptographer(DataProtectionScope.CurrentUser);
			}
			catch
			{
			}
		}

		public static SecureString Key { get; set; }
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
					var key = EnsureGetKey();
					var salt = EnsureGetSalt();

					try
					{
						source = source.DecryptAes(key, salt, salt);
					}
					catch (CryptographicException ex)
					{
						if (_dpapi == null)
							throw;

						try
						{
							source = _dpapi.Decrypt(source, Entropy);
						}
						catch (CryptographicException)
						{
							// throws original error
							throw ex;
						}
						catch (PlatformNotSupportedException)
						{
							// throws original error
							throw ex;
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

			var salt = EnsureGetSalt();
			var key = EnsureGetKey();

			return plainText.EncryptAes(key, salt, salt);
		}

		private static byte[] EnsureGetSalt()
		{
			var salt = Entropy ?? _salt;

			if (salt.Length != 16)
				throw new InvalidOperationException("Entropy must be 16 bytes.");

			return salt;
		}

		private static string EnsureGetKey()
		{
			var key = Key ?? _key;

			if (key.IsEmpty())
				throw new InvalidOperationException("Key not specified.");

			return key.UnSecure();
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