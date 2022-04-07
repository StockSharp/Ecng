namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ILegacySerializer : ISerializer
	{
		Schema Schema { get; }

		//bool AllowNullableItems { get; set; }
		IList<string> IgnoreFields { get; }

		ValueTask<object> CreateObject(SerializationItemCollection source, CancellationToken cancellationToken);

		ValueTask Serialize(object graph, FieldList fields, Stream stream, CancellationToken cancellationToken);

		ValueTask Serialize(object graph, SerializationItemCollection source, CancellationToken cancellationToken);
		ValueTask<object> Deserialize(SerializationItemCollection source, CancellationToken cancellationToken);

		ValueTask Serialize(object graph, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);
		ValueTask<object> Deserialize(SerializationItemCollection source, FieldList fields, object graph, CancellationToken cancellationToken);

		ValueTask Serialize(SerializationItemCollection source, Stream stream, CancellationToken cancellationToken);
		ValueTask Deserialize(Stream stream, SerializationItemCollection source, CancellationToken cancellationToken);

		ValueTask Deserialize(Stream stream, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);

		object GetId(object graph);
	}
}