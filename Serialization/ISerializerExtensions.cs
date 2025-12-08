namespace Ecng.Serialization;

using System.IO;

using Ecng.Common;

/// <summary>
/// Provides extension methods for the ISerializer and ISerializer&lt;T&gt; interfaces to support serialization
/// and deserialization operations with files, streams, and byte arrays.
/// </summary>
public static class ISerializerExtensions
{
	/// <summary>
	/// Serializes the specified object graph and writes the output to a file.
	/// </summary>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="graph">The object graph to serialize.</param>
	/// <param name="fileName">The path of the file to write to.</param>
	public static void Serialize(this ISerializer serializer, object graph, string fileName)
		=> File.WriteAllBytes(fileName, serializer.CheckOnNull(nameof(serializer)).Serialize(graph));

	/// <summary>
	/// Serializes the specified object graph and returns the result as a byte array.
	/// </summary>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="graph">The object graph to serialize.</param>
	/// <returns>A byte array containing the serialized data.</returns>
	public static byte[] Serialize(this ISerializer serializer, object graph)
	{
		using var stream = new MemoryStream();
		serializer.CheckOnNull(nameof(serializer)).Serialize(graph, stream);
		return stream.To<byte[]>();
	}

	/// <summary>
	/// Serializes the specified object graph and writes the output to the provided stream.
	/// </summary>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="graph">The object graph to serialize.</param>
	/// <param name="stream">The stream to which to write the serialized data.</param>
	public static void Serialize(this ISerializer serializer, object graph, Stream stream)
		=> AsyncHelper.Run(() => serializer.CheckOnNull(nameof(serializer)).SerializeAsync(graph, stream, default));

	/// <summary>
	/// Deserializes the data from the specified file into an object of type T.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize.</typeparam>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="fileName">The path of the file to read from.</param>
	/// <returns>An object of type T.</returns>
	public static T Deserialize<T>(this ISerializer<T> serializer, string fileName)
	{
		using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		return serializer.CheckOnNull(nameof(serializer)).Deserialize(stream);
	}

	/// <summary>
	/// Deserializes the specified byte array into an object of type T.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize.</typeparam>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="data">The byte array containing the serialized data.</param>
	/// <returns>An object of type T.</returns>
	public static T Deserialize<T>(this ISerializer<T> serializer, byte[] data)
	{
		using var stream = new MemoryStream(data);
		return serializer.CheckOnNull(nameof(serializer)).Deserialize(stream);
	}

	/// <summary>
	/// Deserializes the data from the given stream into an object of type T.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize.</typeparam>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="stream">The stream containing the serialized data.</param>
	/// <returns>An object of type T.</returns>
	public static T Deserialize<T>(this ISerializer<T> serializer, Stream stream)
		=> AsyncHelper.Run(() => serializer.CheckOnNull(nameof(serializer)).DeserializeAsync(stream, default));

	/// <summary>
	/// Deserializes the specified byte array into an object.
	/// </summary>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="data">The byte array containing the serialized data.</param>
	/// <returns>The deserialized object.</returns>
	public static object Deserialize(this ISerializer serializer, byte[] data)
	{
		using var stream = new MemoryStream(data);
		return serializer.CheckOnNull(nameof(serializer)).Deserialize(stream);
	}

	/// <summary>
	/// Deserializes the data from the given stream into an object.
	/// </summary>
	/// <param name="serializer">The serializer instance.</param>
	/// <param name="stream">The stream containing the serialized data.</param>
	/// <returns>The deserialized object.</returns>
	public static object Deserialize(this ISerializer serializer, Stream stream)
		=> AsyncHelper.Run(() => serializer.CheckOnNull(nameof(serializer)).DeserializeAsync(stream, default));
}