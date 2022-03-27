namespace Ecng.Serialization
{
	using System;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Reflection;

	[Serializable]
	public abstract class RelationFieldFactory<TInstance, TSource, TId> : FieldFactory<TInstance, TSource>
	{
		protected RelationFieldFactory(Field field, int order)
			: base(field, order)
		{
			_storage = ConfigManager.GetService<IStorage<TId>>() ?? (IStorage<TId>)ConfigManager.GetService<IStorage>();
		}

		private readonly IStorage<TId> _storage;

		public IStorage<TId> Storage
		{
			get
			{
				var current = Scope<IStorage<TId>>.Current;
				return current is null ? _storage : current.Value;
			}
		}
	}

	[Serializable]
	public class RelationSingleFieldFactory<TInstance, TSource> : RelationFieldFactory<TInstance, TSource, TSource>
	{
		public RelationSingleFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override Task<TInstance> OnCreateInstance(ISerializer serializer, TSource source, CancellationToken cancellationToken)
		{
			return Storage.GetById<TInstance>(source, cancellationToken);
		}

		protected internal override Task<TSource> OnCreateSource(ISerializer serializer, TInstance instance, CancellationToken cancellationToken)
		{
			if (instance.IsNull())
				throw new ArgumentNullException(nameof(instance), $"Field '{Field.Name}' in schema '{Field.Schema.EntityType}' isn't initialized.");

			return ((TSource)serializer.GetLegacySerializer<TInstance>().GetId(instance)).FromResult();
		}
	}

	[Serializable]
	public class RelationManyFieldFactory<TOwner, TItem, TId> : RelationFieldFactory<RelationManyList<TItem, TId>, VoidType, TId>
	{
		public RelationManyFieldFactory(Field field, int order, Type underlyingListType, bool bulkLoad, bool cacheCount, int bufferSize)
			: base(field, order)
		{
			UnderlyingListType = underlyingListType ?? throw new ArgumentNullException(nameof(underlyingListType));
			BulkLoad = bulkLoad;
			CacheCount = cacheCount;
			BufferSize = bufferSize;
		}

		public Type UnderlyingListType { get; }
		public bool BulkLoad { get; }
		public bool CacheCount { get; }
		public int BufferSize { get; }

		private FastInvoker<VoidType, object[], RelationManyList<TItem, TId>> _listCreator;

		public FastInvoker<VoidType, object[], RelationManyList<TItem, TId>> ListCreator => _listCreator ??= FastInvoker<VoidType, object[], RelationManyList<TItem, TId>>
			.Create(UnderlyingListType.GetMember<ConstructorInfo>(typeof(IStorage<TId>), typeof(TOwner)));

		public override async Task<object> CreateInstance(ISerializer serializer, SerializationItem source, CancellationToken cancellationToken)
		{
			return await OnCreateInstance(serializer, null, cancellationToken);
		}

		protected internal override Task<RelationManyList<TItem, TId>> OnCreateInstance(ISerializer serializer, VoidType source, CancellationToken cancellationToken)
		{
			var context = Scope<SerializationContext>.Current.Value;
			var list = ListCreator.Ctor(new[] { Storage, context.Entity });
			list.BulkLoad = BulkLoad;
			list.CacheCount = CacheCount;
			//list.DelayAction = SerializationContext.DelayAction;
			list.BufferSize = BufferSize;
			return list.FromResult();
		}

		protected internal override Task<VoidType> OnCreateSource(ISerializer serializer, RelationManyList<TItem, TId> instance, CancellationToken cancellationToken)
		{
			return default(VoidType).FromResult();
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
			UnderlyingListType = underlyingListType ?? throw new ArgumentNullException(nameof(underlyingListType));
		}

		public Type UnderlyingListType { get; protected set; }
		public bool BulkLoad { get; set; }
		public bool CacheCount { get; set; }
		public int BufferSize { get; set; }

		protected override Type GetFactoryType(Field field)
		{
			var args = field.Type.GetGenericType(typeof(RelationManyList<,>)).GetGenericArguments();
			return typeof(RelationManyFieldFactory<,,>).Make(field.Schema.EntityType, args[0], args[1]);
		}

		protected override object[] GetArgs(Field field)
		{
			return new object[] { UnderlyingListType, BulkLoad, CacheCount, BufferSize };
		}
	}
}