namespace Ecng.Serialization
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<FieldFactory>))]
	public abstract class FieldFactory : Serializable<FieldFactory>
	{
		#region FieldFactory.ctor()

		protected FieldFactory(Field field, int order)
		{
			Field = field ?? throw new ArgumentNullException(nameof(field));
			Order = order;
			//IsNullable = isNullable;
		}

		#endregion

		public Field Field { get; }
		public int Order { get; private set; }
		//public bool IsNullable { get; private set; }

		public abstract Type InstanceType { get; }
		public abstract Type SourceType { get; }

		public virtual ValueTask<object> CreateInstance(ISerializer serializer, SerializationItem source, CancellationToken cancellationToken)
		{
			if (serializer is null)
				throw new ArgumentNullException(nameof(serializer), $"Serializer for field '{Field.Name}' is null.");

			if (source is null)
				throw new ArgumentNullException(nameof(source), $"Source value for field '{Field.Name}' is null.");

			/*IsNullable && */
			return source.Value is null ? new(default(object)) : OnCreateInstance(serializer, source.Value, cancellationToken);
		}

		public virtual async ValueTask<SerializationItem> CreateSource(ISerializer serializer, object instance, CancellationToken cancellationToken)
		{
			if (serializer is null)
				throw new ArgumentNullException(nameof(serializer), $"Serializer for field '{Field.Name}' is null.");

			//if (!IsNullable && instance is null)
			//	throw new ArgumentNullException(nameof(instance), "Instance value for field '{0}' is null.".Put(Field.Name));

			var source = instance is null ? null : await OnCreateSource(serializer, instance, cancellationToken);
			return new SerializationItem(Field, source);
		}

		protected internal abstract ValueTask<object> OnCreateInstance(ISerializer serializer, object source, CancellationToken cancellationToken);
		protected internal abstract ValueTask<object> OnCreateSource(ISerializer serializer, object instance, CancellationToken cancellationToken);

		#region Serializable<FieldFactory> Members

		protected override ValueTask Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			source.Add(new SerializationItem<int>(new VoidField<int>("Order"), Order));
			//source.Add(new SerializationItem<bool>(new VoidField<bool>("IsNullable"), IsNullable));
			return default;
		}

		protected override ValueTask Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			Order = source["Order"].Value.To<int>();
			//IsNullable = source["IsNullable"].Value.To<bool>();
			return default;
		}

		#endregion

		#region Equatable<FieldFactory> Members

		protected override bool OnEquals(FieldFactory other)
		{
			return
					Order == other.Order &&
					//IsNullable == other.IsNullable &&
					Field == other.Field;
		}

		#endregion

		public override int GetHashCode()
		{
			return Field.GetHashCode();
		}
	}

	[Serializable]
	public abstract class FieldFactory<I, S> : FieldFactory
	{
		#region FieldFactory.ctor()

		protected FieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		#endregion

		public override Type InstanceType => typeof(I);
		public override Type SourceType => typeof(S);

		protected internal abstract ValueTask<I> OnCreateInstance(ISerializer serializer, S source, CancellationToken cancellationToken);
		protected internal abstract ValueTask<S> OnCreateSource(ISerializer serializer, I instance, CancellationToken cancellationToken);

		#region FieldFactory Members

		protected internal override async ValueTask<object> OnCreateInstance(ISerializer serializer, object source, CancellationToken cancellationToken)
		{
			return await OnCreateInstance(serializer, source.To<S>(), cancellationToken);
		}

		protected internal override async ValueTask<object> OnCreateSource(ISerializer serializer, object instance, CancellationToken cancellationToken)
		{
			return await OnCreateSource(serializer, instance.To<I>(), cancellationToken);
		}

		#endregion
	}
}