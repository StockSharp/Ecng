namespace Ecng.ComponentModel
{
	using System;
	using System.Linq;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Collections;
	using System.Reflection;

	using Ecng.Common;

	public interface IItemsSourceItem : INotifyPropertyChangedEx
	{
		string DisplayName {get;}
		string Description {get;}
		Uri Icon { get; }
		bool IsObsolete { get; }

		object Value {get;}
	}

	public interface IItemsSourceItem<out TValue> : IItemsSourceItem
	{
		new TValue Value {get;}
	}

	public class ItemsSourceItem<T>(T value, Func<string> getDisplayName, Func<string> getDescription, Uri iconUri, bool isObsolete) : NotifiableObject, IItemsSourceItem<T>
	{
		private readonly Func<string> _getDisplayName = getDisplayName ?? throw new ArgumentNullException(nameof(getDisplayName));
		private readonly Func<string> _getDescription = getDescription ?? throw new ArgumentNullException(nameof(getDescription));

		object IItemsSourceItem.Value => Value;

		public T Value { get; } = value;

		public string DisplayName => _getDisplayName();
		public string Description => _getDescription();
		public Uri Icon { get; } = iconUri;
		public bool IsObsolete { get; } = isObsolete;

		public override string ToString() => DisplayName;
	}

	public interface IItemsSource
	{
		IEnumerable<IItemsSourceItem> Values { get; }

		Type ValueType { get; }
		bool ExcludeObsolete { get; }
		ListSortDirection? SortOrder { get; }

		IItemsSourceItem CreateNewItem(object value);
	}

	public interface IItemsSource<TValue> : IItemsSource
	{
		/// <summary>Collection of values represented by a ComboBox.</summary>
		new IEnumerable<IItemsSourceItem<TValue>> Values { get; }

		IItemsSourceItem<TValue> CreateNewItem(TValue value);
	}

	public class ItemsSourceBase<T> : IItemsSource<T>
	{
		private readonly T[] _values;
		private readonly Lazy<IEnumerable<IItemsSourceItem<T>>> _items;
		private readonly Func<IItemsSourceItem, bool> _filter;

		private readonly Func<T, string> _getName;
		private readonly Func<T, string> _getDescription;

		public bool ExcludeObsolete { get; }
		public ListSortDirection? SortOrder { get; }

		IEnumerable<IItemsSourceItem> IItemsSource.Values => Values;

		public IEnumerable<IItemsSourceItem<T>> Values => _items.Value;

		public virtual Type ValueType => typeof(T);

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
				if(objects.All(o => o is T))
				{
					_values = objects.Cast<T>().ToArray();
					_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => CreateItems(GetValues()));
				}
				else if (objects.All(o => o is IItemsSourceItem<T>))
				{
					var itemsArr = objects.Cast<IItemsSourceItem<T>>().ToArray();
					_values = itemsArr.Select(item => item.Value).ToArray();
					_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => FilterItems(itemsArr));
				}
				else if (objects.All(o => o is IItemsSourceItem iisi && iisi.Value is T))
				{
					var itemsArr = objects.Cast<IItemsSourceItem>().Select(CreateNewItem).ToArray();
					_values = itemsArr.Select(item => item.Value).ToArray();
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
					_values = Enumerator.GetValues<T>().ToArray();

				_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => CreateItems(GetValues()));
			}
		}

		public ItemsSourceBase(bool excludeObsolete, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null, Func<T, string> getName = null, Func<T, string> getDescription = null)
			: this(null, excludeObsolete, sortOrder, filter, getName, getDescription) { }

		public ItemsSourceBase(IEnumerable values, Func<T, string> getName = null, Func<T, string> getDescription = null)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(values, true, null, null, getName, getDescription) { }

		// this constructor left public to make .CreateInstance() extension work
		public ItemsSourceBase() : this(null) { }

		protected virtual string Format => null;

		protected virtual string GetName(T value)
		{
			if (_getName != null)
				return _getName(value);

			var f = Format;
			return f.IsEmptyOrWhiteSpace() ? value.GetDisplayName() : string.Format($"{{0:{f}}}", value);
		}

		protected virtual string GetDescription(T value) => _getDescription is null ? (typeof(T).IsEnum ? value.GetFieldDescription() : null) : _getDescription(value);

		protected virtual Uri GetIcon(T value) => typeof(T).IsEnum ? value.GetFieldIcon() : null;

		protected virtual bool GetIsObsolete(T value) => typeof(T).IsEnum && value.GetAttributeOfType<ObsoleteAttribute>() != null;

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

		protected virtual IEnumerable<T> GetValues() => _values;

		IEnumerable<IItemsSourceItem<T>> FilterItems(IEnumerable<IItemsSourceItem<T>> items)
		{
			items ??= [];

			items = items.Where(Filter);

			if (SortOrder != null)
				items = SortOrder == ListSortDirection.Ascending ?
					items.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase) :
					items.OrderByDescending(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase);

			return items.ToArray();
		}

		IEnumerable<IItemsSourceItem<T>> CreateItems(IEnumerable<T> values) => FilterItems(values?.Select(CreateNewItem));
	}

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
					// ReSharper disable once PossibleNullReferenceException
					while (!types[i].Is(type))
						type = type.BaseType;
			}

			return type;
		}
	}

	/// <summary>
	/// Represents an attribute that is set on a property to identify the IItemsSource-derived class that will be used.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
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