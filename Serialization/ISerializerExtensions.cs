namespace Ecng.Serialization
{
	using System.IO;

	using Ecng.Common;

	using Nito.AsyncEx;

	public static class ISerializerExtensions
	{
		public static byte[] Serialize<T>(this ISerializer<T> serializer, object graph)
			=> serializer.CheckOnNull(nameof(serializer)).Serialize((T)graph);

		public static void Serialize<T>(this ISerializer<T> serializer, object graph, string fileName)
			=> serializer.CheckOnNull(nameof(serializer)).Serialize((T)graph, fileName);

		public static void Serialize<T>(this ISerializer<T> serializer, object graph, Stream stream)
			=> serializer.CheckOnNull(nameof(serializer)).Serialize((T)graph, stream);

		public static void Serialize<T>(this ISerializer<T> serializer, T graph, string fileName)
			=> File.WriteAllBytes(fileName, serializer.CheckOnNull(nameof(serializer)).Serialize(graph));

		public static byte[] Serialize<T>(this ISerializer<T> serializer, T graph)
		{
			var stream = new MemoryStream();
			serializer.CheckOnNull(nameof(serializer)).Serialize(graph, stream);
			return stream.To<byte[]>();
		}

		public static void Serialize<T>(this ISerializer<T> serializer, T graph, Stream stream)
			=> AsyncContext.Run(() => serializer.CheckOnNull(nameof(serializer)).SerializeAsync(graph, stream, default));

		public static T Deserialize<T>(this ISerializer<T> serializer, string fileName)
		{
			using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			return serializer.CheckOnNull(nameof(serializer)).Deserialize(stream);
		}

		public static T Deserialize<T>(this ISerializer<T> serializer, byte[] data)
		{
			var stream = new MemoryStream(data);
			return serializer.CheckOnNull(nameof(serializer)).Deserialize(stream);
		}

		public static T Deserialize<T>(this ISerializer<T> serializer, Stream stream)
			=> AsyncContext.Run(() => serializer.CheckOnNull(nameof(serializer)).DeserializeAsync(stream, default));


	}
}