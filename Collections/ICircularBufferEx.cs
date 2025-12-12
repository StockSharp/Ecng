namespace Ecng.Collections;

using System;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Specifies which statistics to calculate in circular buffer with stats.
/// </summary>
[Flags]
public enum CircularBufferStats
{
	/// <summary>
	/// No statistics calculated.
	/// </summary>
	None = 0,

	/// <summary>
	/// Calculate sum of elements.
	/// </summary>
	Sum = 1,

	/// <summary>
	/// Calculate minimum value.
	/// </summary>
	Min = 2,

	/// <summary>
	/// Calculate maximum value.
	/// </summary>
	Max = 4,

	/// <summary>
	/// Calculate all statistics.
	/// </summary>
	All = Sum | Min | Max
}

/// <summary>
/// Common interface for statistic circular buffers.
/// </summary>
/// <typeparam name="TItem">The type of elements in the buffer.</typeparam>
public interface ICircularBufferEx<TItem> : IList<TItem>
{
	/// <summary>
	/// Specifies which statistics to calculate.
	/// </summary>
	CircularBufferStats Stats { get; set; }

	/// <summary>
	/// Max value.
	/// </summary>
	NullableEx<TItem> Max { get; }

	/// <summary>
	/// Min value.
	/// </summary>
	NullableEx<TItem> Min { get; }

	/// <summary>
	/// Sum of all elements in buffer.
	/// </summary>
	TItem Sum { get; }

	/// <summary>
	/// Sum of all elements in buffer without the first element.
	/// </summary>
	TItem SumNoFirst { get; }
}