namespace Ecng.Net
{
	using System;

	using Newtonsoft.Json;

	public class JsonBoolConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			=> writer.WriteValue((bool)value ? 1 : 0);

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			=> reader.Value.ToString() == "1";

		public override bool CanConvert(Type objectType) => objectType == typeof(bool);
	}
}