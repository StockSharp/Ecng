namespace Ecng.Serialization
{
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	public abstract class Serializer<T> : ISerializer<T>
	{
		public abstract string FileExtension { get; }

		public abstract ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);

		public abstract ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);

		ValueTask ISerializer.SerializeAsync(object graph, Stream stream, CancellationToken cancellationToken)
			=> SerializeAsync((T)graph, stream, cancellationToken);

		async ValueTask<object> ISerializer.DeserializeAsync(Stream stream, CancellationToken cancellationToken)
			=> await DeserializeAsync(stream, cancellationToken);
	}
}