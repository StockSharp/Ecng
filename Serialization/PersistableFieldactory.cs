namespace Ecng.Serialization
{
	public class PersistableFieldactory<TEntity> : FieldFactory<TEntity, SerializationItemCollection>
		where TEntity : IPersistable, new()
	{
		public PersistableFieldactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override TEntity OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			var storage = serializer.GetSerializer<SettingsStorage>().Deserialize(source);
			return storage.Load<TEntity>();
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, TEntity instance)
		{
			var storage = instance.Save();
			var source = new SerializationItemCollection();

			serializer.GetSerializer<SettingsStorage>().Serialize(storage, source);

			return source;
		}
	}
}