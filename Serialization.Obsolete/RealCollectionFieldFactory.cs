namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	class RealCollectionFieldFactory<TCollection, TItem> : CollectionFieldFactory<TCollection>
		where TCollection : IEnumerable<TItem>
	{
		public RealCollectionFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override ValueTask<TCollection> OnCreateInstance(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		protected internal override async ValueTask<SerializationItemCollection> OnCreateSource(ISerializer serializer, TCollection instance, CancellationToken cancellationToken)
		{
			var source = new SerializationItemCollection();
			var primitive = typeof(TItem).IsSerializablePrimitive();
			var itemSer = serializer.GetLegacySerializer<TItem>();

			var index = 0;
			foreach (var item in GetItems(instance))
			{
				var name = index.ToString();

				if (item.IsNull())
					source.Add(new SerializationItem<TItem>(new VoidField<TItem>(name), default));
				else
				{
					if (primitive)
						source.Add(new SerializationItem<TItem>(new VoidField<TItem>(name), item));
					else
					{
						var innerSource = new SerializationItemCollection();
						await itemSer.Serialize(item, innerSource, cancellationToken);
						source.Add(new SerializationItem(new VoidField<TItem>(name), innerSource));
					}
				}

				index++;
			}

			return source;
		}

		private static IEnumerable<TItem> GetItems(IEnumerable<TItem> collection)
		{
			if (collection is ISynchronizedCollection<TItem> syncCol)
				collection = syncCol.SyncGet(c => c.ToArray());

			return collection;
		}
	}
}