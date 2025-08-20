namespace Ecng.ComponentModel;

using System;

using Ecng.Common;

/// <summary>
/// <see cref="Price"/> extension methods.
/// </summary>
public static class PriceExtensions
{
	/// <summary>
	/// Convert string to <see cref="Price"/>.
	/// </summary>
	/// <param name="str">String value of <see cref="Price"/>.</param>
	/// <returns>Object <see cref="Price"/>.</returns>
	public static Price ToPriceType(this string str)
	{
		if (str.IsEmpty())
			throw new ArgumentNullException(nameof(str));

		Price price = new();

		var last = str[str.Length - 1];
		
		if (!last.IsDigit())
		{
			last = last.ToLower();
			str = str.Substring(0, str.Length - 1);

			price.Type = last switch
			{
				Price.PercentChar => PriceTypes.Percent,
				Price.LimitChar => PriceTypes.Limit,
				_ => throw new ArgumentOutOfRangeException(nameof(str), $"Unknown unit of measurement '{last}'."),
			};
		}

		price.Value = str.To<decimal>();

		return price;
	}

	/// <summary>
	/// Convert the <see cref="int"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <returns>Percents.</returns>
	public static Price Percents(this int value) => Percents((decimal)value);

	/// <summary>
	/// Convert the <see cref="double"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <returns>Percents.</returns>
	public static Price Percents(this double value) => Percents((decimal)value);

	/// <summary>
	/// Convert the <see cref="decimal"/> to percents.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <returns>Percents.</returns>
	public static Price Percents(this decimal value) => new(value, PriceTypes.Percent);

	/// <summary>
	/// Determine whether the specified <see cref="Price"/> is free (zero value).
	/// </summary>
	/// <param name="price"><see cref="Price"/></param>
	/// <returns>Returns <see langword="true"/> if the specified <see cref="Price"/> is free (zero value); otherwise, <see langword="false"/>.</returns>
	public static bool IsFree(this Price price) => price == default;
}