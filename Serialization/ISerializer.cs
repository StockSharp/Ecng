namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ISerializer
	{
		Type Type { get; }
		Serializer<T> GetSerializer<T>();
		ISerializer GetSerializer(Type entityType);
		string FileExtension { get; }

		public abstract Task SerializeAsync(object graph, Stream stream, CancellationToken cancellationToken);
		public abstract Task<object> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
	}

	public interface ISerializer<T> : ISerializer
	{
		public abstract Task SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);
		public new abstract Task<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
	}
}