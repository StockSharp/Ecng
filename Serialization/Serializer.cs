namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

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

		public abstract ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken);

		public abstract ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);

		ValueTask ISerializer.SerializeAsync(object graph, Stream stream, CancellationToken cancellationToken)
			=> SerializeAsync((T)graph, stream, cancellationToken);

		async ValueTask<object> ISerializer.DeserializeAsync(Stream stream, CancellationToken cancellationToken)
			=> await DeserializeAsync(stream, cancellationToken);
	}
}