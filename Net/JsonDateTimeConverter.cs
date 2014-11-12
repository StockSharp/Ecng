namespace Ecng.Net
{
	using System;

	using Ecng.Common;

	using Newtonsoft.Json;

	public class JsonDateTimeConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(DateTime) == objectType;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			//if (reader.TokenType != JsonToken.String)
			//	throw new JsonReaderException("Unexcepted token '{0}'.".Put(reader.TokenType));

			try
			{
				return TimeHelper.GregorianStart.AddSeconds(reader.Value.To<long>());
			}
			catch (Exception ex)
			{
				throw new JsonReaderException(ex.Message, ex);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}
	}
}