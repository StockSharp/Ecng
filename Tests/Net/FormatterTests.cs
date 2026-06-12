namespace Ecng.Tests.Net;

using System.Net.Http.Formatting;

using Ecng.Net;
using Ecng.Serialization;
using Ecng.Tests.Net.Mocks;

using Newtonsoft.Json;

using JsonFormatter = Ecng.Net.JsonMediaTypeFormatter;

[TestClass]
public class FormatterTests : BaseTestClass
{
	#region helpers

	/// <summary>
	/// Serialize via old Microsoft ObjectContent path (how RestBaseApiClient used to work).
	/// </summary>
	private static async Task<string> SerializeLegacy(MediaTypeFormatter formatter, object value)
	{
		using var content = new ObjectContent<object>(value, formatter);
		return await content.ReadAsStringAsync();
	}

	/// <summary>
	/// Serialize via new IMediaTypeFormatter.Serialize path.
	/// </summary>
	private static async Task<string> SerializeNew(IMediaTypeFormatter formatter, object value)
	{
		using var content = formatter.Serialize(value);
		return await content.ReadAsStringAsync();
	}

	/// <summary>
	/// Deserialize via old Microsoft ReadAsAsync path (how RestBaseApiClient used to work).
	/// </summary>
	private static async Task<T> DeserializeLegacy<T>(MediaTypeFormatter formatter, string text, string mediaType)
	{
		using var content = new StringContent(text, Encoding.UTF8, mediaType);
		return await content.ReadAsAsync<T>([formatter]);
	}

	/// <summary>
	/// Deserialize via new IMediaTypeFormatter.DeserializeAsync path.
	/// </summary>
	private static async Task<T> DeserializeNew<T>(IMediaTypeFormatter formatter, string text, string mediaType)
	{
		using var content = new StringContent(text, Encoding.UTF8, mediaType);
		return await formatter.DeserializeAsync<T>(content, default);
	}

	#endregion

	#region Newtonsoft formatter

	private class TimeDto
	{
		public DateTime Time { get; set; }
	}

	[TestMethod]
	public async Task Newtonsoft_RoundTrips_Poco()
	{
		var f = new NewtonsoftMediaTypeFormatter();

		var dto = new TimeDto { Time = new DateTime(2026, 5, 31, 10, 0, 0, DateTimeKind.Utc) };

		var json = await SerializeNew(f, dto);
		var back = await DeserializeNew<TimeDto>(f, json, "application/json");

		back.Time.AssertEqual(dto.Time);
	}

	[TestMethod]
	public async Task Newtonsoft_DateTime_As_UnixMicroseconds()
	{
		var settings = new JsonSerializerSettings();
		settings.Converters.Add(new JsonDateTimeMcsConverter());

		var f = new NewtonsoftMediaTypeFormatter(settings);

		// sub-millisecond time to prove microsecond precision survives
		var t = new DateTime(2026, 5, 31, 10, 0, 0, DateTimeKind.Utc).AddTicks(4560);

		var json = await SerializeNew(f, new TimeDto { Time = t });

		// the wire carries a bare microsecond number, not an ISO date string
		json.Contains(t.ToUnixMcs().ToString()).AssertEqual(true);

		var back = await DeserializeNew<TimeDto>(f, json, "application/json");
		back.Time.ToUnixMcs().AssertEqual(t.ToUnixMcs());
	}

	private class OptTimeDto
	{
		[JsonConverter(typeof(JsonDateTimeMcsConverter))]
		public DateTime? OptTime { get; set; }
	}

	[TestMethod]
	public async Task Newtonsoft_NullableDateTime_RoundTrips()
	{
		// per-field [JsonConverter] on a DateTime? with null-ignoring settings —
		// the exact shape the WS DTOs use for optional time fields.
		var f = new NewtonsoftMediaTypeFormatter(new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
		});

