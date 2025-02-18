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

	/// <summary>
	/// Provides helper methods for JSON serialization and deserialization.
	/// </summary>
	public static class JsonHelper
	{
		/// <summary>
		/// Provides a UTF-8 encoding without a byte order mark (BOM).
		/// </summary>
		public static readonly Encoding UTF8NoBom = new UTF8Encoding(false);

		/// <summary>
		/// Checks that the current token of the JSON reader matches the expected token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="token">The expected JSON token.</param>
		[Conditional("DEBUG")]
		public static void ChechExpectedToken(this JsonReader reader, JsonToken token)
		{
			if (reader.TokenType != token)
				throw new InvalidOperationException($"{reader.TokenType} != {token}");
		}

		/// <summary>
		/// Asynchronously reads the next token from the JSON reader and checks for end-of-file.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>A task that represents the asynchronous read operation.</returns>
		public static async Task ReadWithCheckAsync(this JsonReader reader, CancellationToken cancellationToken)
		{
			if (!await reader.ReadAsync(cancellationToken))
				throw new InvalidOperationException("EOF");
		}

		/// <summary>
		/// Deserializes the JSON string into an object of type T.
		/// </summary>
		/// <typeparam name="T">The target type.</typeparam>
		/// <param name="content">The JSON string.</param>
		/// <returns>The deserialized object.</returns>
		public static T DeserializeObject<T>(this string content)
		{
			return (T)content.DeserializeObject(typeof(T));
		}

		/// <summary>
		/// Deserializes the JSON token into an object of type T.
		/// </summary>
		/// <typeparam name="T">The target type.</typeparam>
		/// <param name="token">The JSON token.</param>
		/// <returns>The deserialized object.</returns>
		public static T DeserializeObject<T>(this JToken token)
		{
			return (T)token.DeserializeObject(typeof(T));
		}

		/// <summary>
		/// Deserializes the JSON string into an object of the specified type.
		/// </summary>
		/// <param name="content">The JSON string.</param>
		/// <param name="type">The target type.</param>
		/// <returns>The deserialized object.</returns>
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

		/// <summary>
		/// Deserializes the JSON string into an object using Newtonsoft.Json.
		/// </summary>
		/// <param name="json">The JSON string.</param>
		/// <param name="type">The target type.</param>
		/// <returns>The deserialized object.</returns>
		public static object FromJson(this string json, Type type)
			=> JsonConvert.DeserializeObject(json, type);

		/// <summary>
		/// Deserializes the JSON token into an object of the specified type.
		/// </summary>
		/// <param name="token">The JSON token.</param>
		/// <param name="type">The target type.</param>
		/// <returns>The deserialized object.</returns>
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

		/// <summary>
		/// Serializes the object into a JSON string with optional indentation.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="indent">True to format the JSON string; otherwise, false.</param>
		/// <returns>The JSON string.</returns>
		public static string ToJson(this object obj, bool indent = true)
			=> ToJson(obj, indent, null);

		/// <summary>
		/// Serializes the object into a JSON string with optional indentation and custom serializer settings.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="indent">True to format the JSON string; otherwise, false.</param>
		/// <param name="settings">The JSON serializer settings.</param>
		/// <returns>The JSON string.</returns>
		public static string ToJson(this object obj, bool indent, JsonSerializerSettings settings)
			=> JsonConvert.SerializeObject(obj, indent ? Formatting.Indented : Formatting.None, settings);

		/// <summary>
		/// Creates and configures a new instance of JsonSerializerSettings.
		/// </summary>
		/// <returns>The configured JsonSerializerSettings instance.</returns>
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

		/// <summary>
		/// Skips the Byte Order Mark (BOM) from the beginning of a byte array if present.
		/// </summary>
		/// <param name="array">The byte array.</param>
		/// <returns>The byte array without the BOM.</returns>
		public static byte[] SkipBom(this byte[] array)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			if (array.Length >= 3 && array[0] == 239 && array[1] == 187 && array[2] == 191)
				array = [.. array.Skip(3)];

			return array;
		}
	}
}