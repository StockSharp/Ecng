namespace Ecng.Common;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Provides basic arithmetic operations and comparison functionality.
/// </summary>
public interface IOperator : IComparer
{
	/// <summary>
	/// Adds two objects.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of addition.</returns>
	object Add(object first, object second);

	/// <summary>
	/// Subtracts the second object from the first.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of subtraction.</returns>
	object Subtract(object first, object second);

	/// <summary>
	/// Multiplies two objects.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of multiplication.</returns>
	object Multiply(object first, object second);

	/// <summary>
	/// Divides the first object by the second.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of division.</returns>
	object Divide(object first, object second);
}

/// <summary>
/// Provides strongly-typed arithmetic operations and comparison functionality.
/// </summary>
/// <typeparam name="T">The type to operate on.</typeparam>
public interface IOperator<T> : IComparer<T>, IOperator
{
	/// <summary>
	/// Adds two values of type T.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of addition.</returns>
	T Add(T first, T second);

	/// <summary>
	/// Subtracts the second value from the first.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of subtraction.</returns>
	T Subtract(T first, T second);

	/// <summary>
	/// Multiplies two values of type T.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of multiplication.</returns>
	T Multiply(T first, T second);

	/// <summary>
	/// Divides the first value by the second.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of division.</returns>
	T Divide(T first, T second);
}

/// <summary>
/// Provides a base implementation for arithmetic operators.
/// </summary>
/// <typeparam name="T">The type to operate on.</typeparam>
public abstract class BaseOperator<T> : IOperator<T>
{
	/// <summary>
	/// Compares two values.
	/// </summary>
	/// <param name="x">The first value.</param>
	/// <param name="y">The second value.</param>
	/// <returns>A value indicating the relative order.</returns>
	public abstract int Compare(T x, T y);

	/// <summary>
	/// Compares two objects.
	/// </summary>
	/// <param name="x">The first object.</param>
	/// <param name="y">The second object.</param>
	/// <returns>A value indicating the relative order.</returns>
	int IComparer.Compare(object x, object y)
	{
		return Compare((T)x, (T)y);
	}

	/// <summary>
	/// Adds two objects.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of addition.</returns>
	object IOperator.Add(object first, object second)
	{
		return Add((T)first, (T)second);
	}

	/// <summary>
	/// Subtracts the second object from the first.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of subtraction.</returns>
	object IOperator.Subtract(object first, object second)
	{
		return Subtract((T)first, (T)second);
	}

	/// <summary>
	/// Multiplies two objects.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of multiplication.</returns>
	object IOperator.Multiply(object first, object second)
	{
		return Multiply((T)first, (T)second);
	}

	/// <summary>
	/// Divides the first object by the second.
	/// </summary>
	/// <param name="first">The first object.</param>
	/// <param name="second">The second object.</param>
	/// <returns>The result of division.</returns>
	object IOperator.Divide(object first, object second)
	{
		return Divide((T)first, (T)second);
	}

	/// <summary>
	/// Adds two values.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of addition.</returns>
	public abstract T Add(T first, T second);

	/// <summary>
	/// Subtracts the second value from the first.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of subtraction.</returns>
	public abstract T Subtract(T first, T second);

	/// <summary>
	/// Multiplies two values.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of multiplication.</returns>
	public abstract T Multiply(T first, T second);

	/// <summary>
	/// Divides the first value by the second.
	/// </summary>
	/// <param name="first">The first value.</param>
	/// <param name="second">The second value.</param>
	/// <returns>The result of division.</returns>
	public abstract T Divide(T first, T second);
}

