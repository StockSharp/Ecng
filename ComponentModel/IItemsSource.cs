namespace Ecng.ComponentModel
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Localization;

	public interface IItemsSourceItem
	{
		string DisplayName {get;}
		string Description {get;}
		object Value {get;}
	}

	public interface IItemsSourceItem<out TValue> : IItemsSourceItem
	{
		new TValue Value {get;}
	}

	public class ItemsSourceItem<T> : NotifiableObject, IItemsSourceItem<T>
	{
		readonly Func<T, string> _valToString;
		readonly Func<T, string> _valToDescription;

		object IItemsSourceItem.Value => Value;

		private string _displayName, _description;
		private T _value;

		public ItemsSourceItem(T val, Func<T, string> valToDisplayName = null, Func<T, string> valToDescription = null)
		{
			_valToString = valToDisplayName ?? (v => v?.GetDisplayName());
			_valToDescription = valToDescription;

			Value = val;
		}

		public ItemsSourceItem(T val, string displayName, Func<T, string> valToString = null, Func<T, string> valToDescription = null)
			: this(val, valToString, valToDescription)
		{
			if(!displayName.IsEmpty())
				DisplayName = displayName;
		}

		public ItemsSourceItem(T val, string displayName, string description, Func<T, string> valToString = null, Func<T, string> valToDescription = null)
			: this(val, valToString, valToDescription)
		{
			if(!displayName.IsEmpty())
				DisplayName = displayName;

			if(!description.IsEmpty())
				Description = description;
		}

		protected virtual void UpdateDisplayName() => DisplayName = _valToString(Value);

		protected virtual void UpdateDescription() => Description = _valToDescription != null ? _valToDescription.Invoke(Value) : Value is Enum e ? e.GetFieldDescription() : null;

		public string DisplayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
				NotifyChanged(nameof(DisplayName));
			}
		}

		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				NotifyChanged(nameof(Description));
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
				UpdateDescription();
			}
		}

		public override string ToString() => DisplayName;
	}

	public class ItemsSourceItem : ItemsSourceItem<object> {
		public ItemsSourceItem(string displayName, object val) : base(val, displayName) { }

		public static ItemsSourceItem<T> Create<T>(T val, string displayName, Func<T, string> valToString = null, Func<T, string> valToDescription = null)
			=> new ItemsSourceItem<T>(val, displayName, valToString, valToDescription);

		public static ItemsSourceItem<T> Create<T>(T val, string displayName, string description, Func<T, string> valToString = null, Func<T, string> valToDescription = null)
			=> new ItemsSourceItem<T>(val, displayName, description, valToString, valToDescription);

		public static ItemsSourceItem<T> Create<T>(T val, Func<T, string> valToString = null, Func<T, string> valToDescription = null)
			=> new ItemsSourceItem<T>(val, valToString, valToDescription);
	}

	public interface IItemsSource
	{
		IEnumerable<IItemsSourceItem> Values { get; }

		string ItemToString(object val);
		string ItemToDescription(object val);
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
				() => GetDescribedValues().Select(nv => ItemsSourceItem.Create(nv.value, nv.displayName, nv.description, ItemToString, ItemToDescription)).ToArray());
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

		string IItemsSource.ItemToDescription(object val)
		{
			if (val is IItemsSourceItem item && !item.Description.IsEmpty())
				return item.Description;

			return ItemToDescription((T)val);
		}

		protected virtual string ItemToString(T val)
		{
			var f = Format;
			if(f == null)
				return val.GetDisplayName();

			return string.Format($"{{0:{f}}}", val);
		}

		protected virtual string ItemToDescription(T val) => val is Enum e ? e.GetFieldDescription() : null;

		protected virtual IEnumerable<T> GetValues()
			=> throw new NotSupportedException();

		protected virtual IEnumerable<(string displayName, T value)> GetNamedValues()
		{
			foreach (var value in GetValues())
			{
				yield return (ItemToString(value), value);
			}
		}

		protected virtual IEnumerable<(string displayName, string description, T value)> GetDescribedValues()
		{
			foreach (var (name, val) in GetNamedValues())
			{
				yield return (name, ItemToDescription(val), val);
			}
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EnumSource<T> : ItemsSourceBase<T>
		where T : Enum
	{
		private readonly IEnumerable<T> _values;

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override IEnumerable<T> GetValues() => _values;

		private static readonly IEnumerable<T> _excluded = Enumerator.GetValues<T>().ExcludeObsolete().ToArray();
		private static readonly IEnumerable<T> _all = Enumerator.GetValues<T>();

		/// <summary>
		/// 
		/// </summary>
		public EnumSource()
			: this(true)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="excludeObsolete"></param>
		public EnumSource(bool excludeObsolete)
			: this(excludeObsolete ? _excluded : _all)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="values"></param>
		public EnumSource(IEnumerable<T> values)
		{
			_values = values ?? throw new ArgumentNullException(nameof(values));
		}
	}

	/// <summary>
	/// Represents an attribute that is set on a property to identify the IItemsSource-derived class that will be used.
	/// </summary>
	public class ItemsSourceAttribute : Attribute
	{
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
				typeof(IItemsSource).IsAssignableFrom(type)
					? type
					: type.IsEnum
						? typeof(EnumSource<>).Make(type)
						: throw new ArgumentException("Type '{0}' must implement the '{1}' interface or be an enum.".Translate().Put(type, typeof(IItemsSource)), nameof(type));
		}
	}
}