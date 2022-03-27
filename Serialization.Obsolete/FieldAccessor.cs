namespace Ecng.Serialization
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<FieldAccessor>))]
	public abstract class FieldAccessor : Serializable<FieldAccessor>
	{
		protected FieldAccessor(Field field)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
		}

		public Field Field { get; }

		public abstract object GetValue(object entity);
		public abstract object SetValue(object entity, object value);

		protected override Task Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		protected override Task Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		protected override bool OnEquals(FieldAccessor other)
		{
			return object.ReferenceEquals(this, other);
		}
	}

	public abstract class FieldAccessor<TEntity> : FieldAccessor
	{
		protected FieldAccessor(Field field)
			: base(field)
		{
		}

		public override object GetValue(object entity)
		{
			return GetValue((TEntity)entity);
		}

		public override object SetValue(object entity, object value)
		{
			return SetValue((TEntity)entity, value);
		}

		public abstract object GetValue(TEntity entity);
		public abstract TEntity SetValue(TEntity entity, object value);
	}

	class PrimitiveFieldAccessor<TEntity> : FieldAccessor<TEntity>
	{
		public PrimitiveFieldAccessor(Field field)
			: base(field)
		{
		}

		public override object GetValue(TEntity entity)
		{
			return entity;
		}

		public override TEntity SetValue(TEntity entity, object value)
		{
			throw new NotSupportedException();
		}
	}
}