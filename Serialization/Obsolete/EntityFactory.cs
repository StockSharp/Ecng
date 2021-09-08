namespace Ecng.Serialization
{
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
			return ReferenceEquals(this, other);
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
}