namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using System.Linq;

#if SILVERLIGHT
	public
#endif
	class RealCollectionFieldFactory<TCollection, TItem> : CollectionFieldFactory<TCollection>
		where TCollection : IEnumerable<TItem>
	{
		public RealCollectionFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override TCollection OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			throw new NotSupportedException();
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, TCollection instance)
		{
			var source = new SerializationItemCollection();
			var primitive = typeof(TItem).IsPrimitive();
			var itemSer = serializer.GetSerializer<TItem>();

			var index = 0;
			foreach (var item in GetItems(instance))
			{
				var name = index.ToString();

				if (item.IsNull())
					source.Add(new SerializationItem<TItem>(new VoidField<TItem>(name), default(TItem)));
				else
				{
					if (primitive)
						source.Add(new SerializationItem<TItem>(new VoidField<TItem>(name), item));
					else
					{
						var innerSource = new SerializationItemCollection();
						itemSer.Serialize(item, innerSource);
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