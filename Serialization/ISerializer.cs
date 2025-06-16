namespace Ecng.Serialization;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides methods for serializing and deserializing objects.
/// </summary>
public interface ISerializer
{
	/// <summary>
	/// Gets the file extension associated with the serialized format.
	/// </summary>
	string FileExtension { get; }

	/// <summary>
	/// Asynchronously serializes the specified object graph into the provided stream.
	/// </summary>
	/// <param name="graph">The object graph to serialize.</param>
	/// <param name="stream">The stream to which the object is serialized.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	public abstract ValueTask SerializeAsync(object graph, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously deserializes an object graph from the provided stream.
	/// </summary>
	/// <param name="stream">The stream from which the object is deserialized.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation and the deserialized object.</returns>
	public abstract ValueTask<object> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
}

/// <summary>
/// Provides methods for serializing and deserializing objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the object graph to serialize and deserialize.</typeparam>
public interface ISerializer<T> : ISerializer
{
	/// <summary>
	/// Asynchronously serializes the specified object of type <typeparamref name="T"/> into the provided stream.
	/// </summary>
	/// <param name="graph">The object to serialize.</param>
	/// <param name="stream">The stream to which the object is serialized.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
	public abstract ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously deserializes an object of type <typeparamref name="T"/> from the provided stream.
	/// </summary>
	/// <param name="stream">The stream from which the object is deserialized.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation and the deserialized object.</returns>
	public new abstract ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
}