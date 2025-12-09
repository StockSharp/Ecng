namespace Ecng.Tests.Common;

[TestClass]
public class OperatorTests : BaseTestClass
{
	#region IntOperator Tests

	[TestMethod]
	public void IntOperator_Add()
	{
		var op = new IntOperator();
		op.Add(2, 3).AssertEqual(5);
		op.Add(-1, 1).AssertEqual(0);
		op.Add(0, 0).AssertEqual(0);
	}

	[TestMethod]
	public void IntOperator_Subtract()
	{
		var op = new IntOperator();
		op.Subtract(5, 3).AssertEqual(2);
		op.Subtract(3, 5).AssertEqual(-2);
		op.Subtract(0, 0).AssertEqual(0);
	}

	[TestMethod]
	public void IntOperator_Multiply()
	{
		var op = new IntOperator();
		op.Multiply(3, 4).AssertEqual(12);
		op.Multiply(-2, 3).AssertEqual(-6);
		op.Multiply(0, 100).AssertEqual(0);
	}

	[TestMethod]
	public void IntOperator_Divide()
	{
		var op = new IntOperator();
		op.Divide(10, 2).AssertEqual(5);
		op.Divide(7, 3).AssertEqual(2); // Integer division
		op.Divide(-6, 2).AssertEqual(-3);
	}

	[TestMethod]
	public void IntOperator_Compare()
	{
		var op = new IntOperator();
		(op.Compare(1, 2) < 0).AssertTrue();
		(op.Compare(2, 1) > 0).AssertTrue();
		op.Compare(1, 1).AssertEqual(0);
	}

	[TestMethod]
	public void IntOperator_NonGenericInterface()
	{
		IOperator op = new IntOperator();
		((int)op.Add(2, 3)).AssertEqual(5);
		((int)op.Subtract(5, 3)).AssertEqual(2);
		((int)op.Multiply(3, 4)).AssertEqual(12);
		((int)op.Divide(10, 2)).AssertEqual(5);
		(op.Compare(1, 2) < 0).AssertTrue();
	}

	#endregion

	#region LongOperator Tests

	[TestMethod]
	public void LongOperator_Add()
	{
		var op = new LongOperator();
		op.Add(2L, 3L).AssertEqual(5L);
		op.Add(long.MaxValue - 1, 1).AssertEqual(long.MaxValue);
	}

	[TestMethod]
	public void LongOperator_Subtract()
	{
		var op = new LongOperator();
		op.Subtract(5L, 3L).AssertEqual(2L);
	}

	[TestMethod]
	public void LongOperator_Multiply()
	{
		var op = new LongOperator();
		op.Multiply(3L, 4L).AssertEqual(12L);
	}

	[TestMethod]
	public void LongOperator_Divide()
	{
		var op = new LongOperator();
		op.Divide(10L, 2L).AssertEqual(5L);
	}

	[TestMethod]
	public void LongOperator_Compare()
	{
		var op = new LongOperator();
		(op.Compare(1L, 2L) < 0).AssertTrue();
		(op.Compare(2L, 1L) > 0).AssertTrue();
		op.Compare(1L, 1L).AssertEqual(0);
	}

	#endregion

	#region ShortOperator Tests

	[TestMethod]
	public void ShortOperator_Add()
	{
		var op = new ShortOperator();
		op.Add((short)2, (short)3).AssertEqual((short)5);
	}

	[TestMethod]
	public void ShortOperator_Subtract()
	{
		var op = new ShortOperator();
		op.Subtract((short)5, (short)3).AssertEqual((short)2);
	}

	[TestMethod]
	public void ShortOperator_Multiply()
	{
		var op = new ShortOperator();
		op.Multiply((short)3, (short)4).AssertEqual((short)12);
	}

	[TestMethod]
	public void ShortOperator_Divide()
	{
		var op = new ShortOperator();
		op.Divide((short)10, (short)2).AssertEqual((short)5);
	}

	[TestMethod]
	public void ShortOperator_Compare()
	{
		var op = new ShortOperator();
		(op.Compare((short)1, (short)2) < 0).AssertTrue();
	}

	#endregion

	#region ByteOperator Tests

	[TestMethod]
	public void ByteOperator_Add()
	{
		var op = new ByteOperator();
		op.Add((byte)2, (byte)3).AssertEqual((byte)5);
	}

	[TestMethod]
	public void ByteOperator_Subtract()
	{
		var op = new ByteOperator();
		op.Subtract((byte)5, (byte)3).AssertEqual((byte)2);
	}

