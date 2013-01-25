namespace Ecng.Data
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Serialization;

	#endregion

#if !SILVERLIGHT
	[Serializable]
	public class PageLoadFieldFactory<TEntity, TItem> : FieldFactory<PageLoadList<TItem>, VoidType>
	{
		#region PageLoadFieldFactory.ctor()

		public PageLoadFieldFactory(Field field, int order, bool isNullable, Type listType, bool bulkLoad, bool cacheCount)
			: base(field, order, isNullable)
		{
			if (listType == null)
				throw new ArgumentNullException("listType");

			this.ListType = listType;
			this.BulkLoad = bulkLoad;
			this.CacheCount = cacheCount;
		}

		#endregion

		public Type ListType { get; private set; }
		public bool BulkLoad { get; private set; }
		public bool CacheCount { get; private set; }

		#region ListCreator

		private FastInvoker<VoidType, object[], PageLoadList<TItem>> _listCreator;

		public FastInvoker<VoidType, object[], PageLoadList<TItem>> ListCreator
		{
			get
			{
				if (_listCreator == null)
					_listCreator = FastInvoker<VoidType, object[], PageLoadList<TItem>>.Create(this.ListType.GetMember<ConstructorInfo>(typeof(Database), typeof(TEntity)));

				return _listCreator;
			}
		}

		#endregion

		#region FieldFactory<PageLoadList<TItem>> Members

		protected override PageLoadList<TItem> OnCreateInstance(ISerializer serializer, VoidType source)
		{
			var context = Scope<MappingContext>.Current.Value;
			var list = this.ListCreator.Ctor(new[] { context.Command.Database, context.Entity });
			list.BulkLoad = this.BulkLoad;
			list.CacheCount = this.CacheCount;
			return list;
		}

		protected override VoidType OnCreateSource(ISerializer serializer, PageLoadList<TItem> instance)
		{
			//return new SerializationItemCollection();
			return null;
		}

		#endregion

		#region Serializable Members

		protected override void Serialize(ISerializer serializer, FieldCollection fields, SerializationItemCollection source)
		{
			source.Add(new SerializationItem(new VoidField<string>("listType"), this.ListType.To<string>()));
			source.Add(new SerializationItem(new VoidField<bool>("bulkLoad"), this.BulkLoad));
			base.Serialize(serializer, fields, source);
		}

		protected override void Deserialize(ISerializer serializer, FieldCollection fields, SerializationItemCollection source)
		{
			this.ListType = source["listType"].Value.To<Type>();
			this.BulkLoad = (bool)source["bulkLoad"].Value;
			base.Deserialize(serializer, fields, source);
		}

		#endregion
	}
#endif

	[AttributeUsage(ReflectionHelper.Members)]
	public class PageLoadAttribute : ReflectionFieldFactoryAttribute
	{
		public Type ListType { get; set; }
		public bool BulkLoad { get; set; }
		public bool CacheCount { get; set; }

		protected override Type GetFactoryType(Field field)
		{
#if SILVERLIGHT
			return typeof(PrimitiveFieldFactory<,>).Make(field.Type, typeof(VoidType));
#else
			return typeof(PageLoadFieldFactory<,>).Make(field.Schema.EntityType, field.Type.GetItemType());
#endif
		}

#if !SILVERLIGHT
		protected override object[] GetArgs(Field field)
		{
			return new object[] { this.ListType, this.BulkLoad, this.CacheCount };
		}
#endif
	}
}