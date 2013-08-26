namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	public class DynamicFieldFactory : InnerSchemaFieldFactory<object>
	{
		private readonly Dictionary<Type, FieldFactory> _factories = new Dictionary<Type, FieldFactory>();

		public DynamicFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override object OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			var type = source["Type"].Value.To<Type>();
			var value = source["Value"].Value;

			return value.To(type);
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, object instance)
		{
			var instanceType = instance.GetType();
			var valueType = instanceType;
			
			var type = instance as Type;

			object value;
			
			if (type == null)
			{
				var isColor = false;

				if (instanceType.IsWpfColor())
				{
					isColor = true;
				}
#if !SILVERLIGHT
				else if (instanceType.IsWinColor())
				{
					isColor = true;
				}
#endif
				if (isColor)
				{
					value = instance.To<int>();
					valueType = typeof(int);
				}
				else
					value = instanceType.IsPrimitive()
						        ? instance
						        : GetInnerSchemaFactory(instanceType).OnCreateSource(serializer, instance);
			}
			else
			{
				value = type.GetTypeAsString();
			}

			return new SerializationItemCollection
			{
				new SerializationItem(new VoidField<string>("Type"), instanceType.GetTypeAsString()),
				new SerializationItem(new VoidField("Value", valueType), value)
			};
		}

		private FieldFactory GetInnerSchemaFactory(Type type)
		{
			return _factories.SafeAdd(type, key => typeof(InnerSchemaFieldFactory<>).Make(type).CreateInstance<FieldFactory>(Field, Order));
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