namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ISerializer
	{
		string FileExtension { get; }

		public abstract ValueTask SerializeAsync(object graph, Stream stream, CancellationToken cancellationToken);
		public abstract ValueTask<object> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
	}

	public interface ISerializer<T> : ISerializer
	{
		public abstract ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);
		public new abstract ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
	}
}