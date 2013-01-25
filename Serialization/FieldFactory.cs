﻿namespace Ecng.Serialization
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
			if (field == null)
				throw new ArgumentNullException("field");

			Field = field;
			Order = order;
			//IsNullable = isNullable;
		}

		#endregion

		public Field Field { get; private set; }
		public int Order { get; private set; }
		//public bool IsNullable { get; private set; }

		public abstract Type InstanceType { get; }
		public abstract Type SourceType { get; }

		public virtual object CreateInstance(ISerializer serializer, SerializationItem source)
		{
			if (serializer == null)
				throw new ArgumentNullException("serializer", "Serializer for field '{0}' is null.".Put(Field.Name));

			if (source == null)
				throw new ArgumentNullException("source", "Source value for field '{0}' is null.".Put(Field.Name));

			/*IsNullable && */
			return source.Value == null ? null : OnCreateInstance(serializer, source.Value);
		}

		public virtual SerializationItem CreateSource(ISerializer serializer, object instance)
		{
			if (serializer == null)
				throw new ArgumentNullException("serializer", "Serializer for field '{0}' is null.".Put(Field.Name));

			//if (!IsNullable && instance == null)
			//	throw new ArgumentNullException("instance", "Instance value for field '{0}' is null.".Put(Field.Name));

			var source = instance == null ? null : OnCreateSource(serializer, instance);
			return new SerializationItem(Field, source);
		}

		protected abstract internal object OnCreateInstance(ISerializer serializer, object source);
		protected abstract internal object OnCreateSource(ISerializer serializer, object instance);

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

		public override Type InstanceType { get { return typeof(I); } }
		public override Type SourceType { get { return typeof(S); } }

		protected abstract internal I OnCreateInstance(ISerializer serializer, S source);
		protected abstract internal S OnCreateSource(ISerializer serializer, I instance);

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