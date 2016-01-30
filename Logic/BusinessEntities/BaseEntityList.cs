namespace Ecng.Logic.BusinessEntities
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	public abstract class BaseEntityList<TEntity, TUser, TRole> : HierarchicalPageLoadList<TEntity>
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
		where TEntity : BaseEntity<TUser, TRole>
	{
		#region Private Fields

		protected static Field CreationDateField;
		protected static Field ModificationDateField;

		#endregion

		#region BaseEntityList.cctor()

		static BaseEntityList()
		{
			var fields = SchemaManager.GetSchema<TEntity>().Fields;
			CreationDateField = fields.Contains("CreationDate") ? fields["CreationDate"] : null;
			ModificationDateField = fields.Contains("ModificationDate") ? fields["ModificationDate"] : null;
		}

		#endregion

		#region BaseEntityList.ctor()

		protected BaseEntityList(IStorage storage)
			: base(storage)
		{
			OverrideItemName = Schema.Name;
		}

		#endregion

		#region FirstCreated

		public virtual TEntity FirstCreated => ReadFirstsCreated(1).FirstOrDefault();

		#endregion

		#region LastCreated

		public virtual TEntity LastCreated => ReadLastsCreated(1).FirstOrDefault();

		#endregion

		#region FirstModified

		public virtual TEntity FirstModified => ReadFirstsModified(1).FirstOrDefault();

		#endregion

		#region LastModified

		public virtual TEntity LastModified => ReadLastsModified(1).FirstOrDefault();

		#endregion

		public IEnumerable<TEntity> ReadFirstsCreated(int count)
		{
			if (CreationDateField == null)
				throw new NotSupportedException();

			return ReadFirsts(count, CreationDateField);
		}

		public IEnumerable<TEntity> ReadLastsCreated(int count)
		{
			if (CreationDateField == null)
				throw new NotSupportedException();

			return ReadLasts(count, CreationDateField);
		}

		public IEnumerable<TEntity> ReadFirstsModified(int count)
		{
			if (ModificationDateField == null)
				throw new NotSupportedException();

			return ReadFirsts(count, ModificationDateField);
		}

		public IEnumerable<TEntity> ReadLastsModified(int count)
		{
			if (ModificationDateField == null)
				throw new NotSupportedException();

			return ReadLasts(count, ModificationDateField);
		}

		private static TUser CurrentUser => LogicHelper<TUser, TRole>.CurrentUser ?? LogicHelper<TUser, TRole>.GetRootObject().GetUsers().Null;

		public string OverrideItemName { get; set; }

		protected override SerializationItemCollection GetOverridedAddSource(TEntity entity)
		{
			return new SerializationItemCollection
			{
				new SerializationItem<long>(new VoidField<long>(OverrideItemName), entity.Id),
				new SerializationItem<DateTime>(new VoidField<DateTime>("CreationDate"), DateTime.Now),
				new SerializationItem<long>(new VoidField<long>("User"), CurrentUser.Id),
				new SerializationItem<bool>(new VoidField<bool>("Deleted"), false),
			};
		}

		protected override SerializationItemCollection GetOverridedRemoveSource(TEntity entity)
		{
			return new SerializationItemCollection { new SerializationItem<long>(new VoidField<long>(OverrideItemName), entity.Id) };
		}

		//public override bool Contains(TEntity item)
		//{
		//	return ReadById(item.Id) != null;
		//}

		protected override void OnAdd(TEntity entity)
		{
			if (entity.User == null)
				entity.User = CurrentUser;

			base.OnAdd(entity);
		}

		protected override void OnUpdate(TEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			entity.ModificationDate = DateTime.Now;

			base.OnUpdate(entity);
		}

		protected override void OnRemove(TEntity entity)
		{
			if (!OverrideCreateDelete)
				entity.Deleted = true;

			base.OnRemove(entity);
		}

		protected void AddFilter(BaseEntity<TUser, TRole> entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			AddFilter(entity.GetType().GetSchema().Name, entity);
		}

		protected void AddFilter(string fieldName, BaseEntity<TUser, TRole> fieldValue)
		{
			AddFilter(Schema.Fields.TryGet(fieldName) ?? new VoidField<long>(fieldName), fieldValue);
		}

		protected void AddFilter(Field field, BaseEntity<TUser, TRole> fieldValue)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));

			if (fieldValue == null)
				throw new ArgumentNullException(nameof(fieldValue));

			AddFilter(field, fieldValue, () => fieldValue.Id);
		}

		private Field _deletedField;

		private Field DeletedField
		{
			get
			{
				if (_deletedField == null && Schema.Fields.Contains("Deleted"))
					_deletedField = Schema.Fields["Deleted"];

				return _deletedField;
			}
		}

		protected TScalar ExecuteScalar<TScalar>(string morph, params BaseEntity<TUser, TRole>[] filterEntities)
		{
			return ExecuteScalar<TScalar>(morph, ConvertToSource(filterEntities));
		}

		protected int ExecuteNonQuery(string morph, params BaseEntity<TUser, TRole>[] filterEntities)
		{
			return ExecuteNonQuery(morph, ConvertToSource(filterEntities));
		}

		protected override IEnumerable<TEntity> ReadAll(string keyFieldsMorph, string valueFieldsMorph, long startIndex, long count, SerializationItemCollection source)
		{
			var newSource = new SerializationItemCollection
			{
				new SerializationItem<string>(new VoidField<string>("OrderBy"), Schema.Identity.Name),
				new SerializationItem<long>(new VoidField<long>("StartIndex"), startIndex),
				new SerializationItem<long>(new VoidField<long>("Count"), count),
			};

			if (DeletedField != null)
				newSource.Add(new SerializationItem<bool>(DeletedField, false));

			newSource.AddRange(source);

			return ReadAll(keyFieldsMorph, valueFieldsMorph, newSource);
		}

		protected IEnumerable<TEntity> ReadAll(string keyFieldsMorph, string valueFieldsMorph, params BaseEntity<TUser, TRole>[] entities)
		{
			return ReadAll(keyFieldsMorph, valueFieldsMorph, ConvertToSource(entities));
		}

		protected TEntity Read(string keyFieldsMorph, string valueFieldsMorph, params BaseEntity<TUser, TRole>[] filterEntities)
		{
			return Read(keyFieldsMorph, valueFieldsMorph, ConvertToSource(filterEntities));
		}

		private static SerializationItemCollection ConvertToSource(IEnumerable<BaseEntity<TUser, TRole>> entities)
		{
			return new SerializationItemCollection(entities.Select(entity => new SerializationItem(new VoidField<long>(entity.GetType().GetSchema().Name), entity.Id)));
		}

		protected override Scope<HierarchicalDatabaseContext> CreateScope(Query query)
		{
			var scope = base.CreateScope(query);

			if (DeletedField != null)
				scope.Value.Source.Add(new SerializationItem<bool>(DeletedField, false));

			return scope;
		}

		public override void Save(TEntity entity)
		{
			if (entity.Id == -1)
				Add(entity);
			else
				Update(entity);
		}
	}
}