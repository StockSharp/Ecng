namespace Ecng.Tests.Serialization;

using System.Globalization;

using Ecng.Serialization;

using Newtonsoft.Json;

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

		Assert.ThrowsExactly<NotSupportedException>(() => converter.WriteJson(new JsonTextWriter(TextWriter.Null), dt, serializer));
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
	public void JArrayConverter_Write()
	{
		var c1 = new JArrayToObjectConverter();
		c1.CanWrite.AssertFalse();
		Assert.ThrowsExactly<NotSupportedException>(() => c1.WriteJson(new JsonTextWriter(TextWriter.Null), new(), JsonSerializer.CreateDefault()));

		var c2 = new JArrayToObjectConverter<JArrayConvTest>();
		c2.CanWrite.AssertFalse();
		Assert.ThrowsExactly<NotSupportedException>(() => c2.WriteJson(new JsonTextWriter(TextWriter.Null), new(), JsonSerializer.CreateDefault()));
	}

	[TestMethod]
	public void JArrayConverter_Read_Object()
	{
		var json = "[123,\"abc\"]";
		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var converter = new JArrayToObjectConverter();
		var serializer = JsonSerializer.CreateDefault();
		var obj = (JArrayConvTest)converter.ReadJson(reader, typeof(JArrayConvTest), null, serializer);
		obj.X.AssertEqual(123);
		obj.Y.AssertEqual("abc");
	}

	[TestMethod]
	public void JArrayConverter_Read_Generic()
	{
		var json = "[456,\"def\"]";
		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var converter = new JArrayToObjectConverter<JArrayConvTest>();
		var serializer = JsonSerializer.CreateDefault();
		var obj = (JArrayConvTest)converter.ReadJson(reader, typeof(JArrayConvTest), null, serializer);
		obj.X.AssertEqual(456);
		obj.Y.AssertEqual("def");
	}

	[TestMethod]
	public void BoolConverter_InvalidValues()
	{
		var converter = new JsonBoolConverter();
		var serializer = JsonSerializer.CreateDefault();
		foreach (var val in new[] { "2", "true", "false", "", null })
		{
			var reader = new JsonTextReader(new StringReader(val ?? ""));
			reader.Read();
			((bool)converter.ReadJson(reader, typeof(bool), null, serializer)).AssertFalse();
		}
	}

	[TestMethod]
	public void DateTimeConverters_InvalidValues()
	{
		var dtConverters = new JsonConverter[]
		{
			new JsonDateTimeConverter(),
			new JsonDateTimeMlsConverter(),
			new JsonDateTimeMcsConverter(),
			new JsonDateTimeNanoConverter(),
		};
		var serializer = JsonSerializer.CreateDefault();
		foreach (var converter in dtConverters)
		{
			// null value
			var readerNull = new JsonTextReader(new StringReader(""));
			readerNull.Read();
			converter.ReadJson(readerNull, typeof(DateTime), null, serializer).AssertNull();

			// string value
			var readerStr = new JsonTextReader(new StringReader("\"notanumber\""));
			readerStr.Read();
			Assert.Throws<JsonReaderException>(() => converter.ReadJson(readerStr, typeof(DateTime), null, serializer));

			// zero value
			var readerZero = new JsonTextReader(new StringReader("0"));
			readerZero.Read();
			converter.ReadJson(readerZero, typeof(DateTime), null, serializer).AssertNull();
		}
	}

	[TestMethod]
	public void DateTimeConverters_Write()
	{
		var converters = new JsonConverter[]
		{
			new JsonDateTimeConverter(),
			new JsonDateTimeMlsConverter(),
			new JsonDateTimeMcsConverter(),
			new JsonDateTimeNanoConverter(),
		};

		foreach (var converter in converters)
		{
			converter.CanConvert(typeof(DateTime)).AssertTrue();
			converter.CanConvert(typeof(string)).AssertFalse();
			converter.CanConvert(typeof(int)).AssertFalse();
		}

		var dt = DateTime.UtcNow;
		foreach (var converter in converters)
		{
			var serializer = JsonSerializer.CreateDefault();
			var writer = new JsonTextWriter(TextWriter.Null);
			Assert.ThrowsExactly<NotSupportedException>(() => converter.WriteJson(writer, dt, serializer));
		}
	}

	private class JArrayConvTest
	{
		public int X { get; set; }
		public string Y { get; set; }
	}
}