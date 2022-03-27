namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<EntityFactory>))]
	public abstract class EntityFactory : Serializable<EntityFactory>
	{
		public abstract bool FullInitialize { get; }

		public abstract Task<object> CreateObject(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken);

		protected override Task Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		protected override Task Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		protected override bool OnEquals(EntityFactory other) => ReferenceEquals(this, other);
	}

	public abstract class EntityFactory<TEntity> : EntityFactory
	{
		public abstract Task<TEntity> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken);

		public override async Task<object> CreateObject(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
			=> await CreateEntity(serializer, source, cancellationToken);
	}
}