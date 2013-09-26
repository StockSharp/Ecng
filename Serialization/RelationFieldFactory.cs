namespace Ecng.Serialization
{
	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Reflection;

	[Serializable]
	public abstract class RelationFieldFactory<TInstance, TSource> : FieldFactory<TInstance, TSource>
	{
		protected RelationFieldFactory(Field field, int order)
			: base(field, order)
		{
			_storage = ConfigManager.GetService<IStorage>();
		}

		private readonly IStorage _storage;

		public IStorage Storage
		{
			get
			{
				var current = Scope<IStorage>.Current;
				return current == null ? _storage : current.Value;
			}
		}
	}

	[Serializable]
	public class RelationSingleFieldFactory<TInstance, TSource> : RelationFieldFactory<TInstance, TSource>
	{
		public RelationSingleFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override TInstance OnCreateInstance(ISerializer serializer, TSource source)
		{
			return Storage.GetById<TInstance>(source);
		}

		protected internal override TSource OnCreateSource(ISerializer serializer, TInstance instance)
		{
			if (instance.IsNull())
				throw new ArgumentNullException("instance", "Field '{0}' in schema '{1}' isn't initialized.".Put(Field.Name, Field.Schema.EntityType));

			return (TSource)serializer.GetSerializer<TInstance>().GetId(instance);
		}
	}

	[Serializable]
	public class RelationManyFieldFactory<TEntity, TItem> : RelationFieldFactory<RelationManyList<TItem>, VoidType>
	{
		public RelationManyFieldFactory(Field field, int order, Type underlyingListType, bool bulkLoad, bool cacheCount, int bufferSize)
			: base(field, order)
		{
			if (underlyingListType == null)
				throw new ArgumentNullException("underlyingListType");

			UnderlyingListType = underlyingListType;
			BulkLoad = bulkLoad;
			CacheCount = cacheCount;
			BufferSize = bufferSize;
		}

		public Type UnderlyingListType { get; private set; }
		public bool BulkLoad { get; private set; }
		public bool CacheCount { get; private set; }
		public int BufferSize { get; private set; }

		private FastInvoker<VoidType, object[], RelationManyList<TItem>> _listCreator;

		public FastInvoker<VoidType, object[], RelationManyList<TItem>> ListCreator
		{
			get
			{
				return _listCreator ?? (_listCreator = FastInvoker<VoidType, object[], RelationManyList<TItem>>
					.Create(UnderlyingListType.GetMember<ConstructorInfo>(typeof(IStorage), typeof(TEntity))));
			}
		}

		public override object CreateInstance(ISerializer serializer, SerializationItem source)
		{
			return OnCreateInstance(serializer, null);
		}

		protected internal override RelationManyList<TItem> OnCreateInstance(ISerializer serializer, VoidType source)
		{
			var context = Scope<SerializationContext>.Current.Value;
			var list = ListCreator.Ctor(new[] { Storage, context.Entity });
			list.BulkLoad = BulkLoad;
			list.CacheCount = CacheCount;
			list.DelayAction = SerializationContext.DelayAction;
			list.BufferSize = BufferSize;
			return list;
		}

		protected internal override VoidType OnCreateSource(ISerializer serializer, RelationManyList<TItem> instance)
		{
			return null;
		}
	}

	public abstract class RelationAttribute : ReflectionFieldFactoryAttribute
	{
		protected RelationAttribute()
		{
			IdentityType = typeof(long);
		}

		public Type IdentityType { get; set; }
	}

	public class RelationSingleAttribute : RelationAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			// var sourceType = IdentityType;//Schema.GetSchema(field.Type).Identity.Type;
			return typeof(RelationSingleFieldFactory<,>).Make(field.Type, IdentityType);
		}
	}

	public class RelationManyAttribute : RelationAttribute
	{
		protected RelationManyAttribute()
		{
			BufferSize = 20;
		}

		public RelationManyAttribute(Type underlyingListType)
			: this()
		{
			if (underlyingListType == null)
				throw new ArgumentNullException("underlyingListType");

			UnderlyingListType = underlyingListType;
		}

		public Type UnderlyingListType { get; protected set; }
		public bool BulkLoad { get; set; }
		public bool CacheCount { get; set; }
		public int BufferSize { get; set; }

		protected override Type GetFactoryType(Field field)
		{
			return typeof(RelationManyFieldFactory<,>).Make(field.Schema.EntityType, field.Type.GetGenericType(typeof(RelationManyList<>)).GetGenericArguments()[0]);
		}

		protected override object[] GetArgs(Field field)
		{
			return new object[] { UnderlyingListType, BulkLoad, CacheCount, BufferSize };
		}
	}
}