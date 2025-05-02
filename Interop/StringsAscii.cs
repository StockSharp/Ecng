namespace Ecng.Interop;

/// <summary>
/// Represents an ASCII string of length 1.
/// </summary>
public unsafe struct AsciiString1
{
	private const int _size = 1;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString1"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString1 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString1"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString1"/> with the ascii representation.</returns>
	public static explicit operator AsciiString1(string value)
	{
		var str = new AsciiString1();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 4.
/// </summary>
public unsafe struct AsciiString4
{
	private const int _size = 4;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString4"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString4 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString4"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString4"/> with the ascii representation.</returns>
	public static explicit operator AsciiString4(string value)
	{
		var str = new AsciiString4();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 7.
/// </summary>
public unsafe struct AsciiString7
{
	private const int _size = 7;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString7"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString7 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString7"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString7"/> with the ascii representation.</returns>
	public static explicit operator AsciiString7(string value)
	{
		var str = new AsciiString7();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 8.
/// </summary>
public unsafe struct AsciiString8
{
	private const int _size = 8;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString8"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString8 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString8"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString8"/> with the ascii representation.</returns>
	public static explicit operator AsciiString8(string value)
	{
		var str = new AsciiString8();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 12.
/// </summary>
public unsafe struct AsciiString12
{
	private const int _size = 12;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString12"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString12 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString12"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString12"/> with the ascii representation.</returns>
	public static explicit operator AsciiString12(string value)
	{
		var str = new AsciiString12();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 16.
/// </summary>
public unsafe struct AsciiString16
{
	private const int _size = 16;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString16"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString16 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString16"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString16"/> with the ascii representation.</returns>
	public static explicit operator AsciiString16(string value)
	{
		var str = new AsciiString16();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 20.
/// </summary>
public unsafe struct AsciiString20
{
	private const int _size = 20;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString20"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString20 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString20"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString20"/> with the ascii representation.</returns>
	public static explicit operator AsciiString20(string value)
	{
		var str = new AsciiString20();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 24.
/// </summary>
public unsafe struct AsciiString24
{
	private const int _size = 24;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString24"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString24 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString24"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString24"/> with the ascii representation.</returns>
	public static explicit operator AsciiString24(string value)
	{
		var str = new AsciiString24();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 32.
/// </summary>
public unsafe struct AsciiString32
{
	private const int _size = 32;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString32"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString32 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString32"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString32"/> with the ascii representation.</returns>
	public static explicit operator AsciiString32(string value)
	{
		var str = new AsciiString32();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 64.
/// </summary>
public unsafe struct AsciiString64
{
	private const int _size = 64;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString64"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString64 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString64"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString64"/> with the ascii representation.</returns>
	public static explicit operator AsciiString64(string value)
	{
		var str = new AsciiString64();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 128.
/// </summary>
public unsafe struct AsciiString128
{
	private const int _size = 128;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString128"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString128 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString128"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString128"/> with the ascii representation.</returns>
	public static explicit operator AsciiString128(string value)
	{
		var str = new AsciiString128();
		return str.ToAscii(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}