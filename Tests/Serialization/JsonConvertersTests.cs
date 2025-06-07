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

		var reader = new JsonTextReader(new StringReader(sb.ToString()));
		reader.Read();
		((bool)converter.ReadJson(reader, typeof(bool), null, serializer)).AssertTrue();

		converter.CanConvert(typeof(bool)).AssertTrue();
		converter.CanConvert(typeof(int)).AssertFalse();
	}

	[TestMethod]
	public void DateTimeSecConverter_ReadWrite()
	{
		var converter = new JsonDateTimeConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 10.0;
		var dt = TimeHelper.GregorianStart.AddSeconds(value);

		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, dt, serializer);
		writer.Flush();
		var json = sb.ToString();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var dt2 = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt2.AssertEqual(dt);
	}

	[TestMethod]
	public void DateTimeMlsConverter_ReadWrite()
	{
		var converter = new JsonDateTimeMlsConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 1500.0;
		var dt = TimeHelper.GregorianStart.AddMilliseconds(value);

		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, dt, serializer);
		writer.Flush();
		var json = sb.ToString();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var dt2 = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt2.AssertEqual(dt);
	}

	[TestMethod]
	public void DateTimeMcsConverter_ReadWrite()
	{
		var converter = new JsonDateTimeMcsConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 1500L;
		var dt = TimeHelper.GregorianStart.AddMicroseconds(value);

		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, dt, serializer);
		writer.Flush();
		var json = sb.ToString();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var dt2 = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt2.AssertEqual(dt);
	}

	[TestMethod]
	public void DateTimeNanoConverter_ReadWrite()
	{
		var converter = new JsonDateTimeNanoConverter();
		var serializer = JsonSerializer.CreateDefault();
		var value = 2000L;
		var dt = TimeHelper.GregorianStart.AddNanoseconds(value);

		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, dt, serializer);
		writer.Flush();
		var json = sb.ToString();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var dt2 = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, serializer);
		dt2.AssertEqual(dt);
	}

	[TestMethod]
	public void JArrayConverter_WriteRead_Object()
	{
		var obj = new JArrayConvTest { X = 123, Y = "abc" };
		var converter = new JArrayToObjectConverter();
		var serializer = JsonSerializer.CreateDefault();
		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, obj, serializer);
		writer.Flush();
		var json = sb.ToString();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var obj2 = (JArrayConvTest)converter.ReadJson(reader, typeof(JArrayConvTest), null, serializer);
		obj2.X.AssertEqual(obj.X);
		obj2.Y.AssertEqual(obj.Y);
	}

	[TestMethod]
	public void JArrayConverter_WriteRead_Generic()
	{
		var obj = new JArrayConvTest { X = 456, Y = "def" };
		var converter = new JArrayToObjectConverter<JArrayConvTest>();
		var serializer = JsonSerializer.CreateDefault();
		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, obj, serializer);
		writer.Flush();
		var json = sb.ToString();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		var obj2 = (JArrayConvTest)converter.ReadJson(reader, typeof(JArrayConvTest), null, serializer);
		obj2.X.AssertEqual(obj.X);
		obj2.Y.AssertEqual(obj.Y);
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

	private class JArrayConvTest
	{
		public int X { get; set; }
		public string Y { get; set; }
	}
}