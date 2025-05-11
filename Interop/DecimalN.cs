namespace Ecng.Interop;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Represents a decimal value with a fixed exponent of -2.
/// </summary>
public struct Decimal2 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-2).
	/// </summary>
	public const sbyte Exponent = -2;

	/// <summary>
	/// Implicitly converts a Decimal2 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal2 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal2 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal2 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal2 representing the same value.</returns>
	public static explicit operator Decimal2(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -3.
/// </summary>
public struct Decimal3 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-3).
	/// </summary>
	public const sbyte Exponent = -3;

	/// <summary>
	/// Implicitly converts a Decimal3 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal3 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal3 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal3 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal3 representing the same value.</returns>
	public static explicit operator Decimal3(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -4.
/// </summary>
public struct Decimal4 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-4).
	/// </summary>
	public const sbyte Exponent = -4;

	/// <summary>
	/// Implicitly converts a Decimal4 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal4 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal4 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal4 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal4 representing the same value.</returns>
	public static explicit operator Decimal4(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -5.
/// </summary>
public struct Decimal5 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-5).
	/// </summary>
	public const sbyte Exponent = -5;

	/// <summary>
	/// Implicitly converts a Decimal5 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal5 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal5 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal5 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal5 representing the same value.</returns>
	public static explicit operator Decimal5(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -6.
/// </summary>
public struct Decimal6 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-6).
	/// </summary>
	public const sbyte Exponent = -6;

	/// <summary>
	/// Implicitly converts a Decimal6 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal6 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal6 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal6 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal6 representing the same value.</returns>
	public static explicit operator Decimal6(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -7.
/// </summary>
public struct Decimal7 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-7).
	/// </summary>
	public const sbyte Exponent = -7;

	/// <summary>
	/// Implicitly converts a Decimal7 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal7 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal7 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal7 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal7 representing the same value.</returns>
	public static explicit operator Decimal7(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -8.
/// </summary>
public struct Decimal8 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-8).
	/// </summary>
	public const sbyte Exponent = -8;

	/// <summary>
	/// Implicitly converts a Decimal8 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal8 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal8 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal8 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal8 representing the same value.</returns>
	public static explicit operator Decimal8(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}
/// <summary>
/// Represents a decimal value with a fixed exponent of -9.
/// </summary>
public struct Decimal9 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-9).
	/// </summary>
	public const sbyte Exponent = -9;

	/// <summary>
	/// Implicitly converts a Decimal9 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal9 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal9 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal9 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal9 representing the same value.</returns>
	public static explicit operator Decimal9(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -10.
/// </summary>
public struct Decimal10 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-10).
	/// </summary>
	public const sbyte Exponent = -10;

	/// <summary>
	/// Implicitly converts a Decimal10 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal10 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal10 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal10 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal10 representing the same value.</returns>
	public static explicit operator Decimal10(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -11.
/// </summary>
public struct Decimal11 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-11).
	/// </summary>
	public const sbyte Exponent = -11;

	/// <summary>
	/// Implicitly converts a Decimal11 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal11 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal11 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal11 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal11 representing the same value.</returns>
	public static explicit operator Decimal11(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal value with a fixed exponent of -12.
/// </summary>
public struct Decimal12 : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	public long Mantissa;

	/// <summary>
	/// The fixed exponent value (-12).
	/// </summary>
	public const sbyte Exponent = -12;

	/// <summary>
	/// Implicitly converts a Decimal12 value to a decimal.
	/// </summary>
	/// <param name="value">The Decimal12 value.</param>
	/// <returns>A decimal representing the same value.</returns>
	public static implicit operator decimal(Decimal12 value)
		=> value.Mantissa * _priceExponent;

	/// <summary>
	/// Explicitly converts a decimal to a Decimal12 value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A Decimal12 representing the same value.</returns>
	public static explicit operator Decimal12(decimal value) => new()
	{
		Mantissa = (long)(value / _priceExponent),
	};

	/// <summary>
	/// The scaling factor for conversion based on the exponent.
	/// </summary>
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, Exponent);

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a decimal number with a variable exponent.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct DecimalN : IFormattable
{
	/// <summary>
	/// The mantissa component of the decimal number.
	/// </summary>
	[FieldOffset(0)]
	public long Mantissa;

	[FieldOffset(8)]
	private byte _exponent;

	/// <summary>
	/// Gets or sets the exponent used for scaling the decimal value.
	/// The exponent must be between 0 and 8 (inclusive).
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when a value less than 0 or greater than 8 is assigned.
	/// </exception>
	public byte Exponent
	{
		readonly get => _exponent;
		set
		{
			if (value < 0 || value > _defExponent)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

			_exponent = value;
		}
	}

	/// <summary>
	/// Calculates the scaling factor as a decimal based on the exponent.
	/// </summary>
	/// <returns>A decimal scaling factor.</returns>
	private readonly decimal GetFactor() => (decimal)Math.Pow(10, -_exponent);

	/// <summary>
	/// Implicitly converts a DecimalN value to a decimal.
	/// </summary>
	/// <param name="value">The DecimalN value.</param>
	/// <returns>A decimal representing the scaled value.</returns>
	public static implicit operator decimal(DecimalN value)
		=> value.Mantissa * value.GetFactor();

	private const byte _defExponent = 8;
	private static readonly decimal _priceExponent = (decimal)Math.Pow(10, _defExponent);

	/// <summary>
	/// Explicitly converts a decimal to a DecimalN value using the default exponent.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A DecimalN representing the same value.</returns>
	public static explicit operator DecimalN(decimal value)
	{
		return new()
		{
			Mantissa = (long)(value * _priceExponent),
			Exponent = _defExponent
		};
	}

	/// <summary>
	/// Creates a DecimalN from a decimal value and a specified exponent.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="exponent">The exponent to use for scaling.</param>
	/// <returns>A DecimalN representing the scaled value.</returns>
	public static DecimalN FromDecimal(decimal value, byte exponent)
	{
		decimal factor = (decimal)Math.Pow(10, exponent);

		return new()
		{
			Mantissa = (long)(value * factor),
			Exponent = exponent
		};
	}

	/// <inheritdoc/>
	public override readonly string ToString() => ((decimal)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((decimal)this).ToString(format, formatProvider);
}
