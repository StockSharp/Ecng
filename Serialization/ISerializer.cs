namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	public interface ISerializer
	{
		Type Type { get; }
		Schema Schema { get; }
		Serializer<T> GetSerializer<T>();
		ISerializer GetSerializer(Type entityType);
		//bool AllowNullableItems { get; set; }
		IList<string> IgnoreFields { get; }
		string FileExtension { get; }

		object CreateObject(SerializationItemCollection source);

		byte[] Serialize(object graph);
		object Deserialize(byte[] data);

		void Serialize(object graph, string fileName);
		object Deserialize(string fileName);

		void Serialize(object graph, Stream stream);
		object Deserialize(Stream stream);

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

	public interface ISerializer<T> : ISerializer
	{
		void Serialize(T graph, string fileName);
		new T Deserialize(string fileName);

		void Serialize(T graph, Stream stream);
		new T Deserialize(Stream stream);

		byte[] Serialize(T graph);
		new T Deserialize(byte[] data);
	}
}