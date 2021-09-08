namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	#endregion

	public class SerializationItemCollection : BaseList<SerializationItem>, ICloneable<SerializationItemCollection>
	{
		private readonly Dictionary<string, SerializationItem> _innerDictionary = new();

		public SerializationItemCollection()
		{
		}

		public SerializationItemCollection(IEnumerable<SerializationItem> source)
		{
			this.AddRange(source);
		}

		#region Item

		public SerializationItem this[string name]
		{
			get
			{
				if (!_innerDictionary.TryGetValue(name, out var retVal))
					throw new ArgumentException("Item with name '{0}' doesn't exists.".Put(name), nameof(name));

				return retVal;
			}
		}

		#endregion

		#region Contains

		public bool Contains(string name)
		{
			//return this.Any(item => string.Compare(item.Field.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0);
			return _innerDictionary.ContainsKey(name);
		}

		#endregion

		public SerializationItem TryGetItem(string name)
		{
			return _innerDictionary.TryGetValue(name);
		}

		public bool Remove(string name)
		{
			var item = TryGetItem(name);
			return item != null && base.Remove(item);
		}

		#region BaseCollection<SerializationItem> Members

		protected override bool OnAdding(SerializationItem item)
		{
			if (Contains(item.Field.Name))
				throw new ArgumentException("Item with name '{0}' already added.".Put(item.Field.Name), nameof(item));

			_innerDictionary.Add(item.Field.Name, item);

			return base.OnAdding(item);
		}

		protected override bool OnClearing()
		{
			_innerDictionary.Clear();
			return base.OnClearing();
		}

		protected override bool OnRemoving(SerializationItem item)
		{
			_innerDictionary.Remove(item.Field.Name);
			return base.OnRemoving(item);
		}

		protected override bool OnInserting(int index, SerializationItem item)
		{
			_innerDictionary.Add(item.Field.Name, item);
			return base.OnInserting(index, item);
		}

		#endregion

		#region Implementation of ICloneable

		public SerializationItemCollection Clone()
		{
			var clone = new SerializationItemCollection();

			foreach (var item in this)
				clone.Add(item.Clone());

			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (obj is SerializationItemCollection)
				return this.SequenceEqual((SerializationItemCollection)obj);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}