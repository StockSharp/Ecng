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

		Task<object> CreateObject(SerializationItemCollection source, CancellationToken cancellationToken);

		Task Serialize(object graph, FieldList fields, Stream stream, CancellationToken cancellationToken);

		Task Serialize(object graph, SerializationItemCollection source, CancellationToken cancellationToken);
		Task<object> Deserialize(SerializationItemCollection source, CancellationToken cancellationToken);

		Task Serialize(object graph, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);
		Task<object> Deserialize(SerializationItemCollection source, FieldList fields, object graph, CancellationToken cancellationToken);

		Task Serialize(SerializationItemCollection source, Stream stream, CancellationToken cancellationToken);
		Task Deserialize(Stream stream, SerializationItemCollection source, CancellationToken cancellationToken);

		Task Deserialize(Stream stream, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);

		object GetId(object graph);
	}
}