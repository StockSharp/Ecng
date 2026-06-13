namespace Ecng.Tests.Serialization;

using Ecng.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[TestClass]
public class JsonConvertersTests : BaseTestClass
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
	[DataRow(0.001)]
	[DataRow(0.5)]
	[DataRow(0.999)]
	public void DateTimeSecConverter_ReadFractionalUnixTimestamp(double seconds)
	{
		var settings = new JsonSerializerSettings();
		settings.Converters.Add(new JsonDateTimeConverter());

		var value = JsonConvert.DeserializeObject<DateTime>(seconds.ToString(CultureInfo.InvariantCulture), settings);

		value.AssertEqual(seconds.FromUnix());
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

	private class GlobalTimeDto
	{
		public DateTime Required { get; set; }
		public DateTime? Optional { get; set; }
	}

	[TestMethod]
	public void DateTimeMcsConverter_GlobalRegistration_HandlesNullable()
	{
		var settings = new JsonSerializerSettings();
		settings.Converters.Add(new JsonDateTimeMcsConverter());

		var t = TimeHelper.GregorianStart.AddMicroseconds(1_700_000_000_000_000L);

		// registered globally (Converters list, not a per-field attribute):
		// must cover both DateTime and DateTime?
		var json = JsonConvert.SerializeObject(new GlobalTimeDto { Required = t, Optional = t }, settings);
		var back = JsonConvert.DeserializeObject<GlobalTimeDto>(json, settings);
		back.Required.ToUnixMcs().AssertEqual(t.ToUnixMcs());
		back.Optional.Value.ToUnixMcs().AssertEqual(t.ToUnixMcs());

		// null DateTime? — no throw, round-trips to null
		var jsonNull = JsonConvert.SerializeObject(new GlobalTimeDto { Required = t, Optional = null }, settings);
		JsonConvert.DeserializeObject<GlobalTimeDto>(jsonNull, settings).Optional.HasValue.AssertEqual(false);
	}

	private static string Write(JsonConverter converter, object value)
	{
		var sb = new StringWriter();
		var writer = new JsonTextWriter(sb);
		converter.WriteJson(writer, value, JsonSerializer.CreateDefault());
		writer.Flush();
		return sb.ToString();
	}

	[TestMethod]
	public void DateTime_WriteDefault_DoesNotThrow_WritesZero()
	{
		// default(DateTime) is year 1, before the unix epoch. A wire serializer must not crash
		// on an unset optional time; it must emit the same "no value" sentinel (0) that ReadJson maps back to null.
		Write(new JsonDateTimeConverter(), default(DateTime)).AssertEqual("0");
		Write(new JsonDateTimeMlsConverter(), default(DateTime)).AssertEqual("0");
		Write(new JsonDateTimeMcsConverter(), default(DateTime)).AssertEqual("0");
		Write(new JsonDateTimeNanoConverter(), default(DateTime)).AssertEqual("0");
	}

	[TestMethod]
	public void DateTime_WritePreEpoch_DoesNotThrow_WritesZero()
	{
		var preEpoch = new DateTime(1965, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		Write(new JsonDateTimeConverter(), preEpoch).AssertEqual("0");
		Write(new JsonDateTimeMlsConverter(), preEpoch).AssertEqual("0");
		Write(new JsonDateTimeMcsConverter(), preEpoch).AssertEqual("0");
		Write(new JsonDateTimeNanoConverter(), preEpoch).AssertEqual("0");
	}

	[TestMethod]
	public void DateTimeMcsConverter_GlobalRegistration_DefaultDateTime_RoundTrips()
	{
		var settings = new JsonSerializerSettings();
		settings.Converters.Add(new JsonDateTimeMcsConverter());

		// Required is default(DateTime) (unset), Optional is null — serialization must not throw,
		// and the unset values must round-trip back to their defaults.
		var json = JsonConvert.SerializeObject(new GlobalTimeDto { Required = default, Optional = null }, settings);
		var back = JsonConvert.DeserializeObject<GlobalTimeDto>(json, settings);
		back.Required.AssertEqual(default(DateTime));
		back.Optional.HasValue.AssertEqual(false);
	}

	[TestMethod]
	public void DateTimeMcsConverter_ReadIsoString_ParsesAsDate()
	{
		// Clients may send an ISO-8601 string instead of a numeric unix timestamp.
		// Newtonsoft auto-parses such a string into a DateTime token; the converter must use it
		// directly instead of coercing it to a number (which previously produced 1970-01-01).
		var converter = new JsonDateTimeMcsConverter();
		var expected = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		var reader = new JsonTextReader(new StringReader("\"2025-02-01T00:00:00Z\""));
		reader.Read();

		var dt = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, JsonSerializer.CreateDefault());
		dt.ToUniversalTime().AssertEqual(expected);
	}

	[TestMethod]
	public void DateTimeMcsConverter_RoundTrip_DateString_PreservesValue()
	{
		var settings = new JsonSerializerSettings();
		settings.Converters.Add(new JsonDateTimeMcsConverter());

		var from = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		// Wire payload arriving as an ISO string (as a WS subscribe client may send).
		var json = "{\"Required\":\"2025-01-15T00:00:00Z\",\"Optional\":null}";
		var back = JsonConvert.DeserializeObject<GlobalTimeDto>(json, settings);
		back.Required.ToUniversalTime().AssertEqual(from);
	}

	[TestMethod]
	public void DateTimeMcsConverter_ReadIsoStringWithoutZone_AssumesUtc()
	{
		// An ISO-8601 string without a zone designator must be read as UTC (the wire is all-UTC),
		// not shifted by the server's local offset. Newtonsoft tokenises it into a DateTime of
		// Kind=Unspecified; the converter must treat that as UTC — consistent with the textual-date
		// path (which uses AssumeUniversal) — instead of assuming local time.
		var converter = new JsonDateTimeMcsConverter();
		var expected = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		var reader = new JsonTextReader(new StringReader("\"2025-02-01T00:00:00\""));
		reader.Read();

		var dt = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, JsonSerializer.CreateDefault());
		dt.AssertEqual(expected);
		dt.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void DateTimeMcsConverter_ReadDateToken_NormalizesEveryKind()
	{
		// The converter accepts a DateTime token of any Kind (a JValue carries the Kind verbatim).
		// All three must yield the correct UTC instant: Utc unchanged, Local converted, and
		// Unspecified taken AS UTC (not assumed local — the wire is all-UTC).
		var converter = new JsonDateTimeMcsConverter();

		DateTime ReadToken(DateTime token)
		{
			var reader = new JTokenReader(new JValue(token));
			reader.Read();
			return (DateTime)converter.ReadJson(reader, typeof(DateTime), null, JsonSerializer.CreateDefault());
		}

		// Utc -> unchanged.
		var utc = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		ReadToken(utc).AssertEqual(utc);

		// Local -> converted to its UTC instant (machine-independent: same conversion both sides).
		var local = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Local);
		ReadToken(local).AssertEqual(local.ToUniversalTime());

		// Unspecified -> same wall-clock taken as UTC (no local-offset shift).
		var unspecified = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Unspecified);
		ReadToken(unspecified).AssertEqual(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc));
	}

	/// <summary>
	/// Regression test for the DateTime write side: ensures an Unspecified-Kind DateTime is treated
	/// AS UTC (no local-offset shift), so its wire value equals the explicit-UTC equivalent and
	/// round-trips back unchanged — consistent with the read side. (Was: WriteJson normalized
	/// Unspecified through ToUniversalTime, shifting it by the machine local offset; see
	/// JsonDateTimeConverter.cs:109 (TryWriteSentinel) and :148 (ToUnix), both now via NormalizeUtc.)
	/// </summary>
	[TestMethod]
	public void WriteJson_UnspecifiedDateTime_TreatedAsUtc_RoundTrips()
	{
		var converter = new JsonDateTimeMcsConverter();

		// Unspecified wall-clock that, taken AS UTC, is the intended instant.
		var unspecified = new DateTime(2025, 2, 1, 12, 30, 0, DateTimeKind.Unspecified);
		var asUtc = DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);

		// The wire value for the Unspecified token must equal the wire value for the explicit-UTC
		// equivalent (machine-independent identity); the formerly buggy code shifted the Unspecified one.
		Write(converter, unspecified).AssertEqual(Write(converter, asUtc));

		// And it must round-trip back to the same UTC instant.
		var reader = new JsonTextReader(new StringReader(Write(converter, unspecified)));
		reader.Read();
		var back = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, JsonSerializer.CreateDefault());
		back.AssertEqual(asUtc);
	}

	/// <summary>
	/// Regression test for the nano DateTime write side: ensures JsonDateTimeNanoConverter.WriteJson
	/// treats an Unspecified-Kind DateTime AS UTC, producing the same wire value as the explicit-UTC
	/// equivalent and round-tripping unchanged. (Was: it shifted Unspecified by the machine local
	/// offset via dt.ToUniversalTime() at JsonDateTimeConverter.cs:250, now via NormalizeUtc.)
	/// </summary>
	[TestMethod]
	public void WriteJson_NanoUnspecifiedDateTime_TreatedAsUtc_RoundTrips()
	{
		var converter = new JsonDateTimeNanoConverter();

		var unspecified = new DateTime(2025, 2, 1, 12, 30, 0, DateTimeKind.Unspecified);
		var asUtc = DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);

		Write(converter, unspecified).AssertEqual(Write(converter, asUtc));

		var reader = new JsonTextReader(new StringReader(Write(converter, unspecified)));
		reader.Read();
		var back = (DateTime)converter.ReadJson(reader, typeof(DateTime), null, JsonSerializer.CreateDefault());
		back.AssertEqual(asUtc);
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
	public void JArrayConverter_ReadNull_ReturnsNull()
	{
		var converter = new JArrayToObjectConverter();
		var reader = new JsonTextReader(new StringReader("null"));
		reader.Read();

		converter.ReadJson(reader, typeof(JArrayConvTest), null, JsonSerializer.CreateDefault()).AssertNull();
	}

	[TestMethod]
	public void JArrayConverter_Generic_ReadNull_ReturnsNull()
	{
		var converter = new JArrayToObjectConverter<JArrayConvTest>();
		var reader = new JsonTextReader(new StringReader("null"));
		reader.Read();

		converter.ReadJson(reader, typeof(JArrayConvTest), null, JsonSerializer.CreateDefault()).AssertNull();
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

		object Read(JsonConverter converter, string json, Type objectType)
		{
			var reader = new JsonTextReader(new StringReader(json));
			reader.Read();
			return converter.ReadJson(reader, objectType, null, serializer);
		}

		foreach (var converter in dtConverters)
		{
			// null / zero with a nullable target -> null
			Read(converter, "", typeof(DateTime?)).AssertNull();
			Read(converter, "0", typeof(DateTime?)).AssertNull();

			// null / zero with a non-nullable target -> default(DateTime); returning null here
			// makes Newtonsoft fail with NullReferenceException assigning into the value-type member.
			((DateTime)Read(converter, "", typeof(DateTime))).AssertEqual(default);
			((DateTime)Read(converter, "0", typeof(DateTime))).AssertEqual(default);

			// non-numeric, non-date string -> error
			Throws<JsonReaderException>(() => Read(converter, "\"notanumber\"", typeof(DateTime)));
		}
	}

	[TestMethod]
	public void JArrayConverter_ShortArray_ThrowsDescriptiveError()
	{
		var json = "[42]"; // JArrayConvTest has 2 fields (X, Y), but array has only 1 element
		var converter = new JArrayToObjectConverter();
		var serializer = JsonSerializer.CreateDefault();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		ThrowsExactly<JsonSerializationException>(() =>
			converter.ReadJson(reader, typeof(JArrayConvTest), null, serializer));
	}

	[TestMethod]
	public void JArrayConverter_Generic_ShortArray_ThrowsDescriptiveError()
	{
		var json = "[42]";
		var converter = new JArrayToObjectConverter<JArrayConvTest>();
		var serializer = JsonSerializer.CreateDefault();

		var reader = new JsonTextReader(new StringReader(json));
		reader.Read();
		ThrowsExactly<JsonSerializationException>(() =>
			converter.ReadJson(reader, typeof(JArrayConvTest), null, serializer));
	}

	private class JArrayConvTest
	{
		public int X { get; set; }
		public string Y { get; set; }
	}
}
