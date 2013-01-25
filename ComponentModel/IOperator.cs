namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;

	public interface IOperator<T> : IComparer<T>
	{
		T Add(T first, T second);
		T Subtract(T first, T second);
		T Multiply(T first, T second);
		T Divide(T first, T second);
	}

	class IntOperator : IOperator<int>
	{
		int IOperator<int>.Add(int first, int second)
		{
			return first + second;
		}

		int IOperator<int>.Subtract(int first, int second)
		{
			return first - second;
		}

		int IOperator<int>.Multiply(int first, int second)
		{
			return first * second;
		}

		int IOperator<int>.Divide(int first, int second)
		{
			return first / second;
		}

		int IComparer<int>.Compare(int first, int second)
		{
			return first.CompareTo(second);
		}
	}

	class UIntOperator : IOperator<uint>
	{
		uint IOperator<uint>.Add(uint first, uint second)
		{
			return first + second;
		}

		uint IOperator<uint>.Subtract(uint first, uint second)
		{
			return first - second;
		}

		uint IOperator<uint>.Multiply(uint first, uint second)
		{
			return first * second;
		}

		uint IOperator<uint>.Divide(uint first, uint second)
		{
			return first / second;
		}

		int IComparer<uint>.Compare(uint first, uint second)
		{
			return first.CompareTo(second);
		}
	}

	class ShortOperator : IOperator<short>
	{
		short IOperator<short>.Add(short first, short second)
		{
			return (short)(first + second);
		}

		short IOperator<short>.Subtract(short first, short second)
		{
			return (short)(first - second);
		}

		short IOperator<short>.Multiply(short first, short second)
		{
			return (short)(first * second);
		}

		short IOperator<short>.Divide(short first, short second)
		{
			return (short)(first / second);
		}

		int IComparer<short>.Compare(short first, short second)
		{
			return first.CompareTo(second);
		}
	}

	class UShortOperator : IOperator<ushort>
	{
		ushort IOperator<ushort>.Add(ushort first, ushort second)
		{
			return (ushort)(first + second);
		}

		ushort IOperator<ushort>.Subtract(ushort first, ushort second)
		{
			return (ushort)(first - second);
		}

		ushort IOperator<ushort>.Multiply(ushort first, ushort second)
		{
			return (ushort)(first * second);
		}

		ushort IOperator<ushort>.Divide(ushort first, ushort second)
		{
			return (ushort)(first / second);
		}

		int IComparer<ushort>.Compare(ushort first, ushort second)
		{
			return first.CompareTo(second);
		}
	}

	class LongOperator : IOperator<long>
	{
		long IOperator<long>.Add(long first, long second)
		{
			return first + second;
		}

		long IOperator<long>.Subtract(long first, long second)
		{
			return first - second;
		}

		long IOperator<long>.Multiply(long first, long second)
		{
			return first * second;
		}

		long IOperator<long>.Divide(long first, long second)
		{
			return first / second;
		}

		int IComparer<long>.Compare(long first, long second)
		{
			return first.CompareTo(second);
		}
	}

	class ULongOperator : IOperator<ulong>
	{
		ulong IOperator<ulong>.Add(ulong first, ulong second)
		{
			return first + second;
		}

		ulong IOperator<ulong>.Subtract(ulong first, ulong second)
		{
			return first - second;
		}

		ulong IOperator<ulong>.Multiply(ulong first, ulong second)
		{
			return first * second;
		}

		ulong IOperator<ulong>.Divide(ulong first, ulong second)
		{
			return first / second;
		}

		int IComparer<ulong>.Compare(ulong first, ulong second)
		{
			return first.CompareTo(second);
		}
	}

	class FloatOperator : IOperator<float>
	{
		float IOperator<float>.Add(float first, float second)
		{
			return first + second;
		}

		float IOperator<float>.Subtract(float first, float second)
		{
			return first - second;
		}

		float IOperator<float>.Multiply(float first, float second)
		{
			return first * second;
		}

		float IOperator<float>.Divide(float first, float second)
		{
			return first / second;
		}

		int IComparer<float>.Compare(float first, float second)
		{
			return first.CompareTo(second);
		}
	}

	class DoubleOperator : IOperator<double>
	{
		double IOperator<double>.Add(double first, double second)
		{
			return first + second;
		}

		double IOperator<double>.Subtract(double first, double second)
		{
			return first - second;
		}

		double IOperator<double>.Multiply(double first, double second)
		{
			return first * second;
		}

		double IOperator<double>.Divide(double first, double second)
		{
			return first / second;
		}

		int IComparer<double>.Compare(double first, double second)
		{
			return first.CompareTo(second);
		}
	}

	class ByteOperator : IOperator<byte>
	{
		byte IOperator<byte>.Add(byte first, byte second)
		{
			return (byte)(first + second);
		}

		byte IOperator<byte>.Subtract(byte first, byte second)
		{
			return (byte)(first - second);
		}

		byte IOperator<byte>.Multiply(byte first, byte second)
		{
			return (byte)(first * second);
		}

		byte IOperator<byte>.Divide(byte first, byte second)
		{
			return (byte)(first / second);
		}

		int IComparer<byte>.Compare(byte first, byte second)
		{
			return first.CompareTo(second);
		}
	}

	class SByteOperator : IOperator<sbyte>
	{
		sbyte IOperator<sbyte>.Add(sbyte first, sbyte second)
		{
			return (sbyte)(first + second);
		}

		sbyte IOperator<sbyte>.Subtract(sbyte first, sbyte second)
		{
			return (sbyte)(first - second);
		}

		sbyte IOperator<sbyte>.Multiply(sbyte first, sbyte second)
		{
			return (sbyte)(first * second);
		}

		sbyte IOperator<sbyte>.Divide(sbyte first, sbyte second)
		{
			return (sbyte)(first / second);
		}

		int IComparer<sbyte>.Compare(sbyte first, sbyte second)
		{
			return first.CompareTo(second);
		}
	}

	class DecimalOperator : IOperator<decimal>
	{
		decimal IOperator<decimal>.Add(decimal first, decimal second)
		{
			return first + second;
		}

		decimal IOperator<decimal>.Subtract(decimal first, decimal second)
		{
			return first - second;
		}

		decimal IOperator<decimal>.Multiply(decimal first, decimal second)
		{
			return first * second;
		}

		decimal IOperator<decimal>.Divide(decimal first, decimal second)
		{
			return first / second;
		}

		int IComparer<decimal>.Compare(decimal first, decimal second)
		{
			return first.CompareTo(second);
		}
	}

	class TimeSpanOperator : IOperator<TimeSpan>
	{
		TimeSpan IOperator<TimeSpan>.Add(TimeSpan first, TimeSpan second)
		{
			return first + second;
		}

		TimeSpan IOperator<TimeSpan>.Subtract(TimeSpan first, TimeSpan second)
		{
			return first - second;
		}

		TimeSpan IOperator<TimeSpan>.Multiply(TimeSpan first, TimeSpan second)
		{
			return new TimeSpan(first.Ticks * second.Ticks);
		}

		TimeSpan IOperator<TimeSpan>.Divide(TimeSpan first, TimeSpan second)
		{
			return new TimeSpan(first.Ticks / second.Ticks);
		}

		int IComparer<TimeSpan>.Compare(TimeSpan first, TimeSpan second)
		{
			return first.CompareTo(second);
		}
	}

	class DateTimeOperator : IOperator<DateTime>
	{
		DateTime IOperator<DateTime>.Add(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks + second.Ticks);
		}

		DateTime IOperator<DateTime>.Subtract(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks - second.Ticks);
		}

		DateTime IOperator<DateTime>.Multiply(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks * second.Ticks);
		}

		DateTime IOperator<DateTime>.Divide(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks / second.Ticks);
		}

		int IComparer<DateTime>.Compare(DateTime first, DateTime second)
		{
			return first.CompareTo(second);
		}
	}
}