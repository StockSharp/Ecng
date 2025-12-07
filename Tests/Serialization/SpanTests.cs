namespace Ecng.Tests.Serialization;

using System.Runtime.CompilerServices;
using System.Text;

using Ecng.Serialization;

[TestClass]
public class SpanTests : BaseTestClass
{
	private struct TestStruct
	{
		public int X;
		public int Y;
	}

	[TestMethod]
	public void WriteAndReadByte()
	{
		Span<byte> buffer = new byte[1];
		var writer = new SpanWriter(buffer);
		writer.WriteByte(0x42);
		writer.Position.AssertEqual(1);
		writer.IsFull.AssertTrue();

		var reader = new SpanReader(buffer);
		var value = reader.ReadByte();
		value.AssertEqual((byte)0x42);
		reader.Position.AssertEqual(1);
		reader.End.AssertTrue();
	}

	[TestMethod]
	public void WriteAndReadInt16_LittleEndian()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer);
		short value = 0x1234;
		writer.WriteInt16(value);
		writer.Position.AssertEqual(2);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadInt16();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
	}

	[TestMethod]
	public void WriteAndReadInt16_BigEndian()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		short value = 0x1234;
		writer.WriteInt16(value);
		writer.Position.AssertEqual(2);

		var expectedBytes = BitConverter.IsLittleEndian ? [0x12, 0x34] : BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadInt16();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
	}

	[TestMethod]
	public void WriteAndReadInt32_LittleEndian()
	{
		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer);
		int value = 0x12345678;
		writer.WriteInt32(value);
		writer.Position.AssertEqual(4);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadInt32();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(4);
	}

	[TestMethod]
	public void WriteAndReadInt32_BigEndian()
	{
		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		int value = 0x12345678;
		writer.WriteInt32(value);
		writer.Position.AssertEqual(4);

		var expectedBytes = BitConverter.IsLittleEndian ? [0x12, 0x34, 0x56, 0x78] : BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadInt32();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(4);
	}

	[TestMethod]
	public void WriteAndReadInt64_LittleEndian()
	{
		Span<byte> buffer = new byte[8];
		var writer = new SpanWriter(buffer);
		long value = 0x123456789ABCDEF0;
		writer.WriteInt64(value);
		writer.Position.AssertEqual(8);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadInt64();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadInt64_BigEndian()
	{
		Span<byte> buffer = new byte[8];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		long value = 0x123456789ABCDEF0;
		writer.WriteInt64(value);
		writer.Position.AssertEqual(8);

		var expectedBytes = BitConverter.IsLittleEndian ? [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0] : BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadInt64();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadString()
	{
		var encoding = Encoding.UTF8;

		string value = "Hello, World!";
		var bytes = encoding.GetBytes(value);
		Span<byte> buffer = new byte[bytes.Length]; // 4 bytes for length + string bytes

		var writer = new SpanWriter(buffer);
		writer.WriteString(value, encoding);
		writer.Position.AssertEqual(bytes.Length);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadString(bytes.Length, encoding);
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(bytes.Length);

		string value2 = "Hello World!";
		var bytes2 = encoding.GetBytes(value + value2);
		buffer = new byte[bytes2.Length];

		writer = new SpanWriter(buffer);
		writer.WriteString(value, encoding);
		writer.WriteString(value2, encoding);
		writer.Position.AssertEqual(bytes2.Length);

		reader = new SpanReader(buffer);
		readValue = reader.ReadString(bytes.Length, encoding);
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(bytes.Length);

		readValue = reader.ReadString(bytes2.Length - bytes.Length, encoding);
		readValue.AssertEqual(value2);
		reader.Position.AssertEqual(bytes2.Length);
	}

	[TestMethod]
	public void WriteAndReadNullString()
	{
		var encoding = Encoding.UTF8;

		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer);
		writer.WriteString(null, encoding);
		writer.Position.AssertEqual(0);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadString(0, encoding);
		readValue.IsEmpty().AssertTrue();
		reader.Position.AssertEqual(0);
	}

	[TestMethod]
	public void WriteAndReadDecimal()
	{
		Span<byte> buffer = new byte[16];
		decimal value = 123.456m;
		var writer = new SpanWriter(buffer);
		writer.WriteDecimal(value);
		writer.Position.AssertEqual(16);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadDecimal();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(16);
	}

	[TestMethod]
	public void WriteAndReadDateTime()
	{
		Span<byte> buffer = new byte[8];
		DateTime value = new DateTime(2025, 4, 12, 15, 30, 45);
		var writer = new SpanWriter(buffer);
		writer.WriteDateTime(value);
		writer.Position.AssertEqual(8);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadDateTime();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadStruct()
	{
		var sizeOf = Unsafe.SizeOf<TestStruct>();
		Span<byte> buffer = new byte[8];
		var value = new TestStruct { X = RandomGen.GetInt(), Y = RandomGen.GetInt() };
		var writer = new SpanWriter(buffer);
		writer.WriteStruct(value, sizeOf);
		writer.Position.AssertEqual(8);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadStruct<TestStruct>(sizeOf);
		readValue.X.AssertEqual(value.X);
		readValue.Y.AssertEqual(value.Y);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadStructArray()
	{
		var sizeOf = Unsafe.SizeOf<TestStruct>();
		Span<byte> buffer = new byte[16];
		var value = new TestStruct[]
		{
			new() { X = RandomGen.GetInt(), Y = RandomGen.GetInt() },
			new() { X = RandomGen.GetInt(), Y = RandomGen.GetInt() }
		};
		var writer = new SpanWriter(buffer);
		writer.WriteStructArray(value, sizeOf);
		writer.Position.AssertEqual(16);

		var reader = new SpanReader(buffer);
		var readValue = new TestStruct[2];
		reader.ReadStructArray(readValue, sizeOf, 2);
		readValue[0].X.AssertEqual(value[0].X);
		readValue[0].Y.AssertEqual(value[0].Y);
		readValue[1].X.AssertEqual(value[1].X);
		readValue[1].Y.AssertEqual(value[1].Y);
		reader.Position.AssertEqual(16);
	}

	[TestMethod]
	public void SkipBytes()
	{
		Span<byte> buffer = new byte[10];
		var writer = new SpanWriter(buffer);
		writer.Skip(5);
		writer.Position.AssertEqual(5);
		writer.WriteByte(0xFF);
		writer.Position.AssertEqual(6);

		var reader = new SpanReader(buffer);
		reader.Skip(5);
		reader.Position.AssertEqual(5);
		var value = reader.ReadByte();
		value.AssertEqual((byte)0xFF);
		reader.Position.AssertEqual(6);
	}

	[TestMethod]
	public void WriteAndReadSpan()
	{
		Span<byte> buffer = new byte[5];
		var source = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
		var writer = new SpanWriter(buffer);
		writer.WriteSpan(source);
		writer.Position.AssertEqual(5);

		var reader = new SpanReader(buffer);
		var readSpan = reader.ReadSpan(5);
		readSpan.ToArray().AssertEqual(source);
		reader.Position.AssertEqual(5);
	}

	[TestMethod]
	public void WriteAndReadFloat_LittleEndian()
	{
		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer);
		float value = 123.456f;
		writer.WriteSingle(value);
		writer.Position.AssertEqual(4);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadSingle();
		readValue.AssertEqual(value, 0.0001f);
		reader.Position.AssertEqual(4);
	}

	[TestMethod]
	public void WriteAndReadFloat_BigEndian()
	{
		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		float value = 123.456f;
		writer.WriteSingle(value);
		writer.Position.AssertEqual(4);

		var expectedBytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
			Array.Reverse(expectedBytes);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadSingle();
		readValue.AssertEqual(value, 0.0001f);
		reader.Position.AssertEqual(4);
	}

	[TestMethod]
	public void WriteAndReadGuid()
	{
		Span<byte> buffer = new byte[16];
		var value = Guid.NewGuid();
		var writer = new SpanWriter(buffer);
		writer.WriteGuid(value);
		writer.Position.AssertEqual(16);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadGuid();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(16);
	}

	[TestMethod]
	public void WriteAndReadSByte()
	{
		Span<byte> buffer = new byte[1];
		var writer = new SpanWriter(buffer);
		sbyte value = -42;
		writer.WriteSByte(value);
		writer.Position.AssertEqual(1);
		writer.IsFull.AssertTrue();

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadSByte();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(1);
		reader.End.AssertTrue();
	}

	[TestMethod]
	public void WriteAndReadBoolean()
	{
		Span<byte> buffer = new byte[1];
		var writer = new SpanWriter(buffer);
		bool value = true;
		writer.WriteBoolean(value);
		writer.Position.AssertEqual(1);
		writer.IsFull.AssertTrue();

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadBoolean();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(1);
		reader.End.AssertTrue();
	}

	[TestMethod]
	public void WriteAndReadUInt16_LittleEndian()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer);
		ushort value = 0x1234;
		writer.WriteUInt16(value);
		writer.Position.AssertEqual(2);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadUInt16();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
	}

	[TestMethod]
	public void WriteAndReadUInt16_BigEndian()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		ushort value = 0x1234;
		writer.WriteUInt16(value);
		writer.Position.AssertEqual(2);

		var expectedBytes = BitConverter.IsLittleEndian ? [0x12, 0x34] : BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadUInt16();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
	}

	[TestMethod]
	public void WriteAndReadUInt32_LittleEndian()
	{
		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer);
		uint value = 0x12345678;
		writer.WriteUInt32(value);
		writer.Position.AssertEqual(4);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadUInt32();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(4);
	}

	[TestMethod]
	public void WriteAndReadUInt32_BigEndian()
	{
		Span<byte> buffer = new byte[4];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		uint value = 0x12345678;
		writer.WriteUInt32(value);
		writer.Position.AssertEqual(4);

		var expectedBytes = BitConverter.IsLittleEndian ? [0x12, 0x34, 0x56, 0x78] : BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadUInt32();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(4);
	}

	[TestMethod]
	public void WriteAndReadUInt64_LittleEndian()
	{
		Span<byte> buffer = new byte[8];
		var writer = new SpanWriter(buffer);
		ulong value = 0x123456789ABCDEF0;
		writer.WriteUInt64(value);
		writer.Position.AssertEqual(8);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadUInt64();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadUInt64_BigEndian()
	{
		Span<byte> buffer = new byte[8];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		ulong value = 0x123456789ABCDEF0;
		writer.WriteUInt64(value);
		writer.Position.AssertEqual(8);

		var expectedBytes = BitConverter.IsLittleEndian ? [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0] : BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadUInt64();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadChar()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer);
		char value = 'A';
		writer.WriteChar(value);
		writer.Position.AssertEqual(2);
		writer.IsFull.AssertTrue();

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadChar();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
		reader.End.AssertTrue();
	}

	[TestMethod]
	public void WriteAndReadHalf_LittleEndian()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer);
		var value = (Half)123.456f;
		writer.WriteHalf(value);
		writer.Position.AssertEqual(2);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadHalf();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
	}

	[TestMethod]
	public void WriteAndReadHalf_BigEndian()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		var value = (Half)123.456f;
		writer.WriteHalf(value);
		writer.Position.AssertEqual(2);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadHalf();
		readValue.AssertEqual(value);
		reader.Position.AssertEqual(2);
	}

	[TestMethod]
	public void WriteAndReadDouble_LittleEndian()
	{
		Span<byte> buffer = new byte[8];
		var writer = new SpanWriter(buffer);
		double value = 123.456789;
		writer.WriteDouble(value);
		writer.Position.AssertEqual(8);

		var expectedBytes = BitConverter.GetBytes(value);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer);
		var readValue = reader.ReadDouble();
		readValue.AssertEqual(value, 0.0000001);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void WriteAndReadDouble_BigEndian()
	{
		Span<byte> buffer = new byte[8];
		var writer = new SpanWriter(buffer, isBigEndian: true);
		double value = 123.456789;
		writer.WriteDouble(value);
		writer.Position.AssertEqual(8);

		var expectedBytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
			Array.Reverse(expectedBytes);
		buffer.ToArray().AssertEqual(expectedBytes);

		var reader = new SpanReader(buffer, isBigEndian: true);
		var readValue = reader.ReadDouble();
		readValue.AssertEqual(value, 0.0000001);
		reader.Position.AssertEqual(8);
	}

	[TestMethod]
	public void Skip_Negative_NotAllowed()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer);
		try
		{
			writer.Skip(-1);
			Fail("Expected ArgumentOutOfRangeException for writer.Skip(-1)");
		}
		catch (ArgumentOutOfRangeException)
		{
			// expected
		}

		var reader = new SpanReader(buffer);
		try
		{
			reader.Skip(-1);
			Fail("Expected ArgumentOutOfRangeException for reader.Skip(-1)");
		}
		catch (ArgumentOutOfRangeException)
		{
			// expected
		}
	}

	[TestMethod]
	public void Skip_Negative_Allowed_Writer()
	{
		Span<byte> buffer = new byte[3];
		var writer = new SpanWriter(buffer, isBigEndian: false, allowNegativeShift: true);
		writer.WriteByte(0xAA);
		writer.WriteByte(0xBB);
		writer.Position.AssertEqual(2);
		writer.Skip(-1); // move back to overwrite 0xBB
		writer.Position.AssertEqual(1);
		writer.WriteByte(0xCC);
		writer.Position.AssertEqual(2);

		buffer[0].AssertEqual((byte)0xAA);
		buffer[1].AssertEqual((byte)0xCC);
	}

	[TestMethod]
	public void Skip_Negative_Allowed_Reader()
	{
		Span<byte> buffer = [0x11, 0x22];
		var reader = new SpanReader(buffer, isBigEndian: false, allowNegativeShift: true);
		var b1 = reader.ReadByte();
		b1.AssertEqual((byte)0x11);
		reader.Position.AssertEqual(1);
		reader.Skip(-1);
		reader.Position.AssertEqual(0);
		var b2 = reader.ReadByte();
		b2.AssertEqual((byte)0x11);
		reader.Position.AssertEqual(1);
	}

	[TestMethod]
	public void Skip_Bounds_Writer()
	{
		Span<byte> buffer = new byte[2];
		var writer = new SpanWriter(buffer);

		try
		{
			writer.Skip(3);
			Fail("Expected ArgumentOutOfRangeException for writer.Skip(3)");
		}
		catch (ArgumentOutOfRangeException)
		{
			// expected
		}

		writer = new SpanWriter(buffer, isBigEndian: false, allowNegativeShift: true);
		writer.Skip(1);
		try
		{
			writer.Skip(-2); // would go to -1
			Fail("Expected ArgumentOutOfRangeException for writer.Skip(-2)");
		}
		catch (ArgumentOutOfRangeException)
		{
			// expected
		}
	}

	[TestMethod]
	public void Skip_Bounds_Reader()
	{
		Span<byte> buffer = new byte[2];
		var reader = new SpanReader(buffer);
		try
		{
			reader.Skip(3);
			Fail("Expected ArgumentOutOfRangeException for reader.Skip(3)");
		}
		catch (ArgumentOutOfRangeException)
		{
			// expected
		}

		reader = new SpanReader(buffer, isBigEndian: false, allowNegativeShift: true);
		reader.Skip(1);
		try
		{
			reader.Skip(-2);
			Fail("Expected ArgumentOutOfRangeException for reader.Skip(-2)");
		}
		catch (ArgumentOutOfRangeException)
		{
			// expected
		}
	}
}