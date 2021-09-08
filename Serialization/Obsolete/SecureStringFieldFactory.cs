namespace Ecng.Serialization
{
	using System.Security;

	public class SecureStringFieldFactory : FieldFactory<SecureString, byte[]>
	{
		public SecureStringFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		private static SecureStringEncryptor Encryptor => SecureStringEncryptor.Instance;

		protected internal override SecureString OnCreateInstance(ISerializer serializer, byte[] source)
			=> Encryptor.Decrypt(source);

		protected internal override byte[] OnCreateSource(ISerializer serializer, SecureString instance)
			=> Encryptor.Encrypt(instance);
	}

	class SecureStringEntityFactory : PrimitiveEntityFactory<SecureString>
	{
		public SecureStringEntityFactory(string name)
			: base(name)
		{
		}

		public override SecureString CreateEntity(ISerializer serializer, SerializationItemCollection source)
			=> SecureStringEncryptor.Instance.Decrypt((byte[])source[Name].Value);
	}
}