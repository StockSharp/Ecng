namespace Ecng.Serialization;

using System;

using Newtonsoft.Json;

/// <summary>
/// Converts boolean values to and from JSON numeric representations (1 or 0).
/// </summary>
public class JsonBoolConverter : JsonConverter
{
	/// <summary>
	/// Writes a boolean value as 1 (true) or 0 (false) to the JSON output.
	/// </summary>
	/// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
	/// <param name="value">The boolean value to be converted and written.</param>
	/// <param name="serializer">The calling serializer.</param>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		=> writer.WriteValue((bool)value ? 1 : 0);

	/// <summary>
	/// Reads a JSON value and converts it to a boolean value where "1" represents true.
	/// </summary>
	/// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
	/// <param name="objectType">The type of the object to convert to.</param>
	/// <param name="existingValue">The existing value of the object being read.</param>
	/// <param name="serializer">The calling serializer.</param>
	/// <returns>A boolean value where "1" evaluates to true, and any other value to false.</returns>
	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		=> reader.Value.ToString() == "1";

	/// <summary>
	/// Determines whether this converter can convert the specified object type.
	/// </summary>
	/// <param name="objectType">The type of object to check.</param>
	/// <returns><c>true</c> if the object type is <see cref="System.Boolean"/>; otherwise, <c>false</c>.</returns>
	public override bool CanConvert(Type objectType) => objectType == typeof(bool);
}