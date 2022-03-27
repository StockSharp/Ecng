namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[AttributeUsage(ReflectionHelper.Members, AllowMultiple = true)]
	public sealed class NameOverrideAttribute : Attribute
	{
		public NameOverrideAttribute(string oldName, string newName)
		{
			OldName = oldName;
			NewName = newName;
		}

		public string OldName { get; }
		public string NewName { get; }
	}

	[Serializable]
	public class InnerSchemaFieldFactory<TEntity> : FieldFactory<TEntity, SerializationItemCollection>
	{
		private readonly bool _nullWhenAllEmpty;

		#region InnerSchemaFieldFactory.ctor()

		public InnerSchemaFieldFactory(Field field, int order, bool nullWhenAllEmpty)
			: base(field, order)
		{
			_nullWhenAllEmpty = nullWhenAllEmpty;
		}

		public InnerSchemaFieldFactory(Field field, int order)
			: this(field, order, true)
		{
		}

		#endregion

		#region ComplexFieldFactory<E> Members

		protected internal override async Task<TEntity> OnCreateInstance(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			if (_nullWhenAllEmpty && source.All(c => c.Value is null))
				return default;

			return (TEntity)await GetSerializer(serializer, source).Deserialize(source, cancellationToken);
		}

		protected internal override async Task<SerializationItemCollection> OnCreateSource(ISerializer serializer, TEntity instance, CancellationToken cancellationToken)
		{
			var source = new SerializationItemCollection();
			await GetSerializer(serializer, source, instance).Serialize(instance, source, cancellationToken);
			return source;
		}

		#endregion

		private ILegacySerializer GetSerializer(ISerializer serializer, SerializationItemCollection source, TEntity instance = default)
		{
			if (serializer is null)
				throw new ArgumentNullException(nameof(serializer));

			var entityType = typeof(TEntity);

			if (Field.IsUnderlying)
			{
				if (instance.IsNull())
					entityType = source["UnderlyingType"].Value.To<Type>();
				else
				{
					entityType = instance.GetType();
					source.Add(new SerializationItem<Type>(new VoidField<Type>("UnderlyingType"), entityType));
				}
			}

			var retVal = serializer.GetLegacySerializer(entityType);
			retVal.IgnoreFields.AddRange(Field.InnerSchemaIgnoreFields);
			return retVal;
		}
	}

	public sealed class InnerSchemaAttribute : ReflectionFieldFactoryAttribute
	{
		private bool _nullWhenAllEmpty = true;

		public bool NullWhenAllEmpty
		{
			get => _nullWhenAllEmpty;
			set => _nullWhenAllEmpty = value;
		}

		#region ReflectionFieldFactoryAttribute Members

		protected override Type GetFactoryType(Field field)
		{
			return typeof(InnerSchemaFieldFactory<>).Make(field.Type);
		}

		protected override object[] GetArgs(Field field)
		{
			return new object[] { NullWhenAllEmpty };
		}

		#endregion
	}
}