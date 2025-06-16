namespace Ecng.Collections;

/// <summary>
/// Represents a thread-safe list with a cached array of its elements for improved performance.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class CachedSynchronizedList<T> : SynchronizedList<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedList{T}"/> class.
	/// </summary>
	public CachedSynchronizedList()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedList{T}"/> class with the specified initial capacity.
	/// </summary>
	/// <param name="capacity">The initial number of elements that the list can contain.</param>
	public CachedSynchronizedList(int capacity)
		: base(capacity)
	{
	}

	private T[] _cache;

	/// <summary>
	/// Gets a cached array of the elements in the list.
	/// </summary>
	/// <remarks>
	/// The array is cached for performance and is reset when the list is modified.
	/// </remarks>
	public T[] Cache
	{
		get
		{
			lock (SyncRoot)
				return _cache ??= [.. this];
		}
	}

	/// <summary>
	/// Called when the list is modified to reset the cached array.
	/// </summary>
	protected override void OnChanged()
	{
		_cache = null;

		base.OnChanged();
	}
}