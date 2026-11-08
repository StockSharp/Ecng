namespace Ecng.Serialization;

using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Source-generated, trimming-safe JSON alternatives to the existing Newtonsoft-based methods.
/// Callers explicitly provide generated metadata for every serialized type.
/// </summary>
public static partial class JsonHelper
{
#if NET7_0_OR_GREATER
	/// <summary>
	/// Deserializes using non-generic, source-generated metadata known at build time.
	/// The type information replaces dynamic CLR type inspection.
	/// </summary>
	public static object FromJson(this string json, JsonTypeInfo typeInfo)
	{
		ArgumentNullException.ThrowIfNull(typeInfo);
		return System.Text.Json.JsonSerializer.Deserialize(json, typeInfo);
	}

	/// <summary>
	/// Serializes an object with non-generic, source-generated metadata.
	/// </summary>
	public static string ToJson(this object value, JsonTypeInfo typeInfo)
	{
		ArgumentNullException.ThrowIfNull(typeInfo);
		return System.Text.Json.JsonSerializer.Serialize(value, typeInfo);
	}

#endif

	/// <summary>
	/// Deserializes with a source-generated JSON contract, without runtime contract discovery.
	/// </summary>
	public static T FromJson<T>(this string json, JsonTypeInfo<T> typeInfo)
	{
		ArgumentNullException.ThrowIfNull(typeInfo);
		return System.Text.Json.JsonSerializer.Deserialize(json, typeInfo);
	}

	/// <summary>
	/// Serializes with a source-generated JSON contract, without runtime contract discovery.
	/// </summary>
	public static string ToJson<T>(this T value, JsonTypeInfo<T> typeInfo)
	{
		ArgumentNullException.ThrowIfNull(typeInfo);
		return System.Text.Json.JsonSerializer.Serialize(value, typeInfo);
	}
}
