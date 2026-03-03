namespace Ecng.Serialization;

/// <summary>
/// Asynchronous collection interface for database entities.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IAsyncCollection<T> : IAsyncEnumerable<T>
{
	/// <summary>
	/// Asynchronously adds an item to the collection.
	/// </summary>
	ValueTask<T> AddAsync(T item, CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously removes all items from the collection.
	/// </summary>
	ValueTask ClearAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously determines whether the collection contains the specified item.
	/// </summary>
	ValueTask<bool> ContainsAsync(T item, CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously copies the collection elements to an array starting at the specified index.
	/// </summary>
	ValueTask CopyToAsync(T[] array, int index, CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously removes the specified item from the collection.
	/// </summary>
	ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously returns the number of items in the collection.
	/// </summary>
	ValueTask<int> CountAsync(CancellationToken cancellationToken);
}