		// non-null → microsecond number, round-trips
		var t = new DateTime(2026, 5, 31, 10, 0, 0, DateTimeKind.Utc).AddTicks(4560);
		var json = await SerializeNew(f, new OptTimeDto { OptTime = t });
		json.Contains(t.ToUnixMcs().ToString()).AssertEqual(true);
		(await DeserializeNew<OptTimeDto>(f, json, "application/json")).OptTime.Value.ToUnixMcs().AssertEqual(t.ToUnixMcs());

		// null → omitted on write, null on read (no throw)
		var jsonNull = await SerializeNew(f, new OptTimeDto { OptTime = null });
		jsonNull.Contains("OptTime").AssertEqual(false);
		(await DeserializeNew<OptTimeDto>(f, jsonNull, "application/json")).OptTime.HasValue.AssertEqual(false);
	}

	private record PositionalDto(long Id, string Name, DateTime Stamp);

	[TestMethod]
	public async Task Newtonsoft_RoundTrips_PositionalRecord_WithMcsTime()
	{
		// The codegen client deserializes response DTOs — some of which are
		// positional records (e.g. the Designer catalog DTOs) — via Newtonsoft
		// now. Prove Newtonsoft binds the positional constructor AND that a
		// DateTime member rides as a Unix-microsecond number through the very
		// same formatter the client uses.
		var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
		settings.Converters.Add(new JsonDateTimeMcsConverter());
		var f = new NewtonsoftMediaTypeFormatter(settings);

		var dto = new PositionalDto(42, "comp", new DateTime(2026, 5, 31, 10, 0, 0, DateTimeKind.Utc).AddTicks(4560));

		var json = await SerializeNew(f, dto);
		json.Contains(dto.Stamp.ToUnixMcs().ToString()).AssertEqual(true);

		var back = await DeserializeNew<PositionalDto>(f, json, "application/json");
		back.Id.AssertEqual(dto.Id);
		back.Name.AssertEqual(dto.Name);
		back.Stamp.ToUnixMcs().AssertEqual(dto.Stamp.ToUnixMcs());
	}

	#endregion

	#region FormUrlEncoded — side-by-side serialization

	[TestMethod]
	public async Task FormUrlEncoded_Dict_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>
		{
			{ "key1", "value1" },
			{ "key2", 42 },
		};

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_SpecialChars_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>
		{
			{ "q", "hello world" },
			{ "tag", "c#" },
		};

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_NullValue_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>
		{
			{ "key1", "value1" },
			{ "key2", null },
		};

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_Empty_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>();

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_SingleEntry_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>
		{
			{ "only", "one" },
		};

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_Cyrillic_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>
		{
			{ "name", "тест" },
			{ "city", "Москва" },
		};

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_String_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		const string body = "foo=bar&baz=1";

		var expected = await SerializeLegacy(legacy, body);
		var actual = await SerializeNew(current, body);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_ManyEntries_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		var dict = new Dictionary<string, object>
		{
			{ "a", "1" },
			{ "b", "2" },
			{ "c", "3" },
			{ "d", "hello&world" },
			{ "e", "test=value" },
		};

		var expected = await SerializeLegacy(legacy, dict);
		var actual = await SerializeNew(current, dict);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task FormUrlEncoded_Dict_FormatsValuesInvariantly()
	{
		var oldCulture = Thread.CurrentThread.CurrentCulture;

		try
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

			var actual = await SerializeNew(
				new RestApiFormUrlEncodedMediaTypeFormatter(),
				new Dictionary<string, object> { ["price"] = 1.5m });

			actual.AssertEqual("price=1.5");
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = oldCulture;
		}
	}

	[TestMethod]
	public void FormUrlEncoded_MediaType_SameAsLegacy()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();
		var current = new RestApiFormUrlEncodedMediaTypeFormatter();

		current.MediaType.AssertEqual(legacy.SupportedMediaTypes.First().MediaType);
	}

	/// <summary>
	/// The old code had a fallback: base.WriteToStreamAsync for non-dict/non-string values.
	/// TryFormat in RestBaseApiClient converts enum/bool to long, so long is the only
	/// realistic non-dict/non-string type. This test proves the legacy base also threw
	/// NotSupportedException, so our new code preserves the same error contract.
	/// </summary>
	[TestMethod]
	public void FormUrlEncoded_Long_BothThrow()
	{
		var legacy = new LegacyFormUrlEncodedFormatter();

		// ObjectContent calls WriteToStreamAsync → base.WriteToStreamAsync
		// which is FormUrlEncodedMediaTypeFormatter → MediaTypeFormatter
		// that throws because it can't serialize a boxed long to form-urlencoded
		ThrowsExactly<NotSupportedException>(() =>
		{
			using var content = new ObjectContent<object>(42L, legacy);
			// ReadAsStringAsync triggers actual serialization
			content.ReadAsStringAsync().GetAwaiter().GetResult();
		});

		// New implementation also throws NotSupportedException — same contract
		ThrowsExactly<NotSupportedException>(() =>
		{
			new RestApiFormUrlEncodedMediaTypeFormatter().Serialize(42L);
		});
	}

	[TestMethod]
	public void FormUrlEncoded_Serialize_UnsupportedType_Throws()
	{
		ThrowsExactly<NotSupportedException>(() =>
		{
			new RestApiFormUrlEncodedMediaTypeFormatter().Serialize(12345);
		});
	}

	[TestMethod]
	public Task FormUrlEncoded_Deserialize_Throws()
	{
		return ThrowsExactlyAsync<NotSupportedException>(async () =>
		{
			var fmt = new RestApiFormUrlEncodedMediaTypeFormatter();
			using var content = new StringContent("test");
			await fmt.DeserializeAsync<string>(content, default);
		});
	}

	#endregion

	#region JsonMediaTypeFormatter — side-by-side serialization & deserialization

	private record TestDto(string Name, int Age);

	private static readonly System.Net.Http.Formatting.JsonMediaTypeFormatter _legacyJson = new();

	[TestMethod]
	public void Json_MediaType_SameAsLegacy()
	{
		new JsonFormatter().MediaType.AssertEqual(_legacyJson.SupportedMediaTypes.First().MediaType);
	}

	[TestMethod]
	public async Task Json_Serialize_Object_SameAsLegacy()
	{
		var current = new JsonFormatter();
		var obj = new { key = "value", num = 42 };

		var expected = await SerializeLegacy(_legacyJson, obj);
		var actual = await SerializeNew(current, obj);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Json_Serialize_Array_SameAsLegacy()
	{
		var current = new JsonFormatter();
		var arr = new[] { 1, 2, 3 };

		var expected = await SerializeLegacy(_legacyJson, arr);
		var actual = await SerializeNew(current, arr);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Json_Serialize_String_SameAsLegacy()
	{
		var current = new JsonFormatter();
		const string value = "hello world";

		var expected = await SerializeLegacy(_legacyJson, value);
		var actual = await SerializeNew(current, value);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Json_Serialize_Null_SameAsLegacy()
	{
		var current = new JsonFormatter();

		var expected = await SerializeLegacy(_legacyJson, null);
		var actual = await SerializeNew(current, null);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Json_Deserialize_Object_SameAsLegacy()
	{
		var current = new JsonFormatter();
		const string json = "{\"Name\":\"Alice\",\"Age\":30}";

		var expected = await DeserializeLegacy<TestDto>(_legacyJson, json, "application/json");
		var actual = await DeserializeNew<TestDto>(current, json, "application/json");

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Json_Deserialize_Array_SameAsLegacy()
	{
		var current = new JsonFormatter();
		const string json = "[1,2,3]";

		var expected = await DeserializeLegacy<int[]>(_legacyJson, json, "application/json");
		var actual = await DeserializeNew<int[]>(current, json, "application/json");

		actual.SequenceEqual(expected).AssertTrue();
	}

	[TestMethod]
	public async Task Json_Deserialize_Nested_SameAsLegacy()
	{
		var current = new JsonFormatter();
		const string json = "{\"items\":[{\"Name\":\"A\",\"Age\":1},{\"Name\":\"B\",\"Age\":2}]}";

		var expected = await DeserializeLegacy<Dictionary<string, TestDto[]>>(_legacyJson, json, "application/json");
		var actual = await DeserializeNew<Dictionary<string, TestDto[]>>(current, json, "application/json");

		actual["items"].Length.AssertEqual(expected["items"].Length);
		actual["items"][0].AssertEqual(expected["items"][0]);
		actual["items"][1].AssertEqual(expected["items"][1]);
	}

	[TestMethod]
	public async Task Json_RoundTrip()
	{
		var fmt = new JsonFormatter();
		var original = new TestDto("Bob", 25);

		using var content = fmt.Serialize(original);
		var deserialized = await fmt.DeserializeAsync<TestDto>(content, default);

		deserialized.AssertEqual(original);
	}

	[TestMethod]
	public void Json_NullOptions_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new JsonFormatter(null));
	}

	#endregion

	#region TextMediaTypeFormatter — side-by-side deserialization

	[TestMethod]
	public void Text_MediaType_SameAsLegacy()
	{
		var mediaTypes = new[] { "application/json", "text/plain" };

		var legacy = new LegacyTextFormatter(mediaTypes);
		var current = new TextMediaTypeFormatter(mediaTypes);

		current.MediaType.AssertEqual(legacy.SupportedMediaTypes.First().MediaType);
	}

	[TestMethod]
	public async Task Text_Deserialize_String_SameAsLegacy()
	{
		var mediaTypes = new[] { "text/plain" };

		var legacy = new LegacyTextFormatter(mediaTypes);
		var current = new TextMediaTypeFormatter(mediaTypes);

		const string body = "hello world";

		var expected = await DeserializeLegacy<string>(legacy, body, "text/plain");
		var actual = await DeserializeNew<string>(current, body, "text/plain");

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Text_Deserialize_EmptyString_SameAsLegacy()
	{
		var mediaTypes = new[] { "text/plain" };

		var legacy = new LegacyTextFormatter(mediaTypes);
		var current = new TextMediaTypeFormatter(mediaTypes);

		var expected = await DeserializeLegacy<string>(legacy, string.Empty, "text/plain");
		var actual = await DeserializeNew<string>(current, string.Empty, "text/plain");

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Text_Deserialize_Cyrillic_SameAsLegacy()
	{
		var mediaTypes = new[] { "text/plain" };

		var legacy = new LegacyTextFormatter(mediaTypes);
		var current = new TextMediaTypeFormatter(mediaTypes);

		const string body = "Привет мир! Тестовая строка.";

		var expected = await DeserializeLegacy<string>(legacy, body, "text/plain");
		var actual = await DeserializeNew<string>(current, body, "text/plain");

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Text_Deserialize_JsonLike_SameAsLegacy()
	{
		var mediaTypes = new[] { "application/json" };

		var legacy = new LegacyTextFormatter(mediaTypes);
		var current = new TextMediaTypeFormatter(mediaTypes);

		const string body = "{\"key\":\"value\",\"num\":42}";

		var expected = await DeserializeLegacy<string>(legacy, body, "application/json");
		var actual = await DeserializeNew<string>(current, body, "application/json");

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Text_Deserialize_MultilineBody_SameAsLegacy()
	{
		var mediaTypes = new[] { "text/plain" };

		var legacy = new LegacyTextFormatter(mediaTypes);
		var current = new TextMediaTypeFormatter(mediaTypes);

		const string body = "line1\nline2\r\nline3";

		var expected = await DeserializeLegacy<string>(legacy, body, "text/plain");
		var actual = await DeserializeNew<string>(current, body, "text/plain");

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public void Text_Serialize_Throws()
	{
		ThrowsExactly<NotSupportedException>(() =>
		{
			new TextMediaTypeFormatter(["text/plain"]).Serialize("test");
		});
	}

	[TestMethod]
	public void Text_NullMediaTypes_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new TextMediaTypeFormatter(null));
	}

	#endregion
}
