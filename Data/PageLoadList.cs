namespace Ecng.Data
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Web.UI.WebControls;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	public abstract class PageLoadList<TEntity> : BaseListEx<TEntity>
	{
		#region private class PageLoadListEnumerator

		private sealed class PageLoadListEnumerator : BaseEnumerator<PageLoadList<TEntity>, TEntity>
		{
			#region Private Fields

			private const long _bufferSize = 20;

			private long _startIndex;
			private ICollection<TEntity> _temporaryBuffer;
			private int _posInBuffer;

			#endregion

			#region PageLoadListEnumerator.ctor()

			public PageLoadListEnumerator(PageLoadList<TEntity> list)
				: base(list)
			{
			}

			#endregion

			#region BaseEnumerator<PageLoadList<E>, E> Members

			public override void Reset()
			{
				base.Reset();
				_startIndex = 0;
				_posInBuffer = -1;
				_temporaryBuffer = null;
			}

			protected override TEntity ProcessMove(ref bool canProcess)
			{
				if (_temporaryBuffer == null || _posInBuffer >= (_temporaryBuffer.Count - 1))
				{
					if (_startIndex < base.Source.Count)
					{
						_temporaryBuffer = (ICollection<TEntity>)base.Source.GetRange(_startIndex, _bufferSize);

						if (_temporaryBuffer.IsEmpty())
							throw new InvalidOperationException();

						_startIndex += _temporaryBuffer.Count;
						_posInBuffer = 0;
						return _temporaryBuffer.First();
					}
					else
					{
						canProcess = false;
						return default(TEntity);
					}
				}
				else
					return _temporaryBuffer.ElementAt(++_posInBuffer);
			}

			#endregion
		}

		#endregion

		#region Private Fields

		private bool _bulkInitialized;
		private readonly object _cachedEntitiesLock = new object();
		private readonly Dictionary<object, TEntity> _cachedEntities = new Dictionary<object, TEntity>();
		//private readonly Dictionary<Field, IEnumerable<E>> _orderByCachedEntities = new Dictionary<Field, IEnumerable<E>>();
		private int? _count;

		#endregion

		protected PageLoadList(Database database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.Database = database;
		}

		#region Schema

		private static Schema _schema;

		public static Schema Schema
		{
			get { return _schema ?? (_schema = SchemaManager.GetSchema<TEntity>()); }
		}

		#endregion

		public bool BulkLoad { get; set; }
		public Database Database { get; private set; }
		public bool CacheCount { get; set; }

		public void ChangeCachedCount(int diff)
		{
			_count += diff;
		}

		public void ResetCache()
		{
			lock (_cachedEntitiesLock)
			{
				_cachedEntities.Clear();
				_bulkInitialized = false;
			}

			_count = null;
		}

		#region Item

		public TEntity this[object id]
		{
			get { return ReadById(id); }
		}

		#endregion

		public TEntity ReadById(object id)
		{
			if (id == null)
				throw new ArgumentNullException("id");

			ThrowExceptionIfDatabaseNull();

			if (this.BulkLoad)
			{
				GetRange();
				return _cachedEntities.TryGetValue(id);
			}
			else
				return Read(new SerializationItem(Schema.Identity, id));
		}

		#region Update

		public void Update(TEntity entity)
		{
			Update(entity, Schema.Fields.NonIdentityFields.SerializableFields);
		}

		public void Update(TEntity entity, Field valueField)
		{
			Update(entity, new FieldCollection(valueField));
		}

		public void Update(TEntity entity, FieldCollection valueFields)
		{
			Update(entity, Schema.Identity, valueFields);
		}

		public void Update(TEntity entity, Field keyField, FieldCollection valueFields)
		{
			Update(entity, new FieldCollection(keyField), valueFields);
		}

		public void Update(TEntity entity, FieldCollection keyFields, FieldCollection valueFields)
		{
			OnUpdate(entity, keyFields, valueFields);
		}

		#endregion

		#region BaseListEx<E> Members

		public override int Count
		{
			get
			{
				ThrowExceptionIfDatabaseNull();

				if (this.BulkLoad)
				{
					if (!_bulkInitialized)
						GetRange(0, 1 /* passed count's value will be ingored and set into OnGetCount() */);

					return _cachedEntities.Count;
				}
				else
				{
					if ((this.CacheCount && _count == null) || !this.CacheCount)
						_count = (int)OnGetCount();

					return _count ?? 0;
				}
			}
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override void Add(TEntity item)
		{
			ThrowExceptionIfDatabaseNull();
			OnCreate(item);

			if (this.BulkLoad)
			{
				if (_bulkInitialized)
					_cachedEntities.Add(GetId(item), item);
				else
					GetRange();
			}

			_count++;
		}

		public override void Clear()
		{
			ThrowExceptionIfDatabaseNull();
			OnDeleteAll();

			if (this.BulkLoad)
			{
				GetRange();
				_cachedEntities.Clear();
			}

			_count = 0;
		}

		public override bool Contains(TEntity item)
		{
			return this.Any(arg => arg.Equals(item));
		}

		public override void CopyTo(TEntity[] array, int index, int count)
		{
			((ICollection<TEntity>)GetRange(index, count)).CopyTo(array, 0);
		}

		public override bool Remove(TEntity item)
		{
			//ThrowExceptionIfReadOnly();
			Remove(item, Schema.Identity);
			return true;
		}

		public override IEnumerable<TEntity> GetRange(long startIndex, long count, string sortExpression, SortDirection directions)
		{
			var orderBy = sortExpression.IsEmpty() ? Schema.Identity : Schema.Fields[sortExpression];
			return ReadAll(startIndex, count, orderBy, directions);
		}

		public override IEnumerator<TEntity> GetEnumerator()
		{
			return new PageLoadListEnumerator(this);
		}

		public override int IndexOf(TEntity item)
		{
			if (this.BulkLoad)
				throw new NotImplementedException();
			else
				throw new NotSupportedException();
		}

		public override void Insert(int index, TEntity item)
		{
			Add(item);
		}

		public override void RemoveAt(int index)
		{
			if (this.BulkLoad)
				Remove(GetRange().ElementAt(index));
			else
				throw new NotSupportedException();
		}

		public override TEntity this[int index]
		{
			get
			{
				if (this.BulkLoad)
				{
					return GetRange().ElementAt(index);
				}
				else
					throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		public void RemoveById(object id)
		{
			Remove(new SerializationItemCollection { new SerializationItem(Schema.Identity, id) });
		}

		public void Remove(TEntity item, Field field)
		{
			Remove(item, new FieldCollection(field));
		}

		public void Remove(TEntity item, FieldCollection fields)
		{
			ThrowExceptionIfDatabaseNull();

			var by = new SerializationItemCollection();

			foreach (var field in fields)
				by.Add(new SerializationItem(field, field.GetAccessor<TEntity>().GetValue(item)));

			Remove(by);
			_count--;
		}

		public void Remove(SerializationItemCollection by)
		{
			ThrowExceptionIfDatabaseNull();
			OnDelete(by.Clone());

			if (this.BulkLoad)
			{
				foreach (var item in by)
				{
					if (item.Field is IdentityField)
					{
						GetRange();
						_cachedEntities.Remove(item.Value);
						break;
					}
				}
			}
		}

		#region Virtual CRUD Methods

		protected virtual long OnGetCount()
		{
			ThrowExceptionIfDatabaseNull();
			return this.Database.GetEntityCount(Schema);
		}

		//protected virtual DatabaseCommand GetCommand(Schema schema, SqlCommandTypes type, FieldCollection keyFields, FieldCollection valueFields, SerializationItemCollection source)
		//{
		//    ThrowExceptionIfDatabaseNull();
		//    return this.Database.GetCommand(this.Schema, type, keyFields, valueFields, source);
		//}

		protected virtual void OnCreate(TEntity entity)
		{
			ThrowExceptionIfDatabaseNull();
			this.Database.Create(entity);
		}

		protected virtual TEntity OnRead(SerializationItemCollection by)
		{
			ThrowExceptionIfDatabaseNull();
			return this.Database.Read<TEntity>(by);
		}

		protected virtual IEnumerable<TEntity> OnReadAll(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			ThrowExceptionIfDatabaseNull();
			return this.Database.ReadAll<TEntity>(startIndex, count, orderBy, direction);
		}

		protected virtual void OnUpdate(TEntity entity, FieldCollection keyFields, FieldCollection valueFields)
		{
			ThrowExceptionIfDatabaseNull();
			this.Database.Update(entity, keyFields, valueFields);
		}

		protected virtual void OnDelete(SerializationItemCollection by)
		{
			ThrowExceptionIfDatabaseNull();
			this.Database.Delete(by);
		}

		protected virtual void OnDeleteAll()
		{
			ThrowExceptionIfDatabaseNull();
			this.Database.DeleteAll(Schema);
		}

		#endregion

		public IEnumerable<TEntity> ReadFirsts(long count, Field orderBy)
		{
			return ReadAll(0, count, orderBy, SortDirection.Ascending);
		}

		public IEnumerable<TEntity> ReadLasts(long count, Field orderBy)
		{
			return ReadAll(0, count, orderBy, SortDirection.Descending);
		}

		public TEntity Read(SerializationItem by)
		{
			if (by == null)
				throw new ArgumentNullException("by");

			return Read(new SerializationItemCollection { by });
		}

		public TEntity Read(SerializationItemCollection by)
		{
			return OnRead(by);
		}

		#region ReadAll

		//public IList<E> ReadAll()
		//{
		//    return ReadAll(0, this.Count, this.Schema.Identity, SortDirection.Ascending);
		//}

		public IEnumerable<TEntity> ReadAll(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			if (orderBy == null)
				throw new ArgumentNullException("orderBy");

			if (count == 0)
				return new List<TEntity>();

			var oldStartIndex = startIndex;
			var oldCount = count;

			if (this.BulkLoad)
			{
				if (_bulkInitialized)
				{
					Func<TEntity, object> keySelector = entity => orderBy.GetAccessor<TEntity>().GetValue(entity);
					return new ListEx<TEntity>((direction == SortDirection.Ascending ? _cachedEntities.Values.OrderBy(keySelector) : _cachedEntities.Values.OrderByDescending(keySelector)).Skip((int)startIndex).Take((int)count));
					//return new ListEx<E>(_orderByCachedEntities.SafeAdd(orderBy, key => _cachedEntities.Values.OrderBy(entity => key.GetAccessor<E>().GetValue(entity))).Skip((int)startIndex).Take((int)count));
				}
				else
				{
					startIndex = 0;
					count = OnGetCount();
				}
			}

			ThrowExceptionIfDatabaseNull();

			var entities = OnReadAll(startIndex, count, orderBy, direction);

			if (this.BulkLoad)
			{
				lock (_cachedEntitiesLock)
				{
					if (!_bulkInitialized)
					{
						//_cachedEntities = new Dictionary<object, E>();

						foreach (TEntity entity in entities)
							_cachedEntities.Add(GetId(entity), entity);

						_bulkInitialized = true;

						entities = entities.Skip((int)oldStartIndex).Take((int)oldCount).ToList();
					}
				}
			}

			return entities;
		}

		#endregion

		private object GetId(TEntity entity)
		{
			return this.Database.GetSerializer<TEntity>().GetId(entity);
		}

		#region ThrowExceptionIfDatabaseNull

		private void ThrowExceptionIfDatabaseNull()
		{
			if (this.Database == null)
				throw new InvalidOperationException();
		}

		#endregion
	}
}