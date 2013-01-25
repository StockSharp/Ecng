namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Web.UI.WebControls;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using Wintellect.PowerCollections;

	#endregion

	public class ForumDataContext
	{
		#region ForumDataContext.ctor()

		public ForumDataContext(string morph, SerializationItemCollection source)
		{
			this.Morph = morph;
			this.Source = source;
		}

		#endregion

		public string Morph { get; private set; }
		public SerializationItemCollection Source { get; private set; }
	}

	[Serializable]
	public abstract class BaseEntityList<E> : PageLoadList<E>
		where E : BaseEntity
	{
		#region Private Fields

		private readonly static Field _creationDateField;
		private readonly static Field _modificationDateField;

		private string _morph;
		private IEnumerable<Triple<Field, object, Func<object>>> _filterValues;

		#endregion

		#region BaseEntityList.cctor()

		static BaseEntityList()
		{
			var fields = Schema.GetSchema<E>().Fields;
			_creationDateField = Schema.GetSchema<E>().Fields["CreationDate"];
			_modificationDateField = fields.ContainsField("ModificationDate") ? Schema.GetSchema<E>().Fields["ModificationDate"] : null;
		}

		#endregion

		protected BaseEntityList(Database database)
			: base(database)
		{
		}

		#region OverrideCreateDelete

		public bool OverrideCreateDelete { get; set; }

		#endregion

		#region FirstCreated

		public virtual E FirstCreated
		{
			get { return ReadFirstsCreated(1).FirstOrDefault(); }
		}

		#endregion

		#region LastCreated

		public virtual E LastCreated
		{
			get { return ReadLastsCreated(1).FirstOrDefault(); }
		}

		#endregion

		#region FirstModified

		public virtual E FirstModified
		{
			get { return ReadFirstsModified(1).FirstOrDefault(); }
		}

		#endregion

		#region LastModified

		public virtual E LastModified
		{
			get { return ReadLastsModified(1).FirstOrDefault(); }
		}

		#endregion

		public IList<E> ReadFirstsCreated(int count)
		{
			return base.ReadFirsts(count, _creationDateField);
		}

		public IList<E> ReadLastsCreated(int count)
		{
			return base.ReadLasts(count, _creationDateField);
		}

		public IList<E> ReadFirstsModified(int count)
		{
			return base.ReadFirsts(count, _modificationDateField);
		}

		public IList<E> ReadLastsModified(int count)
		{
			return base.ReadLasts(count, _modificationDateField);
		}

		internal E this[Identities id]
		{
			get { return base[(long)id]; }
		}

		private static User CurrentUser { get { return ForumRootObject.CurrentUser() ?? ForumRootObject.Instance.Users.Null; } }

		#region PageLoadList<E> Members

		protected override long OnGetEntityCount()
		{
			using (CreateScope())
				return base.OnGetEntityCount();
		}

		protected override void OnCreate(E entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			if (!this.OverrideCreateDelete)
			{
				//var currentUser = ForumRootObject.CurrentUser();

				if (entity.User == null)
					entity.User = CurrentUser;
				//else if (currentUser != null)
				//	throw new ArgumentException("entity");

				if (_filterValues != null)
				{
					foreach (var filterValue in _filterValues)
						filterValue.First.GetAccessor<E>().SetValue(entity, filterValue.Second);
				}

				base.OnCreate(entity);
			}
			else
			{
				var source = new SerializationItemCollection
				{
					new SerializationItem<long>(new VoidField<long>(Schema.Name), entity.Id),
					new SerializationItem<DateTime>(new VoidField<DateTime>("CreationDate"), DateTime.Now),
					new SerializationItem<long>(new VoidField<long>("User"), CurrentUser.Id),
					new SerializationItem<bool>(new VoidField<bool>("Deleted"), false),
				};

				foreach (var filterValue in _filterValues)
					source.Add(new SerializationItem(filterValue.First, filterValue.Third()));

				Execute(_morph + "Create", source);
			}
		}

		protected override IList<E> OnReadAll(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			using (CreateScope())
				return base.OnReadAll(startIndex, count, orderBy, direction);
		}
		
		protected override void OnUpdate(E entity, FieldCollection keyFields, FieldCollection valueFields)
		{
			if (this.OverrideCreateDelete)
				throw new NotSupportedException();

			if (entity == null)
				throw new ArgumentNullException("entity");

			entity.ModificationDate = DateTime.Now;

			//_auditPrevValues.TryGetValue(base.Schema.Identity.Accessor.GetValue(entity), id =>
			//{
			//    var oldValue;
			//});

			base.OnUpdate(entity, keyFields, valueFields);
		}

		protected override void OnDelete(SerializationItemCollection by)
		{
			if (this.OverrideCreateDelete)
			{
				//var source = new SerializationItemCollection(by);

				//foreach (var item in by)
				//	source.Add(new SerializationItem(item.Field, item.Value));

				var idItem = by[Schema.Identity.Name];

				by.Remove(idItem);
				by.Add(new SerializationItem<long>(new VoidField<long>(Schema.Name), (long)idItem.Value));

				foreach (var filterValue in _filterValues)
					by.Add(new SerializationItem(filterValue.First, filterValue.Third()));

				Execute(_morph + "Delete", by);
			}
			else
			{
				//var idPair = by.First(pair => pair.First is IdentityField);
				var entity = base.ReadById(by.First(item => item.Field is IdentityField).Value);
				entity.Deleted = true;
				base.Update(entity);
				//base.OnDelete(by);
			}
		}

		protected override void OnDeleteAll()
		{
			if (this.OverrideCreateDelete)
			{
				var by = new SerializationItemCollection();

				foreach (var filterValue in _filterValues)
					by.Add(new SerializationItem(filterValue.First, filterValue.Third()));

				Execute(_morph + "DeleteAll", by);
			}
			else
			{
				throw new NotSupportedException();
				//using (CreateScope())
				//	base.OnDeleteAll();
			}
		}

		#endregion

		#region Init

		//protected void InitializeFilter(params BaseEntity[] filterEntities)
		//{
		//    InitializeFilter(ConvertToPairs(filterEntities));
		//}

		protected void InitializeFilter(BaseEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			//InitializeFilter(ConvertToPairs(filterEntities));
			InitializeFilter(entity.GetType().Name, entity);
		}

		protected void InitializeFilter(string fieldName, BaseEntity fieldValue)
		{
			InitializeFilter(Schema.Fields[fieldName], fieldValue);
		}

		protected void InitializeFilter(Field field, BaseEntity fieldValue)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			if (fieldValue == null)
				throw new ArgumentNullException("fieldValue");

			InitializeFilter(field, fieldValue, () => fieldValue.Id);
		}

		protected void InitializeFilter(string fieldName, object fieldValue)
		{
			if (fieldName.IsEmpty())
				throw new ArgumentNullException("fieldName");

			if (fieldValue == null)
				throw new ArgumentNullException("fieldValue");

			InitializeFilter(new VoidField(fieldName, fieldValue.GetType()), null, () => fieldValue);
		}

		private void InitializeFilter(Field field, object fieldValue, Func<object> func)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			if (func == null)
				throw new ArgumentNullException("func");

			_filterValues = new[] { new Triple<Field, object, Func<object>>(field, fieldValue, func) };
			InitializeMorph();
		}

		protected void InitializeFilter(Pair<string, object>[] values)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			_filterValues = values.Select(pair => new Triple<Field, object, Func<object>>(new VoidField(pair.First, pair.Second.GetType()), pair.Second, () => pair.Second));
			InitializeMorph();
		}

		protected void InitializeMorph()
		{
			_morph = base.GetType().Name.Replace("List", string.Empty);

			if (_morph == typeof(E).Name)
				_morph = string.Empty;
		}

		#endregion

		private Field _deletedField;

		private Field DeletedField
		{
			get
			{
				if (_deletedField == null && Schema.Fields.ContainsField("Deleted"))
					_deletedField = Schema.Fields["Deleted"];

				return _deletedField;
			}
		}

		private Scope<ForumDataContext> CreateScope()
		{
			var source = new SerializationItemCollection();

			if (_filterValues != null)
			{
				foreach (var filterValue in _filterValues)
					source.Add(new SerializationItem(filterValue.First, filterValue.Third()));
			}

			if (this.DeletedField != null)
				source.Add(new SerializationItem<bool>(this.DeletedField, false));

			return new Scope<ForumDataContext>(new ForumDataContext(_morph, source));
		}

		protected E ReadBy(params BaseEntity[] entities)
		{
			return base.Read(new SerializationItemCollection(ConvertToPairs(entities).Select(pair => new SerializationItem(pair.First, pair.Second))));
		}

		#region Read

		protected E Read(string morph, params BaseEntity[] filterEntities)
		{
			return Read(morph, ConvertToSource(filterEntities));
		}

		protected E Read(Field field, object value)
		{
			return base.Read(new SerializationItemCollection { new SerializationItem(field, value) });
		}

		protected E Read(string morph, SerializationItemCollection source)
		{
			if (base.Database != null)
				return base.Database.Read<E>(GetCommand(SqlCommandTypes.ReadBy, morph, source), source);
			else
				return default(E);
		}

		#endregion

		#region ReadAll

		protected ICollection<E> ReadAll(string morph, params BaseEntity[] entities)
		{
			return ReadAll(morph, ConvertToSource(entities));
		}

		protected ICollection<E> ReadAll(string morph, long startIndex, long count, SerializationItemCollection source)
		{
			var newSource = new SerializationItemCollection
			{
				new SerializationItem<string>(new VoidField<string>("OrderBy"), Schema.Identity.Name),
				new SerializationItem<long>(new VoidField<long>("StartIndex"), startIndex),
				new SerializationItem<long>(new VoidField<long>("Count"), count),
			};

			if (this.DeletedField != null)
				newSource.Add(new SerializationItem<bool>(this.DeletedField, false));

			newSource.AddRange(source);

			return ReadAll(morph, newSource);
		}

		protected ICollection<E> ReadAll(string morph, SerializationItemCollection source)
		{
			return base.Database.ReadAll<E>(GetCommand(SqlCommandTypes.ReadAll, morph, source), source);
		}

		#endregion

		#region Execute

		protected S Execute<S>(string morph, params BaseEntity[] filterEntities)
		{
			return Execute<S>(morph, ConvertToSource(filterEntities));
		}

		protected S Execute<S>(string morph, SerializationItemCollection source)
		{
			return base.Database.GetCommand(Query.Execute(Schema, morph), source).Execute<S>(source);
		}

		protected int Execute(string morph, params BaseEntity[] filterEntities)
		{
			return Execute(morph, ConvertToSource(filterEntities));
		}

		protected int Execute(string morph, SerializationItemCollection source)
		{
			return base.Database.GetCommand(Query.Execute(Schema, morph), source).Execute(source);
		}

		#endregion

		public void Save(E entity)
		{
			if (entity.Id == -1)
				base.Add(entity);
			else
				base.Update(entity);
		}

		private static IEnumerable<Pair<Field, BaseEntity>> ConvertToPairs(IEnumerable<BaseEntity> entities)
		{
			return entities.Select(entity => new Pair<Field, BaseEntity>(Schema.Fields[entity.GetType().Name], entity));
		}

		private static SerializationItemCollection ConvertToSource(IEnumerable<BaseEntity> entities)
		{
			return new SerializationItemCollection(entities.Select(entity => new SerializationItem(new VoidField<long>(entity.GetType().Name), entity.Id)));
		}

		private DatabaseCommand GetCommand(SqlCommandTypes commandType, string keyFieldsMorph, SerializationItemCollection source)
		{
			return base.Database.GetCommand(Query.Execute(Schema, commandType, keyFieldsMorph, null), source);
		}
	}
}