/// <summary>
/// Implements arithmetic operations for integers.
/// </summary>
public class IntOperator : BaseOperator<int>
{
	/// <summary>
	/// Adds two integers.
	/// </summary>
	public override int Add(int first, int second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second integer from the first.
	/// </summary>
	public override int Subtract(int first, int second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two integers.
	/// </summary>
	public override int Multiply(int first, int second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first integer by the second.
	/// </summary>
	public override int Divide(int first, int second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two integers.
	/// </summary>
	public override int Compare(int first, int second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for unsigned integers.
/// </summary>
[CLSCompliant(false)]
public class UIntOperator : BaseOperator<uint>
{
	/// <summary>
	/// Adds two unsigned integers.
	/// </summary>
	public override uint Add(uint first, uint second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second unsigned integer from the first.
	/// </summary>
	public override uint Subtract(uint first, uint second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two unsigned integers.
	/// </summary>
	public override uint Multiply(uint first, uint second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first unsigned integer by the second.
	/// </summary>
	public override uint Divide(uint first, uint second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two unsigned integers.
	/// </summary>
	public override int Compare(uint first, uint second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for short integers.
/// </summary>
public class ShortOperator : BaseOperator<short>
{
	/// <summary>
	/// Adds two short integers.
	/// </summary>
	public override short Add(short first, short second)
	{
		return (short)(first + second);
	}

	/// <summary>
	/// Subtracts the second short integer from the first.
	/// </summary>
	public override short Subtract(short first, short second)
	{
		return (short)(first - second);
	}

	/// <summary>
	/// Multiplies two short integers.
	/// </summary>
	public override short Multiply(short first, short second)
	{
		return (short)(first * second);
	}

	/// <summary>
	/// Divides the first short integer by the second.
	/// </summary>
	public override short Divide(short first, short second)
	{
		return (short)(first / second);
	}

	/// <summary>
	/// Compares two short integers.
	/// </summary>
	public override int Compare(short first, short second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for unsigned short integers.
/// </summary>
[CLSCompliant(false)]
public class UShortOperator : BaseOperator<ushort>
{
	/// <summary>
	/// Adds two unsigned short integers.
	/// </summary>
	public override ushort Add(ushort first, ushort second)
	{
		return (ushort)(first + second);
	}

	/// <summary>
	/// Subtracts the second unsigned short integer from the first.
	/// </summary>
	public override ushort Subtract(ushort first, ushort second)
	{
		return (ushort)(first - second);
	}

	/// <summary>
	/// Multiplies two unsigned short integers.
	/// </summary>
	public override ushort Multiply(ushort first, ushort second)
	{
		return (ushort)(first * second);
	}

	/// <summary>
	/// Divides the first unsigned short integer by the second.
	/// </summary>
	public override ushort Divide(ushort first, ushort second)
	{
		return (ushort)(first / second);
	}

	/// <summary>
	/// Compares two unsigned short integers.
	/// </summary>
	public override int Compare(ushort first, ushort second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for long integers.
/// </summary>
public class LongOperator : BaseOperator<long>
{
	/// <summary>
	/// Adds two long integers.
	/// </summary>
	public override long Add(long first, long second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second long integer from the first.
	/// </summary>
	public override long Subtract(long first, long second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two long integers.
	/// </summary>
	public override long Multiply(long first, long second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first long integer by the second.
	/// </summary>
	public override long Divide(long first, long second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two long integers.
	/// </summary>
	public override int Compare(long first, long second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for unsigned long integers.
/// </summary>
[CLSCompliant(false)]
public class ULongOperator : BaseOperator<ulong>
{
	/// <summary>
	/// Adds two unsigned long integers.
	/// </summary>
	public override ulong Add(ulong first, ulong second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second unsigned long integer from the first.
	/// </summary>
	public override ulong Subtract(ulong first, ulong second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two unsigned long integers.
	/// </summary>
	public override ulong Multiply(ulong first, ulong second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first unsigned long integer by the second.
	/// </summary>
	public override ulong Divide(ulong first, ulong second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two unsigned long integers.
	/// </summary>
	public override int Compare(ulong first, ulong second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for floating-point numbers.
/// </summary>
public class FloatOperator : BaseOperator<float>
{
	/// <summary>
	/// Adds two float values.
	/// </summary>
	public override float Add(float first, float second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second float value from the first.
	/// </summary>
	public override float Subtract(float first, float second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two float values.
	/// </summary>
	public override float Multiply(float first, float second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first float value by the second.
	/// </summary>
	public override float Divide(float first, float second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two float values.
	/// </summary>
	public override int Compare(float first, float second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for double-precision floating-point numbers.
/// </summary>
public class DoubleOperator : BaseOperator<double>
{
	/// <summary>
	/// Adds two double values.
	/// </summary>
	public override double Add(double first, double second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second double value from the first.
	/// </summary>
	public override double Subtract(double first, double second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two double values.
	/// </summary>
	public override double Multiply(double first, double second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first double value by the second.
	/// </summary>
	public override double Divide(double first, double second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two double values.
	/// </summary>
	public override int Compare(double first, double second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for byte values.
/// </summary>
public class ByteOperator : BaseOperator<byte>
{
	/// <summary>
	/// Adds two byte values.
	/// </summary>
	public override byte Add(byte first, byte second)
	{
		return (byte)(first + second);
	}

	/// <summary>
	/// Subtracts the second byte value from the first.
	/// </summary>
	public override byte Subtract(byte first, byte second)
	{
		return (byte)(first - second);
	}

	/// <summary>
	/// Multiplies two byte values.
	/// </summary>
	public override byte Multiply(byte first, byte second)
	{
		return (byte)(first * second);
	}

	/// <summary>
	/// Divides the first byte value by the second.
	/// </summary>
	public override byte Divide(byte first, byte second)
	{
		return (byte)(first / second);
	}

	/// <summary>
	/// Compares two byte values.
	/// </summary>
	public override int Compare(byte first, byte second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for signed byte values.
/// </summary>
[CLSCompliant(false)]
public class SByteOperator : BaseOperator<sbyte>
{
	/// <summary>
	/// Adds two sbyte values.
	/// </summary>
	public override sbyte Add(sbyte first, sbyte second)
	{
		return (sbyte)(first + second);
	}

	/// <summary>
	/// Subtracts the second sbyte value from the first.
	/// </summary>
	public override sbyte Subtract(sbyte first, sbyte second)
	{
		return (sbyte)(first - second);
	}

	/// <summary>
	/// Multiplies two sbyte values.
	/// </summary>
	public override sbyte Multiply(sbyte first, sbyte second)
	{
		return (sbyte)(first * second);
	}

	/// <summary>
	/// Divides the first sbyte value by the second.
	/// </summary>
	public override sbyte Divide(sbyte first, sbyte second)
	{
		return (sbyte)(first / second);
	}

	/// <summary>
	/// Compares two sbyte values.
	/// </summary>
	public override int Compare(sbyte first, sbyte second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for decimal numbers.
/// </summary>
public class DecimalOperator : BaseOperator<decimal>
{
	/// <summary>
	/// Adds two decimals.
	/// </summary>
	public override decimal Add(decimal first, decimal second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second decimal from the first.
	/// </summary>
	public override decimal Subtract(decimal first, decimal second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two decimals.
	/// </summary>
	public override decimal Multiply(decimal first, decimal second)
	{
		return first * second;
	}

	/// <summary>
	/// Divides the first decimal by the second.
	/// </summary>
	public override decimal Divide(decimal first, decimal second)
	{
		return first / second;
	}

	/// <summary>
	/// Compares two decimals.
	/// </summary>
	public override int Compare(decimal first, decimal second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for TimeSpan values.
/// </summary>
public class TimeSpanOperator : BaseOperator<TimeSpan>
{
	/// <summary>
	/// Adds two TimeSpan values.
	/// </summary>
	public override TimeSpan Add(TimeSpan first, TimeSpan second)
	{
		return first + second;
	}

	/// <summary>
	/// Subtracts the second TimeSpan from the first.
	/// </summary>
	public override TimeSpan Subtract(TimeSpan first, TimeSpan second)
	{
		return first - second;
	}

	/// <summary>
	/// Multiplies two TimeSpan values by multiplying their ticks.
	/// </summary>
	public override TimeSpan Multiply(TimeSpan first, TimeSpan second)
	{
		return new TimeSpan(first.Ticks * second.Ticks);
	}

	/// <summary>
	/// Divides two TimeSpan values by dividing their ticks.
	/// </summary>
	public override TimeSpan Divide(TimeSpan first, TimeSpan second)
	{
		return new TimeSpan(first.Ticks / second.Ticks);
	}

	/// <summary>
	/// Compares two TimeSpan values.
	/// </summary>
	public override int Compare(TimeSpan first, TimeSpan second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for DateTime values.
/// </summary>
public class DateTimeOperator : BaseOperator<DateTime>
{
	/// <summary>
	/// Adds two DateTime values by adding their ticks.
	/// </summary>
	public override DateTime Add(DateTime first, DateTime second)
	{
		return new DateTime(first.Ticks + second.Ticks);
	}

	/// <summary>
	/// Subtracts the tick values of the second DateTime from the first.
	/// </summary>
	public override DateTime Subtract(DateTime first, DateTime second)
	{
		return new DateTime(first.Ticks - second.Ticks);
	}

	/// <summary>
	/// Multiplies two DateTime values by multiplying their ticks.
	/// </summary>
	public override DateTime Multiply(DateTime first, DateTime second)
	{
		return new DateTime(first.Ticks * second.Ticks);
	}

	/// <summary>
	/// Divides two DateTime values by dividing their ticks.
	/// </summary>
	public override DateTime Divide(DateTime first, DateTime second)
	{
		return new DateTime(first.Ticks / second.Ticks);
	}

	/// <summary>
	/// Compares two DateTime values.
	/// </summary>
	public override int Compare(DateTime first, DateTime second)
	{
		return first.CompareTo(second);
	}
}

/// <summary>
/// Implements arithmetic operations for DateTimeOffset values.
/// </summary>
public class DateTimeOffsetOperator : BaseOperator<DateTimeOffset>
{
	/// <summary>
	/// Adds two DateTimeOffset values by adding their UTC ticks.
	/// </summary>
	public override DateTimeOffset Add(DateTimeOffset first, DateTimeOffset second)
	{
		return new DateTimeOffset(first.UtcTicks + second.UtcTicks, first.Offset);
	}

	/// <summary>
	/// Subtracts the UTC ticks of the second DateTimeOffset from the first.
	/// </summary>
	public override DateTimeOffset Subtract(DateTimeOffset first, DateTimeOffset second)
	{
		return new DateTimeOffset(first.UtcTicks - second.UtcTicks, first.Offset);
	}

	/// <summary>
	/// Multiplies two DateTimeOffset values by multiplying their UTC ticks.
	/// </summary>
	public override DateTimeOffset Multiply(DateTimeOffset first, DateTimeOffset second)
	{
		return new DateTimeOffset(first.UtcTicks * second.UtcTicks, first.Offset);
	}

	/// <summary>
	/// Divides two DateTimeOffset values by dividing their UTC ticks.
	/// </summary>
	public override DateTimeOffset Divide(DateTimeOffset first, DateTimeOffset second)
	{
		return new DateTimeOffset(first.UtcTicks / second.UtcTicks, first.Offset);
	}

	/// <summary>
	/// Compares two DateTimeOffset values.
	/// </summary>
	public override int Compare(DateTimeOffset first, DateTimeOffset second)
	{
		return first.CompareTo(second);
	}
}