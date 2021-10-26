namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	using Newtonsoft.Json;

	public class JsonDateTimeConverter : JsonConverter
	{
		private readonly bool _isSeconds;

		public JsonDateTimeConverter()
			: this(true)
		{
		}

		public JsonDateTimeConverter(bool isSeconds)
		{
			_isSeconds = isSeconds;
		}

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
				var value = reader.Value.To<double?>();

				if (value is null)
					return null;

				return Convert(value.Value);
			}
			catch (Exception ex)
			{
				throw new JsonReaderException(ex.Message, ex);
			}
		}

		protected virtual DateTime Convert(double value)
		{
			return value.FromUnix(_isSeconds);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}
	}

	public class JsonDateTimeMlsConverter : JsonDateTimeConverter
	{
		public JsonDateTimeMlsConverter()
			: base(false)
		{
		}
	}

	public class JsonDateTimeMcsConverter : JsonDateTimeConverter
	{
		public JsonDateTimeMcsConverter()
			: base(false)
		{
		}

		protected override DateTime Convert(double value)
		{
			return value.FromUnixMcs();
		}
	}

	public class JsonDateTimeNanoConverter : JsonDateTimeConverter
	{
		public JsonDateTimeNanoConverter()
			: base(false)
		{
		}

		protected override DateTime Convert(double value)
		{
			return TimeHelper.GregorianStart.AddNanoseconds((long)value);
		}
	}
}