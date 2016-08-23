namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	public abstract class HierarchicalPageLoadList<TEntity> : RelationManyList<TEntity>
	{
		private readonly List<Tuple<Field, object, Func<object>>> _filters = new List<Tuple<Field, object, Func<object>>>();

		protected HierarchicalPageLoadList(IStorage storage)
			: base(storage)
		{
			Morph = GetType().Name.Replace("List", string.Empty);

			if (Morph == typeof(TEntity).Name)
				Morph = string.Empty;

			Recycle = true;
		}

		public string Morph { get; set; }
		public bool OverrideCreateDelete { get; set; }

		public bool Recycle { get; set; }

		public Query CreateQuery { get; set; }
		public Query UpdateQuery { get; set; }
		public Query RemoveQuery { get; set; }
		public Query ReadAllQuery { get; set; }
		public Query ReadByIdQuery { get; set; }
		public Query CountQuery { get; set; }
		public Query ClearQuery { get; set; }

		#region AddFilter

		protected void AddFilter(string fieldName, object fieldValue)
		{
			if (fieldName.IsEmpty())
				throw new ArgumentNullException(nameof(fieldName));

			if (fieldValue == null)
				throw new ArgumentNullException(nameof(fieldValue));

			AddFilter(new VoidField(fieldName, fieldValue.GetType()), null, () => fieldValue);
		}

		protected void AddFilter(Field field, object fieldValue, Func<object> func)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));

			if (func == null)
				throw new ArgumentNullException(nameof(func));

			_filters.Add(new Tuple<Field, object, Func<object>>(field, fieldValue, func));
		}

		protected void AddFilter(params Tuple<string, object>[] pairs)
		{
			if (pairs == null)
				throw new ArgumentNullException(nameof(pairs));

			foreach (var pair in pairs)
			{
				var fieldName = pair.Item1;
				var fieldValue = pair.Item2;
				AddFilter(new VoidField(fieldName, fieldValue.GetType()), fieldValue, () => fieldValue);
			}
		}

		#endregion

		protected virtual SerializationItemCollection GetOverridedAddSource(TEntity entity)
		{
			throw new NotSupportedException();
		}

		protected virtual SerializationItemCollection GetOverridedRemoveSource(TEntity entity)
		{
			throw new NotSupportedException();
		}

		protected virtual SerializationItemCollection GetOverridedRemoveAllSource()
		{
			return new SerializationItemCollection();
		}

		protected override long OnGetCount()
		{
			using (CreateScope(CountQuery))
				return base.OnGetCount();
		}

		protected override void OnAdd(TEntity entity)
		{
			if (OverrideCreateDelete)
			{
				var source = GetOverridedAddSource(entity);
				FillSource(source);
				ExecuteNonQuery(Morph + "Create", source);
				
			}
			else
			{
				FillEntity(entity);

				using (CreateScope(CreateQuery))
					base.OnAdd(entity);
			}
		}

		protected override void OnRemove(TEntity entity)
		{
			if (OverrideCreateDelete)
			{
				var by = GetOverridedRemoveSource(entity);
				FillSource(by);
				ExecuteNonQuery(Morph + "Delete", by);
			}
			else
			{
				if (RemoveQuery != null || !Recycle)
				{
					using (CreateScope(RemoveQuery))
						base.OnRemove(entity);
				}
				else
					Update(entity);
			}
		}

		protected override IEnumerable<TEntity> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			using (CreateScope(ReadAllQuery))
				return base.OnGetGroup(startIndex, count, orderBy, direction);
		}

		protected override void OnUpdate(TEntity entity)
		{
			if (OverrideCreateDelete)
				throw new NotSupportedException();

			FillEntity(entity);

			using (CreateScope(UpdateQuery))
				base.OnUpdate(entity);
		}

		protected override void OnClear()
		{
			if (OverrideCreateDelete)
			{
				var by = GetOverridedRemoveAllSource();
				FillSource(by);
				ExecuteNonQuery(Morph + "DeleteAll", by);
			}
			else
			{
				if (ClearQuery == null)
					throw new NotSupportedException();

				using (CreateScope(ClearQuery))
					base.OnClear();
			}
		}

		private void FillEntity(TEntity entity)
		{
			foreach (var filter in _filters)
			{
				var accessor = filter.Item1.GetAccessor<TEntity>();

				if (accessor.GetValue(entity) == null)
					entity = accessor.SetValue(entity, filter.Item2);
			}
		}

		protected virtual Scope<HierarchicalDatabaseContext> CreateScope(Query query)
		{
			var source = new SerializationItemCollection();
			FillSource(source);

			var context = query == null ? new HierarchicalDatabaseContext(Morph, Schema, source) : new HierarchicalDatabaseContext(query, Schema, source);
			return new Scope<HierarchicalDatabaseContext>(context);
		}

		protected void FillSource(SerializationItemCollection source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			foreach (var filter in _filters)
				source.Add(new SerializationItem(filter.Item1, filter.Item3()));
		}

		protected TEntity ReadBy(params object[] entities)
		{
			using (CreateScope(ReadByIdQuery))
				return Read(new SerializationItemCollection(ConvertToPairs(entities).Select(pair => new SerializationItem(pair.Item1, pair.Item2))));
		}

		protected TEntity Read(Field field, object value)
		{
			using (CreateScope(ReadByIdQuery))
				return Read(new SerializationItemCollection { new SerializationItem(field, value) });
		}

		protected Database Database => (Database)Storage;

		protected TEntity Read(string keyFieldsMorph, string valueFieldsMorph, SerializationItemCollection source)
		{
			return Database != null
				? Database.Read<TEntity>(GetCommand(SqlCommandTypes.ReadBy, keyFieldsMorph, valueFieldsMorph), source)
				: default(TEntity);
		}

		protected virtual IEnumerable<TEntity> ReadAll(string keyFieldsMorph, string valueFieldsMorph, long startIndex, long count, SerializationItemCollection source)
		{
			var newSource = new SerializationItemCollection
			{
				new SerializationItem<string>(new VoidField<string>("OrderBy"), Schema.Identity.Name),
				new SerializationItem<long>(new VoidField<long>("StartIndex"), startIndex),
				new SerializationItem<long>(new VoidField<long>("Count"), count),
			};

			newSource.AddRange(source);

			return ReadAll(keyFieldsMorph, valueFieldsMorph, newSource);
		}

		protected IEnumerable<TEntity> ReadAll(string keyFieldsMorph, string valueFieldsMorph, SerializationItemCollection source)
		{
			return Database.ReadAll<TEntity>(GetCommand(SqlCommandTypes.ReadAll, keyFieldsMorph, valueFieldsMorph), source);
		}

		protected TScalar ExecuteScalar<TScalar>(string morph, SerializationItemCollection source)
		{
			return Database.GetCommand(Query.Execute(Schema, morph), Schema, null, null).ExecuteScalar<TScalar>(source);
		}

		protected int ExecuteNonQuery(string morph, SerializationItemCollection source)
		{
			return Database.GetCommand(Query.Execute(Schema, morph), Schema, null, null).ExecuteNonQuery(source);
		}

		protected SerializationItemCollection ExecuteRow(string morph, SerializationItemCollection source)
		{
			return Database.GetCommand(Query.Execute(Schema, morph), Schema, null, null).ExecuteRow(source);
		}

		protected SerializationItemCollection ExecuteTable(string morph, SerializationItemCollection source)
		{
			return Database.GetCommand(Query.Execute(Schema, morph), Schema, null, null).ExecuteTable(source);
		}

		private static IEnumerable<Tuple<Field, object>> ConvertToPairs(IEnumerable<object> entities)
		{
			return entities.Select(entity => new Tuple<Field, object>(Schema.Fields[entity.GetType().GetSchema().Name], entity));
		}

		protected DatabaseCommand GetCommand(SqlCommandTypes commandType, string keyFieldsMorph, string valueFieldsMorph)
		{
			return Database.GetCommand(Query.Execute(Schema, commandType, keyFieldsMorph, valueFieldsMorph), Schema, null, null);
		}
	}
}
