namespace Ecng.Markdown;

using System;
using System.Globalization;

using Ecng.Common;

/// <summary>
/// Counts a headline figure up from nothing, the way a product page does when it scrolls into view.
/// </summary>
/// <remarks>
/// The arithmetic is shared so both hosts tick through the same values over the same time and land on
/// exactly the text the author wrote. Each host only owns its timer: what a frame should say is decided
/// here, and neither host is free to round it differently.
/// </remarks>
public sealed class CounterTween
{
	private readonly string _final;
	private readonly double _target;
	private readonly string _prefix;
	private readonly string _suffix;
	private readonly int _decimals;

	/// <summary>
	/// How long the count takes.
	/// </summary>
	public static TimeSpan Duration { get; } = TimeSpan.FromMilliseconds(900);

	/// <summary>
	/// Whether this figure is a number and so can be counted up at all.
	/// </summary>
	/// <remarks>
	/// "Free" and "C# and Python" are figures too, and animating them would mean inventing intermediate
	/// values for text that has none.
	/// </remarks>
	public bool CanTick { get; }

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="value">The figure exactly as the author wrote it.</param>
	public CounterTween(string value)
	{
		_final = value ?? string.Empty;

		var start = 0;
		var end = _final.Length;

		while (start < end && !char.IsDigit(_final[start]))
			start++;

		while (end > start && !char.IsDigit(_final[end - 1]))
			end--;

		if (start >= end)
			return;

		var number = _final[start..end];

		if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _target))
			return;

		_prefix = _final[..start];
		_suffix = _final[end..];

		var point = number.IndexOf('.');
		_decimals = point < 0 ? 0 : number.Length - point - 1;

		CanTick = true;
	}

	/// <summary>
	/// The text to show at a point in the count.
	/// </summary>
	/// <param name="progress">How far the count has got, from 0 to 1.</param>
	/// <returns>Text for this frame.</returns>
	public string this[double progress]
	{
		get
		{
			if (!CanTick || progress >= 1)
				return _final;

			// Eased so the figure races ahead and settles, rather than crawling up at a constant rate - the
			// difference between a number that lands and one that merely stops.
			var eased = 1 - Math.Pow(1 - progress.Max(0), 3);

			return _prefix + (_target * eased).Round(_decimals).ToString($"F{_decimals}", CultureInfo.InvariantCulture) + _suffix;
		}
	}
}
