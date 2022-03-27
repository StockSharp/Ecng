namespace Ecng.Serialization
{
	using System.Security;
	using System.Threading;
	using System.Threading.Tasks;

	public class SecureStringFieldFactory : FieldFactory<SecureString, byte[]>
	{
		public SecureStringFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		private static SecureStringEncryptor Encryptor => SecureStringEncryptor.Instance;

		protected internal override Task<SecureString> OnCreateInstance(ISerializer serializer, byte[] source, CancellationToken cancellationToken)
			=> Task.FromResult(Encryptor.Decrypt(source));

		protected internal override Task<byte[]> OnCreateSource(ISerializer serializer, SecureString instance, CancellationToken cancellationToken)
			=> Task.FromResult(Encryptor.Encrypt(instance));
	}

	class SecureStringEntityFactory : PrimitiveEntityFactory<SecureString>
	{
		public SecureStringEntityFactory(string name)
			: base(name)
		{
		}

		public override Task<SecureString> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Task.FromResult(SecureStringEncryptor.Instance.Decrypt((byte[])source[Name].Value));
	}
}