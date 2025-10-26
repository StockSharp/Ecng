namespace Ecng.Serialization;

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Ecng.Reflection;

using Newtonsoft.Json;
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
		existingValue ??= Activator.CreateInstance(objectType);
		
		var array = JArray.Load(reader);

		var fields = objectType.GetJsonFields();
		
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
public class JArrayToObjectConverter<T> : JsonConverter
	where T : new()
{
	private static Action<T, object> CreateFieldSetter(FieldInfo fieldInfo)
	{
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

	private static readonly (Type, Action<T, object>)[] _fields;

	static JArrayToObjectConverter()
	{
		_fields = [.. typeof(T)
			.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.OrderByDeclaration()
			.Select(f => (f.FieldType, CreateFieldSetter(f)))];
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
		existingValue ??= new T();

		var typed = (T)existingValue;

		var array = JArray.Load(reader);

		for (var i = 0; i < _fields.Length; i++)
		{
			var item = array[i];

			var (type, action) = _fields[i];
			action(typed, item.ToObject(type));
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

		var fields = typeof(T).GetJsonFields();

		writer.WriteStartArray();

		foreach (var (type, action) in _fields)
		{
			foreach (var f in fields)
			{
				if (f.FieldType == type)
				{
					var fieldValue = f.GetValue(value);
					serializer.Serialize(writer, fieldValue);
					break;
				}
			}
		}

		writer.WriteEndArray();
	}
}