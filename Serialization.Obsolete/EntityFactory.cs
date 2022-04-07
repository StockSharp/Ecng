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

		public abstract ValueTask<object> CreateObject(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken);

		protected override ValueTask Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> default;

		protected override ValueTask Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> default;

		protected override bool OnEquals(EntityFactory other) => ReferenceEquals(this, other);
	}

	public abstract class EntityFactory<TEntity> : EntityFactory
	{
		public abstract ValueTask<TEntity> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken);

		public override async ValueTask<object> CreateObject(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
			=> await CreateEntity(serializer, source, cancellationToken);
	}
}