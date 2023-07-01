namespace Ecng.Serialization
{
	using System;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Diagnostics;
	using System.Linq;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Newtonsoft.Json.Serialization;

	using Ecng.Common;

	public static class JsonHelper
	{
		public static readonly Encoding UTF8NoBom = new UTF8Encoding(false);

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
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (content.IsEmpty())
			{
				if (type.IsClass || type.IsNullable() || type.IsInterface)
					return null;

				throw new ArgumentNullException(nameof(content), $"Can't null for {type}.");
			}

			try
			{
				if (content == "null")
					return null;

				return content.FromJson(type);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Can't convert {content} to type '{type.Name}'.", ex);
			}
		}

		public static object FromJson(this string json, Type type)
			=> JsonConvert.DeserializeObject(json, type);

		public static object DeserializeObject(this JToken token, Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (token is null)
			{
				if (type.IsClass || type.IsNullable() || type.IsInterface)
					return null;

				throw new ArgumentNullException(nameof(token), $"Can't null for {type}.");
			}

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

		public static string ToJson(this object obj)
			=> JsonConvert.SerializeObject(obj);

		public static JsonSerializerSettings CreateJsonSerializerSettings()
		{
			return new()
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

		public static byte[] SkipBom(this byte[] array)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			if (array.Length >= 3 && array[0] == 239 && array[1] == 187 && array[2] == 191)
				array = array.Skip(3).ToArray();

			return array;
		}
	}
}