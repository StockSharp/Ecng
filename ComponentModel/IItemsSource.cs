namespace Ecng.ComponentModel
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Reflection;

	public interface IItemsSourceItem
	{
		string DisplayName {get;}
		object Value {get;}
	}

	public interface IItemsSourceItem<out TValue> : IItemsSourceItem
	{
		new TValue Value {get;}
	}

	public class ItemsSourceItem<T> : NotifiableObject, IItemsSourceItem<T>
	{
		readonly Func<T, string> _valToString;

		object IItemsSourceItem.Value => Value;

		private string _displayName;
		private T _value;

		public ItemsSourceItem(T val, Func<T, string> valToString = null)
		{
			_valToString = valToString ?? (v => v?.GetDisplayName());
			Value = val;
		}

		public ItemsSourceItem(T val, string displayName, Func<T, string> valToString = null)
			: this(val, valToString)
		{
			if(!displayName.IsEmpty())
				DisplayName = displayName;
		}

		protected virtual void UpdateDisplayName()
		{
			DisplayName = _valToString(Value);
		}

		public string DisplayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
				NotifyChanged(nameof(DisplayName));
			}
		}

		public T Value
		{
			get => _value;
			set
			{
				_value = value;
				NotifyChanged(nameof(Value));
				UpdateDisplayName();
			}
		}

		public override string ToString() => DisplayName;
	}

	public class ItemsSourceItem : ItemsSourceItem<object> {
		public ItemsSourceItem(string displayName, object val) : base(val, displayName) { }

		public static ItemsSourceItem<T> Create<T>(T val, string displayName, Func<T, string> valToString = null) => new ItemsSourceItem<T>(val, displayName, valToString);
		public static ItemsSourceItem<T> Create<T>(T val, Func<T, string> valToString = null) => new ItemsSourceItem<T>(val, valToString);
	}

	public interface IItemsSource
	{
		IEnumerable<IItemsSourceItem> Values { get; }

		string ItemToString(object val);
	}

	public interface IItemsSource<out TValue> : IItemsSource
	{
		/// <summary>Collection of values represented by a ComboBox.</summary>
		new IEnumerable<IItemsSourceItem<TValue>> Values { get; }
	}

	public class ItemsSourceBase<T> : IItemsSource<T>
	{
		readonly Lazy<IEnumerable<IItemsSourceItem<T>>> _values;

		IEnumerable<IItemsSourceItem> IItemsSource.Values => Values;

		public IEnumerable<IItemsSourceItem<T>> Values => _values.Value;

		public ItemsSourceBase()
		{
			_values = new Lazy<IEnumerable<IItemsSourceItem<T>>>(
				() => GetNamedValues().Select(nv => ItemsSourceItem.Create(nv.Item2, nv.Item1, ItemToString)).ToArray());
		}

		protected virtual string Format => null;

		string IItemsSource.ItemToString(object val)
		{
			if(!Format.IsEmptyOrWhiteSpace())
				return ItemToString((T)val);

			if (val is IItemsSourceItem item && !item.DisplayName.IsEmpty())
				return item.DisplayName;

			return ItemToString((T)val);
		}

		protected virtual string ItemToString(T val)
		{
			var f = Format;
			if(f == null)
				return val.GetDisplayName();

			return string.Format($"{{0:{f}}}", val);
		}

		protected virtual IEnumerable<T> GetValues()
			=> throw new NotSupportedException();

		protected virtual IEnumerable<(string displayName, T value)> GetNamedValues()
		{
			foreach (var value in GetValues())
			{
				yield return (ItemToString(value), value);
			}
		}
	}

	/// <summary>
	/// Represents an attribute that is set on a property to identify the IItemsSource-derived class that will be used.
	/// </summary>
	public class ItemsSourceAttribute : Attribute
	{
		class EnumSource<T> : ItemsSourceBase<T> where T : Enum
		{
			readonly T[] _items;

			protected override IEnumerable<T> GetValues() => _items;

			public EnumSource()
			{
				var vals = typeof(T).GetValues().ToArray();
				_items = (T[]) typeof(T).CreateArray(vals.Length);

				for(var i=0; i<vals.Length; ++i)
					_items[i] = (T) vals[i];
			}
		}

		/// <summary>
		/// Gets the type to use.
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Allow user input.
		/// </summary>
		public bool IsEditable { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceAttribute"/>.
		/// </summary>
		/// <param name="type">The type to use.</param>
		public ItemsSourceAttribute(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			Type = 
				typeof(IItemsSource).IsAssignableFrom(type) ? type :
				type.IsEnum ? typeof(EnumSource<>).MakeGenericType(type) :
				throw new ArgumentException("Type '{0}' must implement the '{1}' interface or be an enum.".Translate().Put(type, typeof(IItemsSource)), nameof(type));
		}
	}
}