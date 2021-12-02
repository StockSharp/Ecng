namespace Ecng.Serialization
{
	using System;

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

		public virtual object CreateInstance(ISerializer serializer, SerializationItem source)
		{
			if (serializer is null)
				throw new ArgumentNullException(nameof(serializer), $"Serializer for field '{Field.Name}' is null.");

			if (source is null)
				throw new ArgumentNullException(nameof(source), $"Source value for field '{Field.Name}' is null.");

			/*IsNullable && */
			return source.Value is null ? null : OnCreateInstance(serializer, source.Value);
		}

		public virtual SerializationItem CreateSource(ISerializer serializer, object instance)
		{
			if (serializer is null)
				throw new ArgumentNullException(nameof(serializer), $"Serializer for field '{Field.Name}' is null.");

			//if (!IsNullable && instance is null)
			//	throw new ArgumentNullException(nameof(instance), "Instance value for field '{0}' is null.".Put(Field.Name));

			var source = instance is null ? null : OnCreateSource(serializer, instance);
			return new SerializationItem(Field, source);
		}

		protected internal abstract object OnCreateInstance(ISerializer serializer, object source);
		protected internal abstract object OnCreateSource(ISerializer serializer, object instance);

		#region Serializable<FieldFactory> Members

		protected override void Serialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			source.Add(new SerializationItem<int>(new VoidField<int>("Order"), Order));
			//source.Add(new SerializationItem<bool>(new VoidField<bool>("IsNullable"), IsNullable));
		}

		protected override void Deserialize(ISerializer serializer, FieldList fields, SerializationItemCollection source)
		{
			Order = source["Order"].Value.To<int>();
			//IsNullable = source["IsNullable"].Value.To<bool>();
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

		protected internal abstract I OnCreateInstance(ISerializer serializer, S source);
		protected internal abstract S OnCreateSource(ISerializer serializer, I instance);

		#region FieldFactory Members

		protected internal override object OnCreateInstance(ISerializer serializer, object source)
		{
			return OnCreateInstance(serializer, source.To<S>());
		}

		protected internal override object OnCreateSource(ISerializer serializer, object instance)
		{
			return OnCreateSource(serializer, instance.To<I>());
		}

		#endregion
	}
}