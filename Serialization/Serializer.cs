namespace Ecng.Serialization
{
	using System;
	using System.IO;

	using Ecng.Common;

	public abstract class Serializer<T> : ISerializer<T>
	{
		public abstract string FileExtension { get; }
		string ISerializer.FileExtension => FileExtension;

		Type ISerializer.Type => typeof(T);

		public Serializer<TData> GetSerializer<TData>()
			=> (Serializer<TData>)GetSerializer(typeof(TData));

		public virtual ISerializer GetSerializer(Type entityType)
			=> GetType().GetGenericTypeDefinition().Make(entityType).CreateInstance<ISerializer>();

		byte[] ISerializer.Serialize(object graph)
			=> Serialize((T)graph);

		object ISerializer.Deserialize(byte[] data)
			=> Deserialize(data);

		void ISerializer.Serialize(object graph, string fileName)
			=> Serialize((T)graph, fileName);

		object ISerializer.Deserialize(string fileName)
			=> Deserialize(fileName);

		void ISerializer.Serialize(object graph, Stream stream)
			=> Serialize((T)graph, stream);

		object ISerializer.Deserialize(Stream stream)
			=> Deserialize(stream);

		public void Serialize(T graph, string fileName)
			=> File.WriteAllBytes(fileName, Serialize(graph));

		public byte[] Serialize(T graph)
		{
			var stream = new MemoryStream();
			Serialize(graph, stream);
			return stream.To<byte[]>();
		}

		public abstract void Serialize(T graph, Stream stream);

		public T Deserialize(string fileName)
		{
			using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			return Deserialize(stream);
		}

		public T Deserialize(byte[] data)
		{
			var stream = new MemoryStream(data);
			return Deserialize(stream);
		}

		public abstract T Deserialize(Stream stream);
	}
}