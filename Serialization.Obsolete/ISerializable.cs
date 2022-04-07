namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface ISerializable
	{
		ValueTask Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);
		ValueTask Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);
	}
}