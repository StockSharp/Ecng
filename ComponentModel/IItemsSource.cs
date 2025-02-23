namespace Ecng.ComponentModel
{
	using System;
	using System.Linq;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Collections;
	using System.Reflection;

	using Ecng.Common;

	/// <summary>
	/// Represents an item used as a source for UI components, with properties for display, description, icon, and state.
	/// </summary>
	public interface IItemsSourceItem : INotifyPropertyChangedEx
	{
		/// <summary>
		/// Gets the display name of the item.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the description of the item.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets the URI of the icon representing the item.
		/// </summary>
		Uri Icon { get; }

		/// <summary>
		/// Gets a value indicating whether the item is marked as obsolete.
		/// </summary>
		bool IsObsolete { get; }

		/// <summary>
		/// Gets the underlying value of the item.
		/// </summary>
		object Value { get; }
	}

	/// <summary>
	/// Represents an item source item with a strongly typed value.
	/// </summary>
	/// <typeparam name="TValue">The type of the value of the item.</typeparam>
	public interface IItemsSourceItem<out TValue> : IItemsSourceItem
	{
		/// <summary>
		/// Gets the strongly typed value of the item.
		/// </summary>
		new TValue Value { get; }
	}

	/// <summary>
	/// Represents an item source item with a strongly typed value. Provides functionality for property change notifications.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value of the item.</param>
	/// <param name="getDisplayName">Function to get the display name of the item.</param>
	/// <param name="getDescription">Function to get the description of the item.</param>
	/// <param name="iconUri">The URI of the icon representing the item.</param>
	/// <param name="isObsolete">A value indicating whether the item is obsolete.</param>
	public class ItemsSourceItem<T>(T value, Func<string> getDisplayName, Func<string> getDescription, Uri iconUri, bool isObsolete) : NotifiableObject, IItemsSourceItem<T>
	{
		private readonly Func<string> _getDisplayName = getDisplayName ?? throw new ArgumentNullException(nameof(getDisplayName));
		private readonly Func<string> _getDescription = getDescription ?? throw new ArgumentNullException(nameof(getDescription));

		object IItemsSourceItem.Value => Value;

		/// <summary>
		/// Gets the strongly typed value of the item.
		/// </summary>
		public T Value { get; } = value;

		/// <summary>
		/// Gets the display name of the item.
		/// </summary>
		public string DisplayName => _getDisplayName();

		/// <summary>
		/// Gets the description of the item.
		/// </summary>
		public string Description => _getDescription();

		/// <summary>
		/// Gets the icon URI of the item.
		/// </summary>
		public Uri Icon { get; } = iconUri;

		/// <summary>
		/// Gets a value indicating whether the item is marked as obsolete.
		/// </summary>
		public bool IsObsolete { get; } = isObsolete;

		/// <summary>
		/// Returns the display name of the item.
		/// </summary>
		/// <returns>The display name.</returns>
		public override string ToString() => DisplayName;
	}

	/// <summary>
	/// Represents a source of items.
	/// </summary>
	public interface IItemsSource
	{
		/// <summary>
		/// Gets the collection of items.
		/// </summary>
		IEnumerable<IItemsSourceItem> Values { get; }

		/// <summary>
		/// Gets the type of the value represented by the items.
		/// </summary>
		Type ValueType { get; }

		/// <summary>
		/// Gets a value indicating whether obsolete items should be excluded.
		/// </summary>
		bool ExcludeObsolete { get; }

		/// <summary>
		/// Gets the sort order for the items.
		/// </summary>
		ListSortDirection? SortOrder { get; }

		/// <summary>
		/// Creates a new item using the specified value.
		/// </summary>
		/// <param name="value">The value for creating the new item.</param>
		/// <returns>The newly created item.</returns>
		IItemsSourceItem CreateNewItem(object value);
	}

	/// <summary>
	/// Represents a strongly typed source of items.
	/// </summary>
	/// <typeparam name="TValue">The type of the value represented in the items.</typeparam>
	public interface IItemsSource<TValue> : IItemsSource
	{
		/// <summary>
		/// Gets the collection of strongly typed items.
		/// </summary>
		new IEnumerable<IItemsSourceItem<TValue>> Values { get; }

		/// <summary>
		/// Creates a new strongly typed item using the specified value.
		/// </summary>
		/// <param name="value">The value for creating the new item.</param>
		/// <returns>The newly created strongly typed item.</returns>
		IItemsSourceItem<TValue> CreateNewItem(TValue value);
	}

	/// <summary>
	/// Represents the base implementation for an items source containing values of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the values in the source.</typeparam>
	public class ItemsSourceBase<T> : IItemsSource<T>
	{
		private readonly T[] _values;
		private readonly Lazy<IEnumerable<IItemsSourceItem<T>>> _items;
		private readonly Func<IItemsSourceItem, bool> _filter;

		private readonly Func<T, string> _getName;
		private readonly Func<T, string> _getDescription;

		/// <summary>
		/// Gets a value indicating whether obsolete items should be excluded.
		/// </summary>
		public bool ExcludeObsolete { get; }

		/// <summary>
		/// Gets the sort order for sorting the items.
		/// </summary>
		public ListSortDirection? SortOrder { get; }

		/// <summary>
		/// Gets the collection of items.
		/// </summary>
		IEnumerable<IItemsSourceItem> IItemsSource.Values => Values;

		/// <summary>
		/// Gets the collection of strongly typed items.
		/// </summary>
		public IEnumerable<IItemsSourceItem<T>> Values => _items.Value;

		/// <summary>
		/// Gets the type of the value.
		/// </summary>
		public virtual Type ValueType => typeof(T);

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceBase{T}"/> class using the specified values.
		/// </summary>
		/// <param name="values">The collection of values or items.</param>
		/// <param name="excludeObsolete">A value indicating whether obsolete items should be excluded.</param>
		/// <param name="sortOrder">The sort order for the items.</param>
		/// <param name="filter">A filter function to apply on items.</param>
		/// <param name="getName">A function to retrieve the display name from a value.</param>
		/// <param name="getDescription">A function to retrieve the description from a value.</param>
		public ItemsSourceBase(IEnumerable values, bool excludeObsolete, ListSortDirection? sortOrder, Func<IItemsSourceItem, bool> filter, Func<T, string> getName, Func<T, string> getDescription)
		{
			SortOrder = sortOrder;
			ExcludeObsolete = excludeObsolete;
			_filter = filter;
			_getName = getName;
			_getDescription = getDescription;

			var objects = values?.Cast<object>().ToArray();
			if (objects != null)
			{
				if (objects.All(o => o is T))
				{
					_values = [.. objects.Cast<T>()];
					_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => CreateItems(GetValues()));
				}
				else if (objects.All(o => o is IItemsSourceItem<T>))
				{
					var itemsArr = objects.Cast<IItemsSourceItem<T>>().ToArray();
					_values = [.. itemsArr.Select(item => item.Value)];
					_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => FilterItems(itemsArr));
				}
				else if (objects.All(o => o is IItemsSourceItem iisi && iisi.Value is T))
				{
					var itemsArr = objects.Cast<IItemsSourceItem>().Select(CreateNewItem).ToArray();
					_values = [.. itemsArr.Select(item => item.Value)];
					_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => FilterItems(itemsArr));
				}
				else
				{
					throw new ArgumentException($"{nameof(values)} is expected to contain either {typeof(T).Name} or {nameof(IItemsSourceItem)}<{typeof(T).Name}> items (mix not supported). actual types found: {objects.Select(o => o.GetType().Name).Distinct().Join(",")}");
				}
			}
			else
			{
				if (typeof(T).IsEnum)
					_values = [.. Enumerator.GetValues<T>()];

				_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => CreateItems(GetValues()));
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceBase{T}"/> class.
		/// </summary>
		/// <param name="excludeObsolete">A value indicating whether obsolete items should be excluded.</param>
		/// <param name="sortOrder">The sort order for the items.</param>
		/// <param name="filter">A filter function to apply on items.</param>
		/// <param name="getName">A function to retrieve the display name from a value.</param>
		/// <param name="getDescription">A function to retrieve the description from a value.</param>
		public ItemsSourceBase(bool excludeObsolete, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null, Func<T, string> getName = null, Func<T, string> getDescription = null)
			: this(null, excludeObsolete, sortOrder, filter, getName, getDescription) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceBase{T}"/> class using a collection of values.
		/// </summary>
		/// <param name="values">The collection of values or items.</param>
		/// <param name="getName">A function to retrieve the display name from a value.</param>
		/// <param name="getDescription">A function to retrieve the description from a value.</param>
		public ItemsSourceBase(IEnumerable values, Func<T, string> getName = null, Func<T, string> getDescription = null)
			: this(values, true, null, null, getName, getDescription) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceBase{T}"/> class.
		/// </summary>
		public ItemsSourceBase() : this(null) { }

		/// <summary>
		/// Gets the format string for displaying the value. Override to provide a custom format.
		/// </summary>
		protected virtual string Format => null;

		/// <summary>
		/// Retrieves the display name for the specified value.
		/// </summary>
		/// <param name="value">The value whose display name is to be retrieved.</param>
		/// <returns>The display name.</returns>
		protected virtual string GetName(T value)
		{
			if (_getName != null)
				return _getName(value);

			var f = Format;
			return f.IsEmptyOrWhiteSpace() ? value.GetDisplayName() : string.Format($"{{0:{f}}}", value);
		}

		/// <summary>
		/// Retrieves the description for the specified value.
		/// </summary>
		/// <param name="value">The value whose description is to be retrieved.</param>
		/// <returns>The description.</returns>
		protected virtual string GetDescription(T value) => _getDescription is null ? (typeof(T).IsEnum ? value.GetFieldDescription() : null) : _getDescription(value);

		/// <summary>
		/// Retrieves the icon for the specified value.
		/// </summary>
		/// <param name="value">The value whose icon is to be retrieved.</param>
		/// <returns>The icon URI.</returns>
		protected virtual Uri GetIcon(T value) => typeof(T).IsEnum ? value.GetFieldIcon() : null;

		/// <summary>
		/// Determines whether the specified value is marked as obsolete.
		/// </summary>
		/// <param name="value">The value to check.</param>
		/// <returns><c>true</c> if obsolete; otherwise, <c>false</c>.</returns>
		protected virtual bool GetIsObsolete(T value) => typeof(T).IsEnum && value.GetAttributeOfType<ObsoleteAttribute>() != null;

		/// <summary>
		/// Applies filtering to the given item.
		/// </summary>
		/// <param name="item">The item to filter.</param>
		/// <returns><c>true</c> if the item should be included; otherwise, <c>false</c>.</returns>
		protected virtual bool Filter(IItemsSourceItem<T> item)
			=> (!ExcludeObsolete || !item.IsObsolete) && _filter?.Invoke(item) != false;

		IItemsSourceItem IItemsSource.CreateNewItem(object value)
		{
			if (value is not T typedVal)
				throw new ArgumentException(nameof(value));

			return CreateNewItem(typedVal);
		}

		private IItemsSourceItem<T> CreateNewItem(IItemsSourceItem fromItem)
		{
			return new ItemsSourceItem<T>(
				(T)fromItem.Value,
				() => fromItem.DisplayName,
				() => fromItem.Description,
				fromItem.Icon,
				fromItem.IsObsolete
			);
		}

		/// <summary>
		/// Creates a new item with the specified value.
		/// </summary>
		/// <param name="value">The value for the new item.</param>
		/// <returns>The newly created item.</returns>
		public virtual IItemsSourceItem<T> CreateNewItem(T value)
		{
			return new ItemsSourceItem<T>(
				value,
				() => GetName(value),
				() => GetDescription(value),
				GetIcon(value),
				GetIsObsolete(value)
			);
		}

		/// <summary>
		/// Retrieves the underlying collection of values.
		/// </summary>
		/// <returns>The collection of values.</returns>
		protected virtual IEnumerable<T> GetValues() => _values;

		private IEnumerable<IItemsSourceItem<T>> FilterItems(IEnumerable<IItemsSourceItem<T>> items)
		{
			items ??= [];

			items = items.Where(Filter);

			if (SortOrder != null)
				items = SortOrder == ListSortDirection.Ascending ?
					items.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase) :
					items.OrderByDescending(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase);

			return items.ToArray();
		}

		private IEnumerable<IItemsSourceItem<T>> CreateItems(IEnumerable<T> values) => FilterItems(values?.Select(CreateNewItem));
	}

	/// <summary>
	/// Represents a non-generic items source based on objects.
	/// </summary>
	public class ItemsSourceBase : ItemsSourceBase<object>
	{
		private static IItemsSource Create(IEnumerable values, Type itemValueType, bool? excludeObsolete, ListSortDirection? sortOrder, Func<IItemsSourceItem, bool> filter, Func<object, string> getName, Func<object, string> getDescription)
		{
			itemValueType ??= GetSourceValueType(values);

			var srcType = typeof(ItemsSourceBase<>).Make(itemValueType);

			excludeObsolete ??= true;

			return (IItemsSource)Activator.CreateInstance(
					srcType,
					BindingFlags.Instance | BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					[values, excludeObsolete.Value, sortOrder, filter, getName, getDescription],
					null,
					null);
		}

		/// <summary>
		/// Creates an <see cref="IItemsSource"/> from an object.
		/// </summary>
		/// <param name="val">The source value.</param>
		/// <param name="itemValueType">The expected type of the items.</param>
		/// <param name="excludeObsolete">A value indicating whether obsolete items should be excluded.</param>
		/// <param name="sortOrder">The sort order for the items.</param>
		/// <param name="filter">A filter function to apply on items.</param>
		/// <param name="getName">A function to retrieve the display name from a value.</param>
		/// <param name="getDescription">A function to retrieve the description from a value.</param>
		/// <returns>An instance of <see cref="IItemsSource"/>.</returns>
		public static IItemsSource Create(object val, Type itemValueType, bool? excludeObsolete = null, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null, Func<object, string> getName = null, Func<object, string> getDescription = null)
		{
			switch (val)
			{
				case null:
					itemValueType ??= typeof(object);
					return Create(itemValueType.CreateArray(0), itemValueType, excludeObsolete, sortOrder, filter, getName, getDescription);

				case IItemsSource src:
					if ((itemValueType is null || src.ValueType == itemValueType) && (excludeObsolete is null || excludeObsolete == src.ExcludeObsolete) && (sortOrder is null || sortOrder == src.SortOrder) && filter is null)
						return src;

					return Create(src.Values, itemValueType, excludeObsolete, sortOrder, filter, getName, getDescription);

				case IEnumerable ie:
					return Create(ie, itemValueType, excludeObsolete, sortOrder, filter, getName, getDescription);

				default:
					throw new ArgumentException($"cannot create {typeof(IItemsSource).FullName} from '{val.GetType().FullName}'");
			}
		}

		private static Type GetSourceValueType(IEnumerable values)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			var itemType = GetParamType(values.GetType(), typeof(IEnumerable<>));
			var innerType = GetParamType(itemType, typeof(IItemsSourceItem<>));

			if (innerType != null && innerType != typeof(object))
				return innerType;

			if (itemType != null && !itemType.Is<IItemsSourceItem>() && itemType != typeof(object))
				return itemType;

			bool foundItems, foundValues;
			foundItems = foundValues = false;

			var types = values.Cast<object>().Select(o =>
			{
				var t = o.GetType();
				var innerItemType = GetParamType(t, typeof(IItemsSourceItem<>));

				if (innerItemType != null)
				{
					foundItems = true;
					return innerItemType;
				}

				if (o is IItemsSourceItem iisi)
				{
					foundItems = true;
					return iisi.Value.GetType();
				}

				foundValues = true;
				return t;
			}).ToArray();

			if (foundItems && foundValues)
				throw new ArgumentException($"{nameof(values)} contains elements of incompatible types");

			return GetCommonType(types);
		}

		private static Type GetParamType(Type type, Type genericInterfaceType)
		{
			if (type is null)
				return null;

			return new[] { type }
				.Concat(type.GetInterfaces())
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)
				.Select(i => i.GetGenericArguments()[0])
			    .FirstOrDefault();
		}

		private static Type GetCommonType(Type[] types)
		{
			if (types is null)
				throw new ArgumentNullException(nameof(types));

			if (types.Length == 0)
				return typeof(object);

			var type = types[0];

			for (var i = 1; i < types.Length; ++i)
			{
				if (type.Is(types[i]))
					type = types[i];
				else
					while (!types[i].Is(type))
						type = type.BaseType;
			}

			return type;
		}
	}

	/// <summary>
	/// Specifies the items source type to be used for a property.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ItemsSourceAttribute : Attribute
	{
		/// <summary>
		/// Gets the type to use for the items source.
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the user is allowed to edit the items.
		/// </summary>
		public bool IsEditable { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceAttribute"/> class with the specified source type.
		/// </summary>
		/// <param name="type">The type to use as the items source.</param>
		public ItemsSourceAttribute(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			Type =
				type.Is<IItemsSource>()
					? type
					: type.IsEnum
						? typeof(ItemsSourceBase<>).Make(type)
						: throw new ArgumentException($"Type '{type}' must implement the '{typeof(IItemsSource)}' interface or be an enum.", nameof(type));
		}
	}
}