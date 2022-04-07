namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	public class CollectionFieldFactory<TCollection> : FieldFactory<TCollection, SerializationItemCollection>
	{
		public CollectionFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override async ValueTask<TCollection> OnCreateInstance(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			return await serializer.GetLegacySerializer<TCollection>().Deserialize(source, cancellationToken);
		}

		protected internal override async ValueTask<SerializationItemCollection> OnCreateSource(ISerializer serializer, TCollection instance, CancellationToken cancellationToken)
		{
			var source = new SerializationItemCollection();
			await serializer.GetLegacySerializer<TCollection>().Serialize(instance, source, cancellationToken);
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

		public override async ValueTask<TCollection> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
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
				throw new InvalidOperationException($"Type '{typeof(TCollection)}' isn't collection.");

			var itemSer = serializer.GetLegacySerializer<TItem>();
			var primitive = typeof(TItem).IsSerializablePrimitive();

			foreach (var item in source)
			{
				TItem elem;

				if (item.Value is null)
					elem = default;
				else
					elem = primitive ? (TItem)item.Value : await itemSer.Deserialize((SerializationItemCollection)item.Value, cancellationToken);

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