namespace Ecng.Interop;

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 3 bytes.
/// </summary>
public unsafe struct Utf8String3
{
	private const int _size = 3;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String3 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String3 instance.</param>
	public static implicit operator string(Utf8String3 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String3.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String3(string value)
	{
		var str = new Utf8String3();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 4 bytes.
/// </summary>
public unsafe struct Utf8String4
{
	private const int _size = 4;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String4 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String4 instance.</param>
	public static implicit operator string(Utf8String4 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String4.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String4(string value)
	{
		var str = new Utf8String4();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 6 bytes.
/// </summary>
public unsafe struct Utf8String6
{
	private const int _size = 6;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String6 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String6 instance.</param>
	public static implicit operator string(Utf8String6 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String6.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String6(string value)
	{
		var str = new Utf8String6();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 7 bytes.
/// </summary>
public unsafe struct Utf8String7
{
	private const int _size = 7;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String7 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String7 instance.</param>
	public static implicit operator string(Utf8String7 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String7.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String7(string value)
	{
		var str = new Utf8String7();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 12 bytes.
/// </summary>
public unsafe struct Utf8String12
{
	private const int _size = 12;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String12 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String12 instance.</param>
	public static implicit operator string(Utf8String12 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String12.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String12(string value)
	{
		var str = new Utf8String12();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 20 bytes.
/// </summary>
public unsafe struct Utf8String20
{
	private const int _size = 20;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String20 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String20 instance.</param>
	public static implicit operator string(Utf8String20 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String20.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String20(string value)
	{
		var str = new Utf8String20();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 25 bytes.
/// </summary>
public unsafe struct Utf8String25
{
	private const int _size = 25;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String25 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String25 instance.</param>
	public static implicit operator string(Utf8String25 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String25.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String25(string value)
	{
		var str = new Utf8String25();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 31 bytes.
/// </summary>
public unsafe struct Utf8String31
{
	private const int _size = 31;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String31 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String31 instance.</param>
	public static implicit operator string(Utf8String31 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String31.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String31(string value)
	{
		var str = new Utf8String31();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 256 bytes.
/// </summary>
public unsafe struct Utf8String256
{
	private const int _size = 256;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String256 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String256 instance.</param>
	public static implicit operator string(Utf8String256 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String256.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String256(string value)
	{
		var str = new Utf8String256();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}