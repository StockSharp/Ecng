namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	public class DynamicFieldFactory : InnerSchemaFieldFactory<object>
	{
		private readonly Dictionary<Type, FieldFactory> _factoriesByFieldType = new();
		private readonly Dictionary<Type, FieldFactory> _factoriesByType = new();

		public DynamicFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override object OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			var type = source["Type"].Value.To<Type>();
			var value = source["Value"].Value;

			if (!SchemaManager.GlobalFieldFactories.TryGetValue(type, out var factoryType))
				return value.To(type);

			var factory = GetFactory(factoryType);
			return factory.CreateInstance(serializer, new SerializationItem(factory.Field, value));
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, object instance)
		{
			var instanceType = instance.GetType();
			var valueType = instanceType;

			object value;

			if (instance is not Type type)
			{
				if (SchemaManager.GlobalFieldFactories.TryGetValue(instanceType, out var factoryType))
				{
					var factory = GetFactory(factoryType);

					value = factory.CreateSource(serializer, instance).Value;
					valueType = factory.SourceType;
				}
				else
				{
					value = instanceType.IsSerializablePrimitive()
						? instance 
						: GetInnerSchemaFactory(instanceType).OnCreateSource(serializer, instance);
				}
			}
			else
			{
				value = type.GetTypeAsString(false);
			}

			return new SerializationItemCollection
			{
				new SerializationItem(new VoidField<string>("Type"), instanceType.GetTypeAsString(false)),
				new SerializationItem(new VoidField("Value", valueType), value)
			};
		}

		private FieldFactory GetInnerSchemaFactory(Type type)
		{
			return _factoriesByFieldType.SafeAdd(type, key => typeof(InnerSchemaFieldFactory<>).Make(type).CreateInstance<FieldFactory>(Field, Order));
		}

		private FieldFactory GetFactory(Type factoryType)
		{
			return _factoriesByType.SafeAdd(factoryType, key => factoryType.CreateInstance<FieldFactory>(Field, Order));
		}
	}

	public class DynamicAttribute : ReflectionImplFieldFactoryAttribute
	{
		public DynamicAttribute()
			: base(typeof(DynamicFieldFactory))
		{
		}
	}
}