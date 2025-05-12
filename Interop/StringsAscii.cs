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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 2.
/// </summary>
public unsafe struct AsciiString2
{
	private const int _size = 2;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString2"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString2 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString2"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString2"/> with the ascii representation.</returns>
	public static explicit operator AsciiString2(string value)
	{
		var str = new AsciiString2();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 3.
/// </summary>
public unsafe struct AsciiString3
{
	private const int _size = 3;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString3"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString3 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString3"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString3"/> with the ascii representation.</returns>
	public static explicit operator AsciiString3(string value)
	{
		var str = new AsciiString3();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 5.
/// </summary>
public unsafe struct AsciiString5
{
	private const int _size = 5;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString5"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString5 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString5"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString5"/> with the ascii representation.</returns>
	public static explicit operator AsciiString5(string value)
	{
		var str = new AsciiString5();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 6.
/// </summary>
public unsafe struct AsciiString6
{
	private const int _size = 6;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString6"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString6 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString6"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString6"/> with the ascii representation.</returns>
	public static explicit operator AsciiString6(string value)
	{
		var str = new AsciiString6();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 9.
/// </summary>
public unsafe struct AsciiString9
{
	private const int _size = 9;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString9"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString9 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString9"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString9"/> with the ascii representation.</returns>
	public static explicit operator AsciiString9(string value)
	{
		var str = new AsciiString9();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 10.
/// </summary>
public unsafe struct AsciiString10
{
	private const int _size = 10;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString10"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString10 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString10"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString10"/> with the ascii representation.</returns>
	public static explicit operator AsciiString10(string value)
	{
		var str = new AsciiString10();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 11.
/// </summary>
public unsafe struct AsciiString11
{
	private const int _size = 11;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString11"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString11 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString11"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString11"/> with the ascii representation.</returns>
	public static explicit operator AsciiString11(string value)
	{
		var str = new AsciiString11();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 13.
/// </summary>
public unsafe struct AsciiString13
{
	private const int _size = 13;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString13"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString13 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString13"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString13"/> with the ascii representation.</returns>
	public static explicit operator AsciiString13(string value)
	{
		var str = new AsciiString13();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 14.
/// </summary>
public unsafe struct AsciiString14
{
	private const int _size = 14;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString14"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString14 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString14"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString14"/> with the ascii representation.</returns>
	public static explicit operator AsciiString14(string value)
	{
		var str = new AsciiString14();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 15.
/// </summary>
public unsafe struct AsciiString15
{
	private const int _size = 15;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString15"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString15 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString15"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString15"/> with the ascii representation.</returns>
	public static explicit operator AsciiString15(string value)
	{
		var str = new AsciiString15();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 17.
/// </summary>
public unsafe struct AsciiString17
{
	private const int _size = 17;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString17"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString17 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString17"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString17"/> with the ascii representation.</returns>
	public static explicit operator AsciiString17(string value)
	{
		var str = new AsciiString17();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 18.
/// </summary>
public unsafe struct AsciiString18
{
	private const int _size = 18;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString18"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString18 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString18"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString18"/> with the ascii representation.</returns>
	public static explicit operator AsciiString18(string value)
	{
		var str = new AsciiString18();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 19.
/// </summary>
public unsafe struct AsciiString19
{
	private const int _size = 19;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString19"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString19 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString19"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString19"/> with the ascii representation.</returns>
	public static explicit operator AsciiString19(string value)
	{
		var str = new AsciiString19();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 21.
/// </summary>
public unsafe struct AsciiString21
{
	private const int _size = 21;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString21"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString21 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString21"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString21"/> with the ascii representation.</returns>
	public static explicit operator AsciiString21(string value)
	{
		var str = new AsciiString21();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 22.
/// </summary>
public unsafe struct AsciiString22
{
	private const int _size = 22;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString22"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString22 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString22"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString22"/> with the ascii representation.</returns>
	public static explicit operator AsciiString22(string value)
	{
		var str = new AsciiString22();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 23.
/// </summary>
public unsafe struct AsciiString23
{
	private const int _size = 23;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString23"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString23 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString23"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString23"/> with the ascii representation.</returns>
	public static explicit operator AsciiString23(string value)
	{
		var str = new AsciiString23();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 25.
/// </summary>
public unsafe struct AsciiString25
{
	private const int _size = 25;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString25"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString25 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString25"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString25"/> with the ascii representation.</returns>
	public static explicit operator AsciiString25(string value)
	{
		var str = new AsciiString25();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 26.
/// </summary>
public unsafe struct AsciiString26
{
	private const int _size = 26;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString26"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString26 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString26"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString26"/> with the ascii representation.</returns>
	public static explicit operator AsciiString26(string value)
	{
		var str = new AsciiString26();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 27.
/// </summary>
public unsafe struct AsciiString27
{
	private const int _size = 27;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString27"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString27 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString27"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString27"/> with the ascii representation.</returns>
	public static explicit operator AsciiString27(string value)
	{
		var str = new AsciiString27();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 28.
/// </summary>
public unsafe struct AsciiString28
{
	private const int _size = 28;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString28"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString28 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString28"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString28"/> with the ascii representation.</returns>
	public static explicit operator AsciiString28(string value)
	{
		var str = new AsciiString28();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 29.
/// </summary>
public unsafe struct AsciiString29
{
	private const int _size = 29;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString29"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString29 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString29"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString29"/> with the ascii representation.</returns>
	public static explicit operator AsciiString29(string value)
	{
		var str = new AsciiString29();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 30.
/// </summary>
public unsafe struct AsciiString30
{
	private const int _size = 30;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString30"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString30 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString30"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString30"/> with the ascii representation.</returns>
	public static explicit operator AsciiString30(string value)
	{
		var str = new AsciiString30();
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents an ASCII string of length 31.
/// </summary>
public unsafe struct AsciiString31
{
	private const int _size = 31;

	/// <summary>
	/// Fixed array storing ASCII bytes.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Converts the given <see cref="AsciiString31"/> to its string representation.
	/// </summary>
	/// <param name="value">The ASCII string.</param>
	public static implicit operator string(AsciiString31 value)
		=> _size.ToAscii(value.Value);

	/// <summary>
	/// Creates an <see cref="AsciiString31"/> from a string.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>An instance of <see cref="AsciiString31"/> with the ascii representation.</returns>
	public static explicit operator AsciiString31(string value)
	{
		var str = new AsciiString31();
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
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
		value.ToAscii(str.Value, _size);
		return str;
	}

	/// <summary>
	/// Returns the string representation of the ASCII string.
	/// </summary>
	/// <returns>A .NET string containing the ASCII characters.</returns>
	public override readonly string ToString() => (string)this;
}