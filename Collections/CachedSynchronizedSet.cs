namespace Ecng.Collections;

using System.Collections.Generic;

/// <summary>
/// Represents a thread-safe set with a cached array of its elements for improved performance.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class CachedSynchronizedSet<T> : SynchronizedSet<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class.
	/// </summary>
	public CachedSynchronizedSet()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class with the specified indexing option.
	/// </summary>
	/// <param name="allowIndexing">A value indicating whether indexing is allowed.</param>
	public CachedSynchronizedSet(bool allowIndexing)
		: base(allowIndexing)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class with the specified collection.
	/// </summary>
	/// <param name="collection">The collection whose elements are copied to the new set.</param>
	public CachedSynchronizedSet(IEnumerable<T> collection)
		: base(collection)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class with the specified equality comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer to use when comparing elements.</param>
	public CachedSynchronizedSet(IEqualityComparer<T> comparer)
		: base(comparer)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class with the specified collection and equality comparer.
	/// </summary>
	/// <param name="collection">The collection whose elements are copied to the new set.</param>
	/// <param name="comparer">The equality comparer to use when comparing elements.</param>
	public CachedSynchronizedSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		: base(collection, comparer)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class with the specified indexing option and equality comparer.
	/// </summary>
	/// <param name="allowIndexing">A value indicating whether indexing is allowed.</param>
	/// <param name="comparer">The equality comparer to use when comparing elements.</param>
	public CachedSynchronizedSet(bool allowIndexing, IEqualityComparer<T> comparer)
		: base(allowIndexing, comparer)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedSet{T}"/> class with the specified indexing option, collection, and equality comparer.
	/// </summary>
	/// <param name="allowIndexing">A value indicating whether indexing is allowed.</param>
	/// <param name="collection">The collection whose elements are copied to the new set.</param>
	/// <param name="comparer">The equality comparer to use when comparing elements.</param>
	public CachedSynchronizedSet(bool allowIndexing, IEnumerable<T> collection, IEqualityComparer<T> comparer)
		: base(allowIndexing, collection, comparer)
	{
	}

	private T[] _cache;

	/// <summary>
	/// Gets a cached array of the elements in the set.
	/// </summary>
	/// <remarks>
	/// The array is cached for performance and is reset when the set is modified.
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
	/// Called when the set is modified to reset the cached array.
	/// </summary>
	protected override void OnChanged()
	{
		_cache = null;

		base.OnChanged();
	}
}