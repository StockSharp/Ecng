namespace Ecng.Interop;

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 1 byte.
/// </summary>
public unsafe struct Utf8String1
{
	private const int _size = 1;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String1 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String1 instance.</param>
	public static implicit operator string(Utf8String1 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String1.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String1(string value)
	{
		var str = new Utf8String1();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 2 bytes.
/// </summary>
public unsafe struct Utf8String2
{
	private const int _size = 2;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String2 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String2 instance.</param>
	public static implicit operator string(Utf8String2 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String2.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String2(string value)
	{
		var str = new Utf8String2();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

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
/// Represents a UTF-8 encoded string with a fixed size of 5 bytes.
/// </summary>
public unsafe struct Utf8String5
{
	private const int _size = 5;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String5 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String5 instance.</param>
	public static implicit operator string(Utf8String5 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String5.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String5(string value)
	{
		var str = new Utf8String5();
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
/// Represents a UTF-8 encoded string with a fixed size of 8 bytes.
/// </summary>
public unsafe struct Utf8String8
{
	private const int _size = 8;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String8 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String8 instance.</param>
	public static implicit operator string(Utf8String8 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String8.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String8(string value)
	{
		var str = new Utf8String8();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 9 bytes.
/// </summary>
public unsafe struct Utf8String9
{
	private const int _size = 9;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String9 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String9 instance.</param>
	public static implicit operator string(Utf8String9 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String9.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String9(string value)
	{
		var str = new Utf8String9();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 10 bytes.
/// </summary>
public unsafe struct Utf8String10
{
	private const int _size = 10;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String10 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String10 instance.</param>
	public static implicit operator string(Utf8String10 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String10.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String10(string value)
	{
		var str = new Utf8String10();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 11 bytes.
/// </summary>
public unsafe struct Utf8String11
{
	private const int _size = 11;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String11 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String11 instance.</param>
	public static implicit operator string(Utf8String11 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String11.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String11(string value)
	{
		var str = new Utf8String11();
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
/// Represents a UTF-8 encoded string with a fixed size of 13 bytes.
/// </summary>
public unsafe struct Utf8String13
{
	private const int _size = 13;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String13 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String13 instance.</param>
	public static implicit operator string(Utf8String13 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String13.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String13(string value)
	{
		var str = new Utf8String13();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 14 bytes.
/// </summary>
public unsafe struct Utf8String14
{
	private const int _size = 14;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String14 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String14 instance.</param>
	public static implicit operator string(Utf8String14 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String14.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String14(string value)
	{
		var str = new Utf8String14();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 15 bytes.
/// </summary>
public unsafe struct Utf8String15
{
	private const int _size = 15;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String15 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String15 instance.</param>
	public static implicit operator string(Utf8String15 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String15.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String15(string value)
	{
		var str = new Utf8String15();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 16 bytes.
/// </summary>
public unsafe struct Utf8String16
{
	private const int _size = 16;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String16 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String16 instance.</param>
	public static implicit operator string(Utf8String16 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String16.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String16(string value)
	{
		var str = new Utf8String16();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 17 bytes.
/// </summary>
public unsafe struct Utf8String17
{
	private const int _size = 17;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String17 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String17 instance.</param>
	public static implicit operator string(Utf8String17 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String17.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String17(string value)
	{
		var str = new Utf8String17();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 18 bytes.
/// </summary>
public unsafe struct Utf8String18
{
	private const int _size = 18;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String18 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String18 instance.</param>
	public static implicit operator string(Utf8String18 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String18.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String18(string value)
	{
		var str = new Utf8String18();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 19 bytes.
/// </summary>
public unsafe struct Utf8String19
{
	private const int _size = 19;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String19 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String19 instance.</param>
	public static implicit operator string(Utf8String19 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String19.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String19(string value)
	{
		var str = new Utf8String19();
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
/// Represents a UTF-8 encoded string with a fixed size of 21 bytes.
/// </summary>
public unsafe struct Utf8String21
{
	private const int _size = 21;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String21 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String21 instance.</param>
	public static implicit operator string(Utf8String21 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String21.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String21(string value)
	{
		var str = new Utf8String21();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 22 bytes.
/// </summary>
public unsafe struct Utf8String22
{
	private const int _size = 22;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String22 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String22 instance.</param>
	public static implicit operator string(Utf8String22 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String22.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String22(string value)
	{
		var str = new Utf8String22();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 23 bytes.
/// </summary>
public unsafe struct Utf8String23
{
	private const int _size = 23;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String23 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String23 instance.</param>
	public static implicit operator string(Utf8String23 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String23.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String23(string value)
	{
		var str = new Utf8String23();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 24 bytes.
/// </summary>
public unsafe struct Utf8String24
{
	private const int _size = 24;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String24 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String24 instance.</param>
	public static implicit operator string(Utf8String24 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String24.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String24(string value)
	{
		var str = new Utf8String24();
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
/// Represents a UTF-8 encoded string with a fixed size of 26 bytes.
/// </summary>
public unsafe struct Utf8String26
{
	private const int _size = 26;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String26 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String26 instance.</param>
	public static implicit operator string(Utf8String26 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String26.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String26(string value)
	{
		var str = new Utf8String26();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 27 bytes.
/// </summary>
public unsafe struct Utf8String27
{
	private const int _size = 27;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String27 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String27 instance.</param>
	public static implicit operator string(Utf8String27 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String27.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String27(string value)
	{
		var str = new Utf8String27();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 28 bytes.
/// </summary>
public unsafe struct Utf8String28
{
	private const int _size = 28;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String28 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String28 instance.</param>
	public static implicit operator string(Utf8String28 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String28.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String28(string value)
	{
		var str = new Utf8String28();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string regarda fixed size of 29 bytes.
/// </summary>
public unsafe struct Utf8String29
{
	private const int _size = 29;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String29 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String29 instance.</param>
	public static implicit operator string(Utf8String29 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String29.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String29(string value)
	{
		var str = new Utf8String29();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 30 bytes.
/// </summary>
public unsafe struct Utf8String30
{
	private const int _size = 30;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String30 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String30 instance.</param>
	public static implicit operator string(Utf8String30 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String30.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String30(string value)
	{
		var str = new Utf8String30();
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
/// Represents a UTF-8 encoded string with a fixed size of 32 bytes.
/// </summary>
public unsafe struct Utf8String32
{
	private const int _size = 32;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String32 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String32 instance.</param>
	public static implicit operator string(Utf8String32 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String32.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String32(string value)
	{
		var str = new Utf8String32();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 33 bytes.
/// </summary>
public unsafe struct Utf8String33
{
	private const int _size = 33;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String33 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String33 instance.</param>
	public static implicit operator string(Utf8String33 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String33.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String33(string value)
	{
		var str = new Utf8String33();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 48 bytes.
/// </summary>
public unsafe struct Utf8String48
{
	private const int _size = 48;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String48 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String48 instance.</param>
	public static implicit operator string(Utf8String48 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String48.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String48(string value)
	{
		var str = new Utf8String48();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 64 bytes.
/// </summary>
public unsafe struct Utf8String64
{
	private const int _size = 64;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String64 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String64 instance.</param>
	public static implicit operator string(Utf8String64 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String64.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String64(string value)
	{
		var str = new Utf8String64();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 65 bytes.
/// </summary>
public unsafe struct Utf8String65
{
	private const int _size = 65;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String65 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String65 instance.</param>
	public static implicit operator string(Utf8String65 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String65.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String65(string value)
	{
		var str = new Utf8String65();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 128 bytes.
/// </summary>
public unsafe struct Utf8String128
{
	private const int _size = 128;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String128 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String128 instance.</param>
	public static implicit operator string(Utf8String128 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String128.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String128(string value)
	{
		var str = new Utf8String128();
		return str.ToUtf8(value, str.Value);
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns>The string representation.</returns>
	public override readonly string ToString() => (string)this;
}

/// <summary>
/// Represents a UTF-8 encoded string with a fixed size of 129 bytes.
/// </summary>
public unsafe struct Utf8String129
{
	private const int _size = 129;

	/// <summary>
	/// Fixed byte array storing the UTF-8 encoded string.
	/// </summary>
	public fixed byte Value[_size];

	/// <summary>
	/// Implicit conversion from Utf8String129 to <see cref="string"/>.
	/// </summary>
	/// <param name="value">The Utf8String129 instance.</param>
	public static implicit operator string(Utf8String129 value)
		=> _size.ToUtf8(value.Value);

	/// <summary>
	/// Explicit conversion from <see cref="string"/> to Utf8String129.
	/// </summary>
	/// <param name="value">The string value.</param>
	public static explicit operator Utf8String129(string value)
	{
		var str = new Utf8String129();
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