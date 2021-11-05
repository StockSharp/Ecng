namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.IO;

	public interface ILegacySerializer : ISerializer
	{
		Schema Schema { get; }

		//bool AllowNullableItems { get; set; }
		IList<string> IgnoreFields { get; }

		object CreateObject(SerializationItemCollection source);

		void Serialize(object graph, FieldList fields, Stream stream);

		void Serialize(object graph, SerializationItemCollection source);
		object Deserialize(SerializationItemCollection source);

		void Serialize(object graph, FieldList fields, SerializationItemCollection source);
		object Deserialize(SerializationItemCollection source, FieldList fields, object graph);

		void Serialize(SerializationItemCollection source, Stream stream);
		void Deserialize(Stream stream, SerializationItemCollection source);

		void Deserialize(Stream stream, FieldList fields, SerializationItemCollection source);

		object GetId(object graph);
	}
}