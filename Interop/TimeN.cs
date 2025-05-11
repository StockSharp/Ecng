namespace Ecng.Interop;

using System;

using Ecng.Common;

/// <summary>
/// Represents a 4-second resolution time value.
/// </summary>
public struct Time4Sec : IFormattable
{
	/// <summary>
	/// The underlying value representing seconds since Unix epoch divided by 4.
	/// </summary>
	public uint Value;

	/// <summary>
	/// Converts the <see cref="Time4Sec"/> value to a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="value">The <see cref="Time4Sec"/> value.</param>
	public static implicit operator DateTime(Time4Sec value)
		=> ((long)value.Value).FromUnix();

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="Time4Sec"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value.</param>
	public static explicit operator Time4Sec(DateTime value)
		=> new() { Value = (uint)value.ToUnix() };

	/// <summary>
	/// Converts the <see cref="Time4Sec"/> value to a <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <param name="value">The <see cref="Time4Sec"/> value.</param>
	public static implicit operator DateTimeOffset(Time4Sec value)
		=> (DateTime)value;

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to a <see cref="Time4Sec"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
	public static explicit operator Time4Sec(DateTimeOffset value)
		=> (Time4Sec)value.UtcDateTime;

	/// <inheritdoc/>
	public override readonly string ToString()
		=> ((DateTime)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((DateTime)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents an 8-millisecond resolution time value.
/// </summary>
public struct Time8Mls : IFormattable
{
	/// <summary>
	/// The underlying value representing milliseconds since Unix epoch.
	/// </summary>
	public ulong Value;

	/// <summary>
	/// Converts the <see cref="Time8Mls"/> value to a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="value">The <see cref="Time8Mls"/> value.</param>
	public static implicit operator DateTime(Time8Mls value)
		=> ((long)value.Value).FromUnix(false);

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="Time8Mls"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value.</param>
	public static explicit operator Time8Mls(DateTime value)
		=> new() { Value = (ulong)value.ToUnix(false) };

	/// <summary>
	/// Converts the <see cref="Time8Mls"/> value to a <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <param name="value">The <see cref="Time8Mls"/> value.</param>
	public static implicit operator DateTimeOffset(Time8Mls value)
		=> (DateTime)value;

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to a <see cref="Time8Mls"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
	public static explicit operator Time8Mls(DateTimeOffset value)
		=> (Time8Mls)value.UtcDateTime;

	/// <inheritdoc/>
	public override readonly string ToString()
		=> ((DateTime)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((DateTime)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents an 8-microsecond resolution time value.
/// </summary>
public struct Time8Mcs : IFormattable
{
	/// <summary>
	/// The underlying value representing microseconds since Unix epoch.
	/// </summary>
	public ulong Value;

	/// <summary>
	/// Converts the <see cref="Time8Mcs"/> value to a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="value">The <see cref="Time8Mcs"/> value.</param>
	public static implicit operator DateTime(Time8Mcs value)
		=> ((long)value.Value).FromUnixMcs();

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="Time8Mcs"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value.</param>
	public static explicit operator Time8Mcs(DateTime value)
		=> new() { Value = (ulong)value.ToUnixMcs() };

	/// <summary>
	/// Converts the <see cref="Time8Mcs"/> value to a <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <param name="value">The <see cref="Time8Mcs"/> value.</param>
	public static implicit operator DateTimeOffset(Time8Mcs value)
		=> (DateTime)value;

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to a <see cref="Time8Mcs"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
	public static explicit operator Time8Mcs(DateTimeOffset value)
		=> (Time8Mcs)value.UtcDateTime;

	/// <inheritdoc/>
	public override readonly string ToString()
		=> ((DateTime)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((DateTime)this).ToString(format, formatProvider);
}

/// <summary>
/// Represents a nanosecond resolution time value.
/// </summary>
public struct TimeNano : IFormattable
{
	/// <summary>
	/// The underlying value representing nanoseconds since Gregorian start.
	/// </summary>
	public ulong Value;

	/// <summary>
	/// Converts the <see cref="TimeNano"/> value to a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="value">The <see cref="TimeNano"/> value.</param>
	public static implicit operator DateTime(TimeNano value)
		=> TimeHelper.GregorianStart.AddNanoseconds((long)value.Value);

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="TimeNano"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value.</param>
	public static explicit operator TimeNano(DateTime value)
		=> new() { Value = (ulong)(value.ToUniversalTime() - TimeHelper.GregorianStart).ToNanoseconds() };

	/// <summary>
	/// Converts the <see cref="TimeNano"/> value to a <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <param name="value">The <see cref="TimeNano"/> value.</param>
	public static implicit operator DateTimeOffset(TimeNano value)
		=> (DateTime)value;

	/// <summary>
	/// Converts a <see cref="DateTime"/> to a <see cref="TimeNano"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value.</param>
	public static explicit operator TimeNano(DateTimeOffset value)
		=> (TimeNano)value.UtcDateTime;

	/// <inheritdoc/>
	public override readonly string ToString()
		=> ((DateTime)this).ToString();

	/// <inheritdoc/>
	public readonly string ToString(string format, IFormatProvider formatProvider)
		=> ((DateTime)this).ToString(format, formatProvider);
}
