namespace Ecng.Serialization
{
	using System;
	using System.IO;

	public interface ISerializer
	{
		Type Type { get; }
		Serializer<T> GetSerializer<T>();
		ISerializer GetSerializer(Type entityType);
		string FileExtension { get; }

		byte[] Serialize(object graph);
		object Deserialize(byte[] data);

		void Serialize(object graph, string fileName);
		object Deserialize(string fileName);

		void Serialize(object graph, Stream stream);
		object Deserialize(Stream stream);
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