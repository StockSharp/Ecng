namespace Ecng.Serialization
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Diagnostics;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Newtonsoft.Json.Serialization;

	using Ecng.Common;

	public static class JsonHelper
	{
		[Conditional("DEBUG")]
		public static void ChechExpectedToken(this JsonReader reader, JsonToken token)
		{
			if (reader.TokenType != token)
				throw new InvalidOperationException($"{reader.TokenType} != {token}");
		}

		public static async Task ReadWithCheckAsync(this JsonReader reader, CancellationToken cancellationToken)
		{
			if (!await reader.ReadAsync(cancellationToken))
				throw new InvalidOperationException("EOF");
		}

		public static T DeserializeObject<T>(this string content)
		{
			return (T)content.DeserializeObject(typeof(T));
		}

		public static T DeserializeObject<T>(this JToken token)
		{
			return (T)token.DeserializeObject(typeof(T));
		}

		public static object DeserializeObject(this string content, Type type)
		{
			if (content.IsEmpty())
				throw new ArgumentNullException(nameof(content));

			if (type is null)
				throw new ArgumentNullException(nameof(type));

			try
			{
				if (content == "null")
					return null;

				return JsonConvert.DeserializeObject(content, type);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Can't convert {content} to type '{type.Name}'.", ex);
			}
		}

		public static object DeserializeObject(this JToken token, Type type)
		{
			if (token is null)
				throw new ArgumentNullException(nameof(token));

			if (type is null)
				throw new ArgumentNullException(nameof(type));

			try
			{
				if (token.Type == JTokenType.String && (string)token == "null")
					return null;

				return token.ToObject(type);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Can't convert {token} to type '{type.Name}'.", ex);
			}
		}

		public static JsonSerializerSettings CreateJsonSerializerSettings()
		{
			return new JsonSerializerSettings
			{
				FloatParseHandling = FloatParseHandling.Decimal,
				NullValueHandling = NullValueHandling.Ignore,
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new SnakeCaseNamingStrategy()
				}
			};
		}

		[Obsolete]
		public static JsonWriter WriteProperty(this JsonWriter writer, string name, object value)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			writer.WritePropertyName(name);
			writer.WriteValue(value);

			return writer;
		}
	}
}