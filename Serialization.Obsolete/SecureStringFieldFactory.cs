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

		protected internal override ValueTask<SecureString> OnCreateInstance(ISerializer serializer, byte[] source, CancellationToken cancellationToken)
			=> new(Encryptor.Decrypt(source));

		protected internal override ValueTask<byte[]> OnCreateSource(ISerializer serializer, SecureString instance, CancellationToken cancellationToken)
			=> new(Encryptor.Encrypt(instance));
	}

	class SecureStringEntityFactory : PrimitiveEntityFactory<SecureString>
	{
		public SecureStringEntityFactory(string name)
			: base(name)
		{
		}

		public override ValueTask<SecureString> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
			=> new(SecureStringEncryptor.Instance.Decrypt((byte[])source[Name].Value));
	}
}