namespace Ecng.Serialization;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides an abstract base class for type-specific serializers.
/// </summary>
/// <typeparam name="T">The type of the graph to serialize and deserialize.</typeparam>
public abstract class Serializer<T> : ISerializer<T>
{
	/// <summary>
	/// Gets the file extension associated with this serializer.
	/// </summary>
	public abstract string FileExtension { get; }

	/// <summary>
	/// Serializes the specified object graph to the provided stream asynchronously.
	/// </summary>
	/// <param name="graph">The object graph to serialize.</param>
	/// <param name="stream">The stream to which the graph is serialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the serialization operation.</param>
	/// <returns>A task representing the asynchronous serialize operation.</returns>
	public abstract ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Deserializes an object graph from the provided stream asynchronously.
	/// </summary>
	/// <param name="stream">The stream from which the object graph is deserialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the deserialization operation.</param>
	/// <returns>A task that represents the asynchronous deserialize operation. The task result contains the deserialized object graph.</returns>
	public abstract ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Non-generic implementation of the serialize method.
	/// </summary>
	/// <param name="graph">The object graph to serialize.</param>
	/// <param name="stream">The stream to which the graph is serialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the serialization operation.</param>
	/// <returns>A task representing the asynchronous serialize operation.</returns>
	ValueTask ISerializer.SerializeAsync(object graph, Stream stream, CancellationToken cancellationToken)
		=> SerializeAsync((T)graph, stream, cancellationToken);

	/// <summary>
	/// Non-generic implementation of the deserialize method.
	/// </summary>
	/// <param name="stream">The stream from which the object graph is deserialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the deserialization operation.</param>
	/// <returns>A task that represents the asynchronous deserialize operation. The task result contains the deserialized object graph.</returns>
	async ValueTask<object> ISerializer.DeserializeAsync(Stream stream, CancellationToken cancellationToken)
		=> await DeserializeAsync(stream, cancellationToken);
}