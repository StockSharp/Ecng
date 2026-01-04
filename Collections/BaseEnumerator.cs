namespace Ecng.Collections;

using System;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Represents an abstract base class for an enumerator over a generic enumerable source.
/// </summary>
/// <typeparam name="TEnumerable">The type of the enumerable source, which must implement <see cref="IEnumerable{TItem}"/>.</typeparam>
/// <typeparam name="TItem">The type of elements in the enumerable source.</typeparam>
[Obsolete("Use yield return instead.")]
public abstract class BaseEnumerator<TEnumerable, TItem> : SimpleEnumerator<TItem>
	where TEnumerable : IEnumerable<TItem>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseEnumerator{TEnumerable, TItem}"/> class with the specified source.
	/// </summary>
	/// <param name="source">The enumerable source to iterate over.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is null.</exception>
	protected BaseEnumerator(TEnumerable source)
	{
		if (source.IsNull())
			throw new ArgumentException(nameof(source));

		Source = source;
		Reset();
	}

	/// <summary>
	/// Gets or sets the enumerable source being iterated over.
	/// </summary>
	public TEnumerable Source { get; private set; }

	/// <summary>
	/// Disposes of managed resources by resetting the enumerator and clearing the source.
	/// </summary>
	protected override void DisposeManaged()
	{
		Reset();
		Source = default;
	}

	/// <summary>
	/// Advances the enumerator to the next element of the collection.
	/// </summary>
	/// <returns>True if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
	/// <exception cref="ObjectDisposedException">Thrown if the enumerator has been disposed.</exception>
	public override bool MoveNext()
	{
		ThrowIfDisposed();

		var canProcess = true;
		Current = ProcessMove(ref canProcess);
		return canProcess;
	}

	/// <summary>
	/// Sets the enumerator to its initial position, which is before the first element in the collection.
	/// </summary>
	public override void Reset()
	{
		Current = default;
	}

	/// <summary>
	/// Processes the movement to the next element and determines whether iteration can continue.
	/// </summary>
	/// <param name="canProcess">A reference parameter indicating whether the enumerator can proceed; set to false if the end is reached.</param>
	/// <returns>The next item in the collection, or the default value if no further items are available.</returns>
	protected abstract TItem ProcessMove(ref bool canProcess);
}