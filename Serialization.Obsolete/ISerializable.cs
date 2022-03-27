namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface ISerializable
	{
		Task Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);
		Task Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);
	}
}