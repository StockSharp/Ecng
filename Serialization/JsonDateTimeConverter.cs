namespace Ecng.Serialization;

using System;

using Ecng.Common;

using Newtonsoft.Json;

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
	/// <returns><c>true</c> if the objectType is DateTime; otherwise, <c>false</c>.</returns>
	public override bool CanConvert(Type objectType)
	{
		return typeof(DateTime) == objectType;
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
			var value = reader.Value.To<double?>();

			if (value is not double d || (int)d == 0)
				return null;

			return Convert(d);
		}
		catch (Exception ex)
		{
			throw new JsonReaderException(ex.Message, ex);
		}
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
		if (value is not DateTime dt)
			throw new ArgumentException($"Value must be DateTime, but was {value?.GetType()}");

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
		if (value is not DateTime dt)
			throw new ArgumentException($"Value must be DateTime, but was {value?.GetType()}");

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
		if (value is not DateTime dt)
			throw new ArgumentException($"Value must be DateTime, but was {value?.GetType()}");

		writer.WriteRawValue(dt.ToNanoseconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
	}
}