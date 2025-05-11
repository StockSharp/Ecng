namespace Ecng.Tests.Serialization;

using System.Runtime.CompilerServices;
using System.Text;

using Ecng.Serialization;

[TestClass]
public class SpanTests
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
}