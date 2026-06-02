namespace Ecng.Serialization;

/// <summary>
/// Provides a JSON converter for converting DateTime values from Unix timestamps.
/// </summary>
/// <param name="isSeconds">Determines whether the Unix timestamp is in seconds (true) or another resolution (false).</param>
public class JsonDateTimeConverter(bool isSeconds) : JsonConverter
{
	private readonly bool _isSeconds = isSeconds;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonDateTimeConverter"/> class using seconds as the default resolution.
	/// </summary>
	public JsonDateTimeConverter()
		: this(true)
	{
	}

	/// <summary>
	/// Determines whether this converter can convert the specified object type.
	/// </summary>
	/// <param name="objectType">The type of the object to check.</param>
	/// <returns><c>true</c> if the objectType is <see cref="DateTime"/> or
	/// <see cref="Nullable{DateTime}"/>; otherwise, <c>false</c>. Accepting the
	/// nullable form lets the converter be registered globally and still cover
	/// optional <c>DateTime?</c> members.</returns>
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
	}

	/// <summary>
	/// Reads the JSON representation of the object and converts it to a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
	/// <param name="objectType">The type of the object to convert.</param>
	/// <param name="existingValue">The existing value of the object being read.</param>
	/// <param name="serializer">The calling serializer.</param>
	/// <returns>The converted <see cref="DateTime"/> value or null if conversion is not possible.</returns>
	/// <exception cref="JsonReaderException">Thrown when an error occurs during conversion.</exception>
	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		//if (reader.TokenType != JsonToken.String)
		//	throw new JsonReaderException("Unexcepted token '{0}'.".Put(reader.TokenType));

		try
		{
			switch (reader.Value)
			{
				case null:
					return NoValue(objectType);

				// Newtonsoft auto-parses an ISO-8601 string into a DateTime/DateTimeOffset token;
				// use it directly instead of coercing it to a number (which collapses to the epoch).
				case DateTime dt:
					return NormalizeUtc(dt);

				case DateTimeOffset dto:
					return dto.UtcDateTime;

				case string s:
				{
					if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
						return FromNumber(num, objectType);

					return DateTime.Parse(s, System.Globalization.CultureInfo.InvariantCulture,
						System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
				}
			}

			return FromNumber(reader.Value.To<double?>(), objectType);
		}
		catch (Exception ex)
		{
			throw new JsonReaderException(ex.Message, ex);
		}
	}

	private object FromNumber(double? value, Type objectType)
	{
		if (value is not double d || (int)d == 0)
			return NoValue(objectType);

		return Convert(d);
	}

	// "No value" on the wire (null / absent / the 0 sentinel) maps to null for a DateTime? target,
	// but to default(DateTime) for a non-nullable DateTime target — returning null there makes
	// Newtonsoft fail with NullReferenceException when assigning into the value-type member.
	private static object NoValue(Type objectType)
		=> objectType == typeof(DateTime?) ? null : default(DateTime);

	// The wire is all-UTC. A Local token is converted; an Unspecified token (e.g. an ISO-8601
	// string with no zone designator) is taken AS UTC rather than assumed local — consistent
	// with the textual-date path's AssumeUniversal, so a no-zone timestamp is not shifted by the
	// server's local offset.
	private static DateTime NormalizeUtc(DateTime dt)
		=> dt.Kind switch
		{
			DateTimeKind.Utc => dt,
			DateTimeKind.Local => dt.ToUniversalTime(),
			_ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
		};

	// Pre-epoch / unset (e.g. default(DateTime)) is written as the 0 sentinel that ReadJson maps back
	// to "no value". This keeps the wire format numeric and avoids the GetUnixDiff pre-epoch throw.
	private protected static bool TryWriteSentinel(JsonWriter writer, DateTime dt)
	{
		if (dt.ToUniversalTime() < TimeHelper.GregorianStart)
		{
			writer.WriteRawValue("0");
			return true;
		}

		return false;
	}

	/// <summary>
	/// Converts the specified double value to a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="value">The double value representing the Unix timestamp.</param>
	/// <returns>A <see cref="DateTime"/> corresponding to the Unix timestamp.</returns>
	protected virtual DateTime Convert(double value)
	{
		return value.FromUnix(_isSeconds);
	}

	/// <summary>
	/// Writes the JSON representation of the object.
	/// </summary>
	/// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
	/// <param name="value">The value to convert.</param>
	/// <param name="serializer">The calling serializer.</param>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		if (value is not DateTime dt)
			throw new ArgumentException($"Value must be DateTime, but was {value?.GetType()}");

		if (TryWriteSentinel(writer, dt))
			return;

		writer.WriteRawValue(dt.ToUnix(_isSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture));
	}
}

/// <summary>
/// Provides a JSON converter for converting DateTime values from Unix timestamps in milliseconds.
/// </summary>
public class JsonDateTimeMlsConverter : JsonDateTimeConverter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonDateTimeMlsConverter"/> class using milliseconds.
	/// </summary>
	public JsonDateTimeMlsConverter()
		: base(false)
	{
	}
}

/// <summary>
/// Provides a JSON converter for converting DateTime values from Unix timestamps in microseconds.
/// </summary>
public class JsonDateTimeMcsConverter : JsonDateTimeConverter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonDateTimeMcsConverter"/> class using microseconds.
	/// </summary>
	public JsonDateTimeMcsConverter()
		: base(false)
	{
	}

	/// <summary>
	/// Converts the specified double value to a <see cref="DateTime"/> using microsecond precision.
	/// </summary>
	/// <param name="value">The double value representing the Unix timestamp in microseconds.</param>
	/// <returns>A <see cref="DateTime"/> corresponding to the Unix timestamp.</returns>
	protected override DateTime Convert(double value)
	{
		return value.FromUnixMcs();
	}

	/// <summary>
	/// Writes the JSON representation of the object in microseconds.
	/// </summary>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		if (value is not DateTime dt)
			throw new ArgumentException($"Value must be DateTime, but was {value?.GetType()}");

		if (TryWriteSentinel(writer, dt))
			return;

		writer.WriteRawValue(dt.ToUnixMcs().ToString(System.Globalization.CultureInfo.InvariantCulture));
	}
}

/// <summary>
/// Provides a JSON converter for converting DateTime values from Unix timestamps in nanoseconds.
/// </summary>
public class JsonDateTimeNanoConverter : JsonDateTimeConverter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonDateTimeNanoConverter"/> class using nanoseconds.
	/// </summary>
	public JsonDateTimeNanoConverter()
		: base(false)
	{
	}

	/// <summary>
	/// Converts the specified double value to a <see cref="DateTime"/> using nanosecond precision.
	/// </summary>
	/// <param name="value">The double value representing the Unix timestamp in nanoseconds.</param>
	/// <returns>A <see cref="DateTime"/> corresponding to the Unix timestamp.</returns>
	protected override DateTime Convert(double value)
	{
		return TimeHelper.GregorianStart.AddNanoseconds((long)value);
	}

	/// <summary>
	/// Writes the JSON representation of the object in nanoseconds.
	/// </summary>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		if (value is not DateTime dt)
			throw new ArgumentException($"Value must be DateTime, but was {value?.GetType()}");

		if (TryWriteSentinel(writer, dt))
			return;

		writer.WriteRawValue((dt.ToUniversalTime() - TimeHelper.GregorianStart).ToNanoseconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
	}
}