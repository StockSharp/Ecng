namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	public class CollectionFieldFactory<TCollection> : FieldFactory<TCollection, SerializationItemCollection>
	{
		public CollectionFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override TCollection OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			return serializer.GetSerializer<TCollection>().Deserialize(source);
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, TCollection instance)
		{
			var source = new SerializationItemCollection();
			serializer.GetSerializer<TCollection>().Serialize(instance, source);
			return source;
		}
	}

	public class CollectionAttribute : SerializerAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(CollectionFieldFactory<>).Make(field.Type);
		}
	}

	class CollectionEntityFactory<TCollection, TItem> : EntityFactory<TCollection>
		where TCollection : IEnumerable<TItem>
	{
		public override bool FullInitialize => true;

		public override TCollection CreateEntity(ISerializer serializer, SerializationItemCollection source)
		{
			ICollection<TItem> instance;

			if (typeof(TCollection).GetGenericType(typeof(IDictionary<,>)) != null)
			{
				instance = typeof(TCollection).IsClass
					? ReflectionHelper.CreateInstance<TCollection>().To<ICollection<TItem>>()
					: typeof(Dictionary<,>).Make(typeof(TCollection).GetGenericArguments()).CreateInstance<ICollection<TItem>>();
			}
			else if (typeof(TCollection).IsArray || typeof(TCollection).GetGenericType(typeof(IList<>)) != null || typeof(TCollection).GetGenericType(typeof(IEnumerable<>)) != null)
			{
				if (!typeof(TCollection).IsArray && typeof(TCollection).IsClass)
					instance = ReflectionHelper.CreateInstance<TCollection>().To<ICollection<TItem>>();
				else
					instance = new List<TItem>().To<ICollection<TItem>>();
			}
			else
				throw new InvalidOperationException("Type '{0}' isn't collection.".Put(typeof(TCollection)));

			var itemSer = serializer.GetSerializer<TItem>();
			var primitive = typeof(TItem).IsSerializablePrimitive();

			foreach (var item in source)
			{
				TItem elem;

				if (item.Value is null)
					elem = default;
				else
					elem = primitive ? (TItem)item.Value : itemSer.Deserialize((SerializationItemCollection)item.Value);

				instance.Add(elem);
			}

			if (typeof(TCollection).IsArray)
				instance = ((List<TItem>)instance).ToArray();
			else
			{
				if (!typeof(TCollection).IsInterface)
				{
					var ctors = typeof(TCollection).GetMembers<ConstructorInfo>(typeof(IEnumerable<TItem>));

					if (ctors.Length > 0)
						instance = typeof(TCollection).GetMember<ConstructorInfo>(typeof(IEnumerable<TItem>)).CreateInstance<ICollection<TItem>>(instance);
					else
					{
						var col = typeof(TCollection).GetMember<ConstructorInfo>().CreateInstance<ICollection<TItem>>(null);
						col.AddRange(instance);
						instance = col;
					}
				}
			}

			return instance.To<TCollection>();
		}
	}
}