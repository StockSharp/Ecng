namespace Ecng.Serialization
{
	public class PersistableFieldactory<TEntity> : FieldFactory<TEntity, SerializationItemCollection>
		where TEntity : IPersistable
	{
		public PersistableFieldactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override TEntity OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			var storage = new SettingsStorage();

			foreach (var item in source)
				storage.Add(item.Field.Name, item.Value);

			return storage.LoadEntire<TEntity>();
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, TEntity instance)
		{
			var storage = instance.SaveEntire(false);

			var source = new SerializationItemCollection();

			foreach (var pair in storage)
			{
				source.Add(new SerializationItem(new VoidField(pair.Key, pair.Value == null ? typeof(object) : pair.Value.GetType()), pair.Value));
			}

			return source;
		}
	}
}