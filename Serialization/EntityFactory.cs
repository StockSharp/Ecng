namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<EntityFactory>))]
	public abstract class EntityFactory : Serializable<EntityFactory>
	{
		public abstract bool FullInitialize { get; }

		public abstract object CreateObject(ISerializer serializer, SerializationItemCollection source);

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
		}

		protected override bool OnEquals(EntityFactory other)
		{
			return object.ReferenceEquals(this, other);
		}
	}

	public abstract class EntityFactory<TEntity> : EntityFactory
	{
		public abstract TEntity CreateEntity(ISerializer serializer, SerializationItemCollection source);

		public override object CreateObject(ISerializer serializer, SerializationItemCollection source)
		{
			return CreateEntity(serializer, source);
		}
	}

#if SILVERLIGHT
	public
#endif
	class PrimitiveEntityFactory<TEntity> : EntityFactory<TEntity>
	{
		public PrimitiveEntityFactory(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			Name = name;
		}

		public string Name { get; }

		public override bool FullInitialize => true;

		public override TEntity CreateEntity(ISerializer serializer, SerializationItemCollection source)
		{
			return source[Name].Value.To<TEntity>();
		}
	}

#if SILVERLIGHT
	public
#endif
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

				if (item.Value == null)
					elem = default(TItem);
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