namespace Ecng.Serialization;

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Ecng.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JArrayToObjectConverter : JsonConverter
{
	public override bool CanConvert(Type objectType) => true;

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		existingValue ??= Activator.CreateInstance(objectType);
		
		var array = JArray.Load(reader);

		var fields = objectType
			.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.OrderByDeclaration()
			.ToArray();
		
		for (var i = 0; i < fields.Length; i++)
		{
			var field = fields[i];
			var token = array[i];
			field.SetValue(existingValue, token.ToObject(field.FieldType));
		}

		return existingValue;
	}

	public override bool CanWrite => false;

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		=> throw new NotSupportedException();
}

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

	public override bool CanConvert(Type objectType) => true;

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

	public override bool CanWrite => false;

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		=> throw new NotSupportedException();
}