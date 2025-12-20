namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Base class for custom observable collections.
/// </summary>
public abstract class BaseObservableCollection
{
	private int _maxCount = -1;

	/// <summary>
	/// Max number of elements before collection will auto trim itself. -1 to disable.
	/// </summary>
	public int MaxCount
	{
		get => _maxCount;
		set
		{
			if (value < -1 || value == 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be -1 or positive.");

			_maxCount = value;
		}
	}

	/// <summary>
	/// Gets the number of elements in the collection.
	/// </summary>
	public abstract int Count { get; }

	/// <summary>
	/// Removes a range of elements from the collection.
	/// </summary>
	/// <param name="index">Starting index.</param>
	/// <param name="count">Number of elements to remove.</param>
	/// <returns>Number of elements removed.</returns>
	public abstract int RemoveRange(int index, int count);

	/// <summary>
	/// Check current count and trim if necessary.
	/// </summary>
	protected void CheckCount()
	{
		if (MaxCount == -1)
			return;

		if(Count > 1.5 * MaxCount)
			RemoveRange(0, Count - MaxCount);
	}
}