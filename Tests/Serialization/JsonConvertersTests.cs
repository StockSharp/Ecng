namespace Ecng.Tests.Serialization;

using Newtonsoft.Json;
using System.Globalization;
using Ecng.Serialization;

[TestClass]
public class JsonConvertersTests
{
	[TestMethod]
	public void BoolConverter_ReadWrite()
	{
		var converter = new JsonBoolConverter();
		var serializer = JsonSerializer.CreateDefault();

		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, true, serializer);
		writer.Flush();
		sb.ToString().AssertEqual("1");

		converter.CanConvert(typeof(bool)).AssertTrue();
		converter.CanConvert(typeof(int)).AssertFalse();

		var reader = new JsonTextReader(new StringReader("0"));
		reader.Read();
		((bool)converter.ReadJson(reader, typeof(bool), null, serializer)).AssertFalse();
	}

	[TestMethod]
	public void DateTimeConverter_ReadSeconds()
	{
		var converter = new JsonDateTimeConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 10.0;

		var reader = new JsonTextReader(new StringReader(value.ToString(CultureInfo.InvariantCulture)));
		reader.Read();
		var dt = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt.AssertEqual(TimeHelper.GregorianStart.AddSeconds(value));

		Assert.ThrowsException<NotSupportedException>(() => converter.WriteJson(new JsonTextWriter(TextWriter.Null), dt, serializer));
	}

	[TestMethod]
	public void DateTimeMlsConverter_ReadMilliseconds()
	{
		var converter = new JsonDateTimeMlsConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 1500.0;

		var reader = new JsonTextReader(new StringReader(value.ToString(CultureInfo.InvariantCulture)));
		reader.Read();
		var dt = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt.AssertEqual(TimeHelper.GregorianStart.AddMilliseconds(value));
	}

	[TestMethod]
	public void DateTimeMcsConverter_ReadMicroseconds()
	{
		var converter = new JsonDateTimeMcsConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 1500.0;

		var reader = new JsonTextReader(new StringReader(value.ToString(CultureInfo.InvariantCulture)));
		reader.Read();
		var dt = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt.AssertEqual(TimeHelper.GregorianStart.AddMicroseconds((long)value));
	}

	[TestMethod]
	public void DateTimeNanoConverter_ReadNanoseconds()
	{
		var converter = new JsonDateTimeNanoConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 2000.0;

		var reader = new JsonTextReader(new StringReader(value.ToString(CultureInfo.InvariantCulture)));
		reader.Read();
		var dt = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt.AssertEqual(TimeHelper.GregorianStart.AddNanoseconds((long)value));
	}

	[TestMethod]
	public void JArrayConverter_WriteNotSupported()
	{
		var c1 = new JArrayToObjectConverter();
		c1.CanWrite.AssertFalse();
		Assert.ThrowsException<NotSupportedException>(() => c1.WriteJson(new JsonTextWriter(TextWriter.Null), new(), JsonSerializer.CreateDefault()));

		var c2 = new JArrayToObjectConverter<JArrayConvTest>();
		c2.CanWrite.AssertFalse();
		Assert.ThrowsException<NotSupportedException>(() => c2.WriteJson(new JsonTextWriter(TextWriter.Null), new(), JsonSerializer.CreateDefault()));
	}

	
	private class JArrayConvTest
	{
		public int X;
		public string Y;
	}
}
