namespace Ecng.Serialization;

using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

/// <summary>
/// Converts a JSON array to an object by mapping array elements to the object's fields in declaration order.
/// </summary>
public class JArrayToObjectConverter : JsonConverter
{
	/// <summary>
	/// Determines whether this converter can convert the specified object type.
	/// </summary>
	/// <param name="objectType">Type of the object.</param>
	/// <returns>Always returns true.</returns>
	public override bool CanConvert(Type objectType) => true;

	/// <summary>
	/// Reads the JSON representation of the object and maps JSON array elements to object fields.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="objectType">Type of the object.</param>
	/// <param name="existingValue">The existing value of the object being populated.</param>
	/// <param name="serializer">The JSON serializer.</param>
	/// <returns>The deserialized object with fields set from the JSON array.</returns>
	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;

		existingValue ??= Activator.CreateInstance(objectType);
		
		var array = JArray.Load(reader);

		var fields = objectType.GetJsonFields();

		if (array.Count < fields.Length)
			throw new JsonSerializationException($"JSON array has {array.Count} elements but type '{objectType.Name}' requires {fields.Length}.");

		for (var i = 0; i < fields.Length; i++)
		{
			var field = fields[i];
			var token = array[i];
			field.SetValue(existingValue, token.ToObject(field.FieldType));
		}

		return existingValue;
	}

	/// <summary>
	/// Gets a value indicating whether this converter can write JSON.
	/// </summary>
	public override bool CanWrite => true;

	/// <summary>
	/// Writes the object as a JSON array, mapping fields in declaration order.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The object value.</param>
	/// <param name="serializer">The JSON serializer.</param>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}

		var type = value.GetType();
		var fields = type.GetJsonFields();

		writer.WriteStartArray();

		foreach (var field in fields)
		{
			var fieldValue = field.GetValue(value);
			serializer.Serialize(writer, fieldValue);
		}

		writer.WriteEndArray();
	}
}

/// <summary>
/// Converts a JSON array to an object of type <typeparamref name="T"/> by mapping array elements to the object's fields 
/// in declaration order using compiled IL for performance.
/// </summary>
/// <typeparam name="T">The type of the object to convert to. Must have a parameterless constructor.</typeparam>
public class JArrayToObjectConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T> : JsonConverter
	where T : new()
{
	// Native AOT: explicit, statically compiled field accessors and stable wire order.
	// JIT callers may continue to use the original reflection/IL-emit path.
	private static (Type fieldType, Func<T, object> getter, Action<T, object> setter)[] _registeredFields;

	/// <summary>
	/// Registers field metadata and precompiled accessors in JSON wire order.
	/// Use this instead of relying on reflected fields for Native AOT models.
	/// Must be called before reading or writing the first payload.
	/// </summary>
	public static void RegisterAotFields(params (Type fieldType, Func<T, object> getter, Action<T, object> setter)[] fields)
	{
		ArgumentNullException.ThrowIfNull(fields);
		if (fields.Length == 0)
			throw new ArgumentException("At least one field must be registered.", nameof(fields));

		foreach (var field in fields)
		{
			ArgumentNullException.ThrowIfNull(field.fieldType);
			ArgumentNullException.ThrowIfNull(field.getter);
			ArgumentNullException.ThrowIfNull(field.setter);
		}

		var snapshot = fields.ToArray();
		if (Interlocked.CompareExchange(ref _registeredFields, snapshot, null) is not null)
			throw new InvalidOperationException($"AOT JSON fields for '{typeof(T)}' are already registered.");
	}

	private static Action<T, object> CreateFieldSetter(FieldInfo fieldInfo)
	{
		// Reflection.Emit is unavailable in Native AOT. Use retained field metadata
		// for the legacy Newtonsoft path instead of failing in the static constructor.
		// For reflection-free AOT payloads, use JsonHelper's source-generated overloads.
		if (!RuntimeFeature.IsDynamicCodeSupported)
			return (target, value) => fieldInfo.SetValue(target, value);

		var method = new DynamicMethod(
			"Set" + fieldInfo.Name,
			typeof(void),
			[typeof(T), typeof(object)],
			typeof(T), true);

		var il = method.GetILGenerator();

		il.Emit(OpCodes.Ldarg_0);

		if (fieldInfo.FieldType.IsValueType)
		{
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
		}
		else
		{
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Castclass, fieldInfo.FieldType);
		}

		il.Emit(OpCodes.Stfld, fieldInfo);

		il.Emit(OpCodes.Ret);

		return (Action<T, object>)method.CreateDelegate(typeof(Action<T, object>));
	}

	private static readonly (FieldInfo, Action<T, object>)[] _fields;

	static JArrayToObjectConverter()
	{
		_fields = [.. typeof(T)
			.GetJsonFields()
			.Select(f => (f, CreateFieldSetter(f)))];
	}

	/// <summary>
	/// Determines whether this converter can convert the specified object type.
	/// </summary>
	/// <param name="objectType">Type of the object.</param>
	/// <returns>Always returns true.</returns>
	public override bool CanConvert(Type objectType) => true;

	/// <summary>
	/// Reads the JSON representation of the object and maps JSON array elements to the corresponding fields of an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The JSON reader.</param>
	/// <param name="objectType">Type of the object.</param>
	/// <param name="existingValue">The existing instance of <typeparamref name="T"/> to populate.</param>
	/// <param name="serializer">The JSON serializer.</param>
	/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;

		existingValue ??= new T();

		var typed = (T)existingValue;

		var array = JArray.Load(reader);

		var registered = Volatile.Read(ref _registeredFields);
		if (registered is not null)
		{
			if (array.Count < registered.Length)
				throw new JsonSerializationException($"JSON array has {array.Count} elements but type '{typeof(T).Name}' requires {registered.Length}.");

			for (var i = 0; i < registered.Length; i++)
			{
				var field = registered[i];
				field.setter(typed, array[i].ToObject(field.fieldType));
			}

			return existingValue;
		}

		if (array.Count < _fields.Length)
			throw new JsonSerializationException($"JSON array has {array.Count} elements but type '{typeof(T).Name}' requires {_fields.Length}.");

		for (var i = 0; i < _fields.Length; i++)
		{
			var item = array[i];

			var (f, action) = _fields[i];
			action(typed, item.ToObject(f.FieldType));
		}

		return existingValue;
	}

	/// <summary>
	/// Gets a value indicating whether this converter can write JSON.
	/// </summary>
	public override bool CanWrite => true;

	/// <summary>
	/// Writes the object as a JSON array, mapping fields in declaration order.
	/// </summary>
	/// <param name="writer">The JSON writer.</param>
	/// <param name="value">The object value.</param>
	/// <param name="serializer">The JSON serializer.</param>
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartArray();

		var registered = Volatile.Read(ref _registeredFields);
		if (registered is not null)
		{
			foreach (var field in registered)
				serializer.Serialize(writer, field.getter((T)value));
		}
		else
		{
			foreach (var (f, _) in _fields)
				serializer.Serialize(writer, f.GetValue(value));
		}

		writer.WriteEndArray();
	}
}
