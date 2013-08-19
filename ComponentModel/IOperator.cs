namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public interface IOperator : IComparer
	{
		object Add(object first, object second);
		object Subtract(object first, object second);
		object Multiply(object first, object second);
		object Divide(object first, object second);
	}

	public interface IOperator<T> : IComparer<T>, IOperator
	{
		T Add(T first, T second);
		T Subtract(T first, T second);
		T Multiply(T first, T second);
		T Divide(T first, T second);
	}

	public abstract class BaseOperator<T> : IOperator<T>
	{
		public abstract int Compare(T x, T y);

		int IComparer.Compare(object x, object y)
		{
			return Compare((T)x, (T)y);
		}

		object IOperator.Add(object first, object second)
		{
			return Add((T)first, (T)second);
		}

		object IOperator.Subtract(object first, object second)
		{
			return Subtract((T)first, (T)second);
		}

		object IOperator.Multiply(object first, object second)
		{
			return Multiply((T)first, (T)second);
		}

		object IOperator.Divide(object first, object second)
		{
			return Divide((T)first, (T)second);
		}

		public abstract T Add(T first, T second);

		public abstract T Subtract(T first, T second);

		public abstract T Multiply(T first, T second);

		public abstract T Divide(T first, T second);
	}

	class IntOperator : BaseOperator<int>
	{
		public override int Add(int first, int second)
		{
			return first + second;
		}

		public override int Subtract(int first, int second)
		{
			return first - second;
		}

		public override int Multiply(int first, int second)
		{
			return first * second;
		}

		public override int Divide(int first, int second)
		{
			return first / second;
		}

		public override int Compare(int first, int second)
		{
			return first.CompareTo(second);
		}
	}

	class UIntOperator : BaseOperator<uint>
	{
		public override uint Add(uint first, uint second)
		{
			return first + second;
		}

		public override uint Subtract(uint first, uint second)
		{
			return first - second;
		}

		public override uint Multiply(uint first, uint second)
		{
			return first * second;
		}

		public override uint Divide(uint first, uint second)
		{
			return first / second;
		}

		public override int Compare(uint first, uint second)
		{
			return first.CompareTo(second);
		}
	}

	class ShortOperator : BaseOperator<short>
	{
		public override short Add(short first, short second)
		{
			return (short)(first + second);
		}

		public override short Subtract(short first, short second)
		{
			return (short)(first - second);
		}

		public override short Multiply(short first, short second)
		{
			return (short)(first * second);
		}

		public override short Divide(short first, short second)
		{
			return (short)(first / second);
		}

		public override int Compare(short first, short second)
		{
			return first.CompareTo(second);
		}
	}

	class UShortOperator : BaseOperator<ushort>
	{
		public override ushort Add(ushort first, ushort second)
		{
			return (ushort)(first + second);
		}

		public override ushort Subtract(ushort first, ushort second)
		{
			return (ushort)(first - second);
		}

		public override ushort Multiply(ushort first, ushort second)
		{
			return (ushort)(first * second);
		}

		public override ushort Divide(ushort first, ushort second)
		{
			return (ushort)(first / second);
		}

		public override int Compare(ushort first, ushort second)
		{
			return first.CompareTo(second);
		}
	}

	class LongOperator : BaseOperator<long>
	{
		public override long Add(long first, long second)
		{
			return first + second;
		}

		public override long Subtract(long first, long second)
		{
			return first - second;
		}

		public override long Multiply(long first, long second)
		{
			return first * second;
		}

		public override long Divide(long first, long second)
		{
			return first / second;
		}

		public override int Compare(long first, long second)
		{
			return first.CompareTo(second);
		}
	}

	class ULongOperator : BaseOperator<ulong>
	{
		public override ulong Add(ulong first, ulong second)
		{
			return first + second;
		}

		public override ulong Subtract(ulong first, ulong second)
		{
			return first - second;
		}

		public override ulong Multiply(ulong first, ulong second)
		{
			return first * second;
		}

		public override ulong Divide(ulong first, ulong second)
		{
			return first / second;
		}

		public override int Compare(ulong first, ulong second)
		{
			return first.CompareTo(second);
		}
	}

	class FloatOperator : BaseOperator<float>
	{
		public override float Add(float first, float second)
		{
			return first + second;
		}

		public override float Subtract(float first, float second)
		{
			return first - second;
		}

		public override float Multiply(float first, float second)
		{
			return first * second;
		}

		public override float Divide(float first, float second)
		{
			return first / second;
		}

		public override int Compare(float first, float second)
		{
			return first.CompareTo(second);
		}
	}

	class DoubleOperator : BaseOperator<double>
	{
		public override double Add(double first, double second)
		{
			return first + second;
		}

		public override double Subtract(double first, double second)
		{
			return first - second;
		}

		public override double Multiply(double first, double second)
		{
			return first * second;
		}

		public override double Divide(double first, double second)
		{
			return first / second;
		}

		public override int Compare(double first, double second)
		{
			return first.CompareTo(second);
		}
	}

	class ByteOperator : BaseOperator<byte>
	{
		public override byte Add(byte first, byte second)
		{
			return (byte)(first + second);
		}

		public override byte Subtract(byte first, byte second)
		{
			return (byte)(first - second);
		}

		public override byte Multiply(byte first, byte second)
		{
			return (byte)(first * second);
		}

		public override byte Divide(byte first, byte second)
		{
			return (byte)(first / second);
		}

		public override int Compare(byte first, byte second)
		{
			return first.CompareTo(second);
		}
	}

	class SByteOperator : BaseOperator<sbyte>
	{
		public override sbyte Add(sbyte first, sbyte second)
		{
			return (sbyte)(first + second);
		}

		public override sbyte Subtract(sbyte first, sbyte second)
		{
			return (sbyte)(first - second);
		}

		public override sbyte Multiply(sbyte first, sbyte second)
		{
			return (sbyte)(first * second);
		}

		public override sbyte Divide(sbyte first, sbyte second)
		{
			return (sbyte)(first / second);
		}

		public override int Compare(sbyte first, sbyte second)
		{
			return first.CompareTo(second);
		}
	}

	class DecimalOperator : BaseOperator<decimal>
	{
		public override decimal Add(decimal first, decimal second)
		{
			return first + second;
		}

		public override decimal Subtract(decimal first, decimal second)
		{
			return first - second;
		}

		public override decimal Multiply(decimal first, decimal second)
		{
			return first * second;
		}

		public override decimal Divide(decimal first, decimal second)
		{
			return first / second;
		}

		public override int Compare(decimal first, decimal second)
		{
			return first.CompareTo(second);
		}
	}

	class TimeSpanOperator : BaseOperator<TimeSpan>
	{
		public override TimeSpan Add(TimeSpan first, TimeSpan second)
		{
			return first + second;
		}

		public override TimeSpan Subtract(TimeSpan first, TimeSpan second)
		{
			return first - second;
		}

		public override TimeSpan Multiply(TimeSpan first, TimeSpan second)
		{
			return new TimeSpan(first.Ticks * second.Ticks);
		}

		public override TimeSpan Divide(TimeSpan first, TimeSpan second)
		{
			return new TimeSpan(first.Ticks / second.Ticks);
		}

		public override int Compare(TimeSpan first, TimeSpan second)
		{
			return first.CompareTo(second);
		}
	}

	class DateTimeOperator : BaseOperator<DateTime>
	{
		public override DateTime Add(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks + second.Ticks);
		}

		public override DateTime Subtract(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks - second.Ticks);
		}

		public override DateTime Multiply(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks * second.Ticks);
		}

		public override DateTime Divide(DateTime first, DateTime second)
		{
			return new DateTime(first.Ticks / second.Ticks);
		}

		public override int Compare(DateTime first, DateTime second)
		{
			return first.CompareTo(second);
		}
	}
}