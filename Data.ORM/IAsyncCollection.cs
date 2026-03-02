namespace Ecng.Serialization;

public interface IAsyncCollection<T> : IAsyncEnumerable<T>
{
	ValueTask<T> AddAsync(T item, CancellationToken cancellationToken);
	ValueTask ClearAsync(CancellationToken cancellationToken);
	ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken);
	ValueTask CopyToAsync(T[] array, int index, CancellationToken cancellationToken);
	ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken);

	ValueTask<int> CountAsync(CancellationToken cancellationToken);
}