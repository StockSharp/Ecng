namespace Ecng.Serialization
{
	public interface ISerializable
	{
		void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source);
		void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source);
	}
}