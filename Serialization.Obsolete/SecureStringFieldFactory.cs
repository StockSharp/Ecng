namespace Ecng.Serialization
{
	using System.Security;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	public class SecureStringFieldFactory : FieldFactory<SecureString, byte[]>
	{
		public SecureStringFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		private static SecureStringEncryptor Encryptor => SecureStringEncryptor.Instance;

		protected internal override Task<SecureString> OnCreateInstance(ISerializer serializer, byte[] source, CancellationToken cancellationToken)
			=> Encryptor.Decrypt(source).FromResult();

		protected internal override Task<byte[]> OnCreateSource(ISerializer serializer, SecureString instance, CancellationToken cancellationToken)
			=> Encryptor.Encrypt(instance).FromResult();
	}

	class SecureStringEntityFactory : PrimitiveEntityFactory<SecureString>
	{
		public SecureStringEntityFactory(string name)
			: base(name)
		{
		}

		public override Task<SecureString> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
			=> SecureStringEncryptor.Instance.Decrypt((byte[])source[Name].Value).FromResult();
	}
}