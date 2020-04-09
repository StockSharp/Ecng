namespace Ecng.ComponentModel
{
	using System;
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

	public class ItemsSourceItem<T> : IItemsSourceItem<T>
	{
		public ItemsSourceItem(T val)
		{
			Value = val;
			DisplayName = val.ToString();
		}

		public ItemsSourceItem(string displayName, T val)
		{
			DisplayName = displayName;
			Value = val;
		}

		object IItemsSourceItem.Value => Value;

		public string DisplayName { get; }
		public T Value { get; }

		public override string ToString() => DisplayName;
	}

	public class ItemsSourceItem : ItemsSourceItem<object> {
		public ItemsSourceItem(string displayName, object val) : base(displayName, val) { }

		public static ItemsSourceItem<T> Create<T>(string displayName, T val) => new ItemsSourceItem<T>(displayName, val);
		public static ItemsSourceItem<T> Create<T>(T val) => new ItemsSourceItem<T>(val);
	}

	public interface IItemsSource
	{
		IEnumerable<IItemsSourceItem> Values {get;}
	}

	public interface IItemsSource<out TValue> : IItemsSource
	{
		/// <summary>
		/// Collection of values represented by a ComboBox.
		/// </summary>
		new IEnumerable<IItemsSourceItem<TValue>> Values { get; }
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

			if (type.GetGenericType(typeof(IItemsSource<>)) == null)
				throw new ArgumentException("Type '{0}' must implement the '{1}' interface.".Translate().Put(type, typeof(IItemsSource<>)), nameof(type));

			Type = type;
		}
	}
}