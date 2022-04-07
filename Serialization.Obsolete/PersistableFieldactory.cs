namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	public class PersistableFieldactory<TEntity> : FieldFactory<TEntity, SerializationItemCollection>
		where TEntity : IPersistable, new()
	{
		public PersistableFieldactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override async ValueTask<TEntity> OnCreateInstance(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			var storage = await serializer.GetLegacySerializer<SettingsStorage>().Deserialize(source, cancellationToken);
			return storage.Load<TEntity>();
		}

		protected internal override async ValueTask<SerializationItemCollection> OnCreateSource(ISerializer serializer, TEntity instance, CancellationToken cancellationToken)
		{
			var storage = instance.Save();
			var source = new SerializationItemCollection();

			await serializer.GetLegacySerializer<SettingsStorage>().Serialize(storage, source, cancellationToken);

			return source;
		}
	}
}