	[TestMethod]
	public void ByteOperator_Multiply()
	{
		var op = new ByteOperator();
		op.Multiply((byte)3, (byte)4).AssertEqual((byte)12);
	}

	[TestMethod]
	public void ByteOperator_Divide()
	{
		var op = new ByteOperator();
		op.Divide((byte)10, (byte)2).AssertEqual((byte)5);
	}

	[TestMethod]
	public void ByteOperator_Compare()
	{
		var op = new ByteOperator();
		(op.Compare((byte)1, (byte)2) < 0).AssertTrue();
	}

	#endregion

	#region FloatOperator Tests

	[TestMethod]
	public void FloatOperator_Add()
	{
		var op = new FloatOperator();
		op.Add(2.5f, 3.5f).AssertEqual(6.0f);
	}

	[TestMethod]
	public void FloatOperator_Subtract()
	{
		var op = new FloatOperator();
		op.Subtract(5.5f, 3.0f).AssertEqual(2.5f);
	}

	[TestMethod]
	public void FloatOperator_Multiply()
	{
		var op = new FloatOperator();
		op.Multiply(3.0f, 4.0f).AssertEqual(12.0f);
	}

	[TestMethod]
	public void FloatOperator_Divide()
	{
		var op = new FloatOperator();
		op.Divide(10.0f, 2.0f).AssertEqual(5.0f);
	}

	[TestMethod]
	public void FloatOperator_Compare()
	{
		var op = new FloatOperator();
		(op.Compare(1.0f, 2.0f) < 0).AssertTrue();
		(op.Compare(2.0f, 1.0f) > 0).AssertTrue();
		op.Compare(1.0f, 1.0f).AssertEqual(0);
	}

	#endregion

	#region DoubleOperator Tests

	[TestMethod]
	public void DoubleOperator_Add()
	{
		var op = new DoubleOperator();
		op.Add(2.5, 3.5).AssertEqual(6.0);
	}

	[TestMethod]
	public void DoubleOperator_Subtract()
	{
		var op = new DoubleOperator();
		op.Subtract(5.5, 3.0).AssertEqual(2.5);
	}

	[TestMethod]
	public void DoubleOperator_Multiply()
	{
		var op = new DoubleOperator();
		op.Multiply(3.0, 4.0).AssertEqual(12.0);
	}

	[TestMethod]
	public void DoubleOperator_Divide()
	{
		var op = new DoubleOperator();
		op.Divide(10.0, 2.0).AssertEqual(5.0);
	}

	[TestMethod]
	public void DoubleOperator_Compare()
	{
		var op = new DoubleOperator();
		(op.Compare(1.0, 2.0) < 0).AssertTrue();
		(op.Compare(2.0, 1.0) > 0).AssertTrue();
		op.Compare(1.0, 1.0).AssertEqual(0);
	}

	#endregion

	#region DecimalOperator Tests

	[TestMethod]
	public void DecimalOperator_Add()
	{
		var op = new DecimalOperator();
		op.Add(2.5m, 3.5m).AssertEqual(6.0m);
	}

	[TestMethod]
	public void DecimalOperator_Subtract()
	{
		var op = new DecimalOperator();
		op.Subtract(5.5m, 3.0m).AssertEqual(2.5m);
	}

	[TestMethod]
	public void DecimalOperator_Multiply()
	{
		var op = new DecimalOperator();
		op.Multiply(3.0m, 4.0m).AssertEqual(12.0m);
	}

	[TestMethod]
	public void DecimalOperator_Divide()
	{
		var op = new DecimalOperator();
		op.Divide(10.0m, 2.0m).AssertEqual(5.0m);
	}

	[TestMethod]
	public void DecimalOperator_Compare()
	{
		var op = new DecimalOperator();
		(op.Compare(1.0m, 2.0m) < 0).AssertTrue();
		(op.Compare(2.0m, 1.0m) > 0).AssertTrue();
		op.Compare(1.0m, 1.0m).AssertEqual(0);
	}

	#endregion

	#region TimeSpanOperator Tests

	[TestMethod]
	public void TimeSpanOperator_Add()
	{
		var op = new TimeSpanOperator();
		var result = op.Add(TimeSpan.FromHours(1), TimeSpan.FromMinutes(30));
		result.AssertEqual(TimeSpan.FromMinutes(90));
	}

	[TestMethod]
	public void TimeSpanOperator_Subtract()
	{
		var op = new TimeSpanOperator();
		var result = op.Subtract(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30));
		result.AssertEqual(TimeSpan.FromMinutes(90));
	}

	[TestMethod]
	public void TimeSpanOperator_Multiply()
	{
		var op = new TimeSpanOperator();
		// Multiplies ticks
		var ts1 = TimeSpan.FromTicks(2);
		var ts2 = TimeSpan.FromTicks(3);
		var result = op.Multiply(ts1, ts2);
		result.Ticks.AssertEqual(6);
	}

	[TestMethod]
	public void TimeSpanOperator_Divide()
	{
		var op = new TimeSpanOperator();
		// Divides ticks
		var ts1 = TimeSpan.FromTicks(10);
		var ts2 = TimeSpan.FromTicks(2);
		var result = op.Divide(ts1, ts2);
		result.Ticks.AssertEqual(5);
	}

	[TestMethod]
	public void TimeSpanOperator_Compare()
	{
		var op = new TimeSpanOperator();
		(op.Compare(TimeSpan.FromHours(1), TimeSpan.FromHours(2)) < 0).AssertTrue();
		(op.Compare(TimeSpan.FromHours(2), TimeSpan.FromHours(1)) > 0).AssertTrue();
		op.Compare(TimeSpan.FromHours(1), TimeSpan.FromHours(1)).AssertEqual(0);
	}

	#endregion

	#region DateTimeOperator Tests

	[TestMethod]
	public void DateTimeOperator_Add()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(100, DateTimeKind.Utc);
		var dt2 = new DateTime(50, DateTimeKind.Utc);
		var result = op.Add(dt1, dt2);
		result.Ticks.AssertEqual(150);
		result.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void DateTimeOperator_Subtract()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(100, DateTimeKind.Local);
		var dt2 = new DateTime(30, DateTimeKind.Local);
		var result = op.Subtract(dt1, dt2);
		result.Ticks.AssertEqual(70);
		result.Kind.AssertEqual(DateTimeKind.Local);
	}

	[TestMethod]
	public void DateTimeOperator_Multiply()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(10, DateTimeKind.Utc);
		var dt2 = new DateTime(5, DateTimeKind.Utc);
		var result = op.Multiply(dt1, dt2);
		result.Ticks.AssertEqual(50);
		result.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void DateTimeOperator_Divide()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(100, DateTimeKind.Utc);
		var dt2 = new DateTime(10, DateTimeKind.Utc);
		var result = op.Divide(dt1, dt2);
		result.Ticks.AssertEqual(10);
		result.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void DateTimeOperator_Compare()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(2020, 1, 1);
		var dt2 = new DateTime(2021, 1, 1);
		(op.Compare(dt1, dt2) < 0).AssertTrue();
		(op.Compare(dt2, dt1) > 0).AssertTrue();
		op.Compare(dt1, dt1).AssertEqual(0);
	}

	[TestMethod]
	public void DateTimeOperator_KindMismatch_Throws()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(100, DateTimeKind.Utc);
		var dt2 = new DateTime(50, DateTimeKind.Local);

		ThrowsExactly<ArgumentException>(() => op.Add(dt1, dt2));
		ThrowsExactly<ArgumentException>(() => op.Subtract(dt1, dt2));
		ThrowsExactly<ArgumentException>(() => op.Multiply(dt1, dt2));
		ThrowsExactly<ArgumentException>(() => op.Divide(dt1, dt2));
	}

	[TestMethod]
	public void DateTimeOperator_UnspecifiedKind_Works()
	{
		var op = new DateTimeOperator();
		var dt1 = new DateTime(100); // Unspecified
		var dt2 = new DateTime(50);  // Unspecified
		var result = op.Add(dt1, dt2);
		result.Ticks.AssertEqual(150);
		result.Kind.AssertEqual(DateTimeKind.Unspecified);
	}

	#endregion

	#region DateTimeOffsetOperator Tests

	[TestMethod]
	public void DateTimeOffsetOperator_Add()
	{
		var op = new DateTimeOffsetOperator();
		var offset = TimeSpan.Zero;
		var dto1 = new DateTimeOffset(1000, offset);
		var dto2 = new DateTimeOffset(500, offset);
		var result = op.Add(dto1, dto2);
		// Uses UTC ticks: result = dto1.UtcTicks + dto2.UtcTicks
		result.UtcTicks.AssertEqual(1500);
	}

	[TestMethod]
	public void DateTimeOffsetOperator_Subtract()
	{
		var op = new DateTimeOffsetOperator();
		var offset = TimeSpan.Zero;
		var dto1 = new DateTimeOffset(1000, offset);
		var dto2 = new DateTimeOffset(300, offset);
		var result = op.Subtract(dto1, dto2);
		result.UtcTicks.AssertEqual(700);
	}

	[TestMethod]
	public void DateTimeOffsetOperator_Compare()
	{
		var op = new DateTimeOffsetOperator();
		var dto1 = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var dto2 = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
		(op.Compare(dto1, dto2) < 0).AssertTrue();
		(op.Compare(dto2, dto1) > 0).AssertTrue();
		op.Compare(dto1, dto1).AssertEqual(0);
	}

	[TestMethod]
	public void DateTimeOffsetOperator_UsesFirstOffset()
	{
		var op = new DateTimeOffsetOperator();
		var offset1 = TimeSpan.FromHours(3);
		var offset2 = TimeSpan.FromHours(5);
		// Create DateTimeOffset with specific dates (same UTC time but different offsets)
		var dto1 = new DateTimeOffset(2020, 1, 1, 12, 0, 0, offset1);
		var dto2 = new DateTimeOffset(2020, 1, 1, 12, 0, 0, offset2);
		var result = op.Add(dto1, dto2);
		// Result uses first operand's offset
		result.Offset.AssertEqual(offset1);
	}

	#endregion

	#region UIntOperator Tests

	[TestMethod]
	public void UIntOperator_BasicOperations()
	{
		var op = new UIntOperator();
		op.Add(2u, 3u).AssertEqual(5u);
		op.Subtract(5u, 3u).AssertEqual(2u);
		op.Multiply(3u, 4u).AssertEqual(12u);
		op.Divide(10u, 2u).AssertEqual(5u);
		(op.Compare(1u, 2u) < 0).AssertTrue();
	}

	#endregion

	#region ULongOperator Tests

	[TestMethod]
	public void ULongOperator_BasicOperations()
	{
		var op = new ULongOperator();
		op.Add(2ul, 3ul).AssertEqual(5ul);
		op.Subtract(5ul, 3ul).AssertEqual(2ul);
		op.Multiply(3ul, 4ul).AssertEqual(12ul);
		op.Divide(10ul, 2ul).AssertEqual(5ul);
		(op.Compare(1ul, 2ul) < 0).AssertTrue();
	}

	#endregion

	#region UShortOperator Tests

	[TestMethod]
	public void UShortOperator_BasicOperations()
	{
		var op = new UShortOperator();
		op.Add((ushort)2, (ushort)3).AssertEqual((ushort)5);
		op.Subtract((ushort)5, (ushort)3).AssertEqual((ushort)2);
		op.Multiply((ushort)3, (ushort)4).AssertEqual((ushort)12);
		op.Divide((ushort)10, (ushort)2).AssertEqual((ushort)5);
		(op.Compare((ushort)1, (ushort)2) < 0).AssertTrue();
	}

	#endregion

	#region SByteOperator Tests

	[TestMethod]
	public void SByteOperator_BasicOperations()
	{
		var op = new SByteOperator();
		op.Add((sbyte)2, (sbyte)3).AssertEqual((sbyte)5);
		op.Subtract((sbyte)5, (sbyte)3).AssertEqual((sbyte)2);
		op.Multiply((sbyte)3, (sbyte)4).AssertEqual((sbyte)12);
		op.Divide((sbyte)10, (sbyte)2).AssertEqual((sbyte)5);
		(op.Compare((sbyte)1, (sbyte)2) < 0).AssertTrue();
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void IntOperator_DivideByZero_Throws()
	{
		var op = new IntOperator();
		ThrowsExactly<DivideByZeroException>(() => op.Divide(10, 0));
	}

	[TestMethod]
	public void DecimalOperator_DivideByZero_Throws()
	{
		var op = new DecimalOperator();
		ThrowsExactly<DivideByZeroException>(() => op.Divide(10m, 0m));
	}

	[TestMethod]
	public void DoubleOperator_DivideByZero_ReturnsInfinity()
	{
		var op = new DoubleOperator();
		var result = op.Divide(10.0, 0.0);
		double.IsPositiveInfinity(result).AssertTrue();
	}

	[TestMethod]
	public void FloatOperator_DivideByZero_ReturnsInfinity()
	{
		var op = new FloatOperator();
		var result = op.Divide(10.0f, 0.0f);
		float.IsPositiveInfinity(result).AssertTrue();
	}

	#endregion
}
