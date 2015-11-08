namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	#endregion

	public class FieldList : BaseList<Field>
	{
		private readonly Dictionary<string, Field> _innerDictionary = new Dictionary<string, Field>();

		#region FieldList.ctor()

		public FieldList()
			: this(ArrayHelper.Empty<Field>())
		{
		}

		public FieldList(params Field[] fields)
			: this((IEnumerable<Field>)fields)
		{
			//base.AddRange(fields);
		}

		public FieldList(IEnumerable<Field> fields)
		{
			this.AddRange(fields);
		}

		#endregion

		#region Item

		public Field this[string name]
		{
			get
			{
				var field = TryGet(name);

				if (field == null)
					throw new ArgumentException("Item with name '{0}' doesn't exists.".Put(name), nameof(name));

				return field;
			}
		}

		#endregion

		public Field TryGet(string fieldName)
		{
			return _innerDictionary.TryGetValue(fieldName);
		}

		#region Contains

		public bool Contains(string name)
		{
			//return base.Exists(field => string.Compare(field.Name, nName, StringComparison.InvariantCultureIgnoreCase) == 0);
			return _innerDictionary.ContainsKey(name);
		}

		#endregion

		#region NonIdentityFields

		private FieldList _nonIdentityFields;

		public FieldList NonIdentityFields
		{
			get { return _nonIdentityFields ?? (_nonIdentityFields = new FieldList(this.Where(field => !(field is IdentityField)))); }
		}

		#endregion

		#region IndexFields

		private FieldList _indexFields;

		public FieldList IndexFields
		{
			get { return _indexFields ?? (_indexFields = new FieldList(this.Where(field => field.IsIndex))); }
		}

		#endregion

		#region ReadOnlyFields

		private FieldList _readOnlyFields;

		public FieldList ReadOnlyFields
		{
			get { return _readOnlyFields ?? (_readOnlyFields = new FieldList(this.Where(field => field.IsReadOnly))); }
		}

		#endregion

		#region NonReadOnlyFields

		private FieldList _nonReadOnlyFields;

		public FieldList NonReadOnlyFields
		{
			get { return _nonReadOnlyFields ?? (_nonReadOnlyFields = new FieldList(this.Where(field => !field.IsReadOnly))); }
		}

		#endregion

		#region SerializableFields

		private FieldList _serializableFields;

		public FieldList SerializableFields
		{
			get
			{
				return _serializableFields ?? (_serializableFields = new FieldList(this.Where(field =>
				{
					if (field.Factory == null)
						throw new InvalidOperationException("Field '{0}' doesn't have factory.".Put(field.Name));

					return field.Factory.SourceType != typeof(VoidType);
				})));
			}
		}

		#endregion

		#region RelationManyFields

		private FieldList _relationManyFields;

		public FieldList RelationManyFields
		{
			get { return _relationManyFields ?? (_relationManyFields = new FieldList(this.Where(field => field.IsRelationMany()))); }
		}

		#endregion

		#region BaseCollection<Field> Members

		protected override bool OnAdding(Field item)
		{
			if (Contains(item.Name))
				throw new ArgumentException("Item with name '{0}' already added.".Put(item.Name), nameof(item));

			ResetCachedStates();
			_innerDictionary.Add(item.Name, item);

			return base.OnAdding(item);
		}

		protected override bool OnClearing()
		{
			ResetCachedStates();
			_innerDictionary.Clear();
			return base.OnClearing();
		}

		protected override bool OnRemoving(Field item)
		{
			ResetCachedStates();
			_innerDictionary.Remove(item.Name);
			return base.OnRemoving(item);
		}

		protected override bool OnInserting(int index, Field item)
		{
			if (Contains(item.Name))
				throw new ArgumentException("Item with name '{0}' already added.".Put(item.Name), nameof(item));

			ResetCachedStates();
			_innerDictionary.Add(item.Name, item);

			return base.OnInserting(index, item);
		}

		#endregion

		#region ResetCachedStates

		private void ResetCachedStates()
		{
			_nonIdentityFields = null;
			_indexFields = null;
			_readOnlyFields = null;
			_nonReadOnlyFields = null;
		}

		#endregion
	}
}