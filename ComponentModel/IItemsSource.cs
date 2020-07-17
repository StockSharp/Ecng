namespace Ecng.ComponentModel
{
	using System;
	using System.Linq;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Collections;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Localization;

	using MoreLinq;

	public interface IItemsSourceItem
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

	public class ItemsSourceItem<T> : IItemsSourceItem<T>
	{
		object IItemsSourceItem.Value => Value;

		public T Value { get; }

		public string DisplayName { get; }
		public string Description { get; }
		public Uri Icon { get; }
		public bool IsObsolete { get; }

		public ItemsSourceItem(T value, string displayName, string description, Uri iconUri, bool isObsolete)
		{
			Value         = value;
			DisplayName   = displayName;
			Description   = description;
			Icon          = iconUri;
			IsObsolete    = isObsolete;
		}

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
		readonly T[] _values;
		readonly Lazy<IEnumerable<IItemsSourceItem<T>>> _items;
		readonly Func<IItemsSourceItem, bool> _filter;

		public bool ExcludeObsolete { get; }
		public ListSortDirection? SortOrder { get; }

		IEnumerable<IItemsSourceItem> IItemsSource.Values => Values;

		public IEnumerable<IItemsSourceItem<T>> Values => _items.Value;

		public virtual Type ValueType => typeof(T);

		protected ItemsSourceBase(IEnumerable<T> values, bool excludeObsolete, ListSortDirection? sortOrder, Func<IItemsSourceItem, bool> filter)
		{
			SortOrder = sortOrder;
			ExcludeObsolete = excludeObsolete;
			_filter = filter;
			_values = values?.ToArray() ?? (typeof(T).IsEnum ? Enumerator.GetValues<T>().ToArray() : null);
			_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => CreateItems(GetValues()));
		}

		protected ItemsSourceBase(bool excludeObsolete, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null)
			: this((IEnumerable<T>)null, excludeObsolete, sortOrder, filter) { }

		protected ItemsSourceBase(IEnumerable<T> values)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(values, true, null, null) { }

		public ItemsSourceBase()
			: this((IEnumerable<T>)null) { }

		protected ItemsSourceBase(IEnumerable<IItemsSourceItem<T>> items, bool excludeObsolete, ListSortDirection? sortOrder, Func<IItemsSourceItem, bool> filter)
			: this(excludeObsolete, sortOrder, filter)
		{
			var itemsArr = items?.ToArray() ?? throw new ArgumentNullException(nameof(items));

			_values = itemsArr.Select(item => item.Value).ToArray();
			_items = new Lazy<IEnumerable<IItemsSourceItem<T>>>(() => FilterItems(itemsArr));
		}

		protected ItemsSourceBase(IEnumerable<IItemsSourceItem<T>> items)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(items, true, null, null) { }

		protected virtual string Format => null;

		protected virtual string GetName(T value)
		{
			var f = Format;
			return f.IsEmptyOrWhiteSpace() ? value.GetDisplayName() : string.Format($"{{0:{f}}}", value);
		}

		protected virtual string GetDescription(T value) => typeof(T).IsEnum ? value.GetFieldDescription() : null;

		protected virtual Uri GetIcon(T value) => typeof(T).IsEnum ? value.GetFieldIcon() : null;

		protected virtual bool GetIsObsolete(T value) => typeof(T).IsEnum && value.GetAttributeOfType<ObsoleteAttribute>() != null;

		protected virtual bool Filter(IItemsSourceItem<T> item)
			=> (!ExcludeObsolete || !item.IsObsolete) && _filter?.Invoke(item) != false;

		IItemsSourceItem IItemsSource.CreateNewItem(object value)
		{
			if(!(value is T typedVal))
				throw new ArgumentException(nameof(value));

			return CreateNewItem(typedVal);
		}

		public virtual IItemsSourceItem<T> CreateNewItem(T value)
		{
			return new ItemsSourceItem<T>(
				value,
				GetName(value),
				GetDescription(value),
				GetIcon(value),
				GetIsObsolete(value)
			);
		}

		protected virtual IEnumerable<T> GetValues() => _values;

		IEnumerable<IItemsSourceItem<T>> FilterItems(IEnumerable<IItemsSourceItem<T>> items)
		{
			items ??= new IItemsSourceItem<T>[0];

			items = items.Where(Filter);

			if(SortOrder != null)
				items = SortOrder == ListSortDirection.Ascending ?
					items.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase) :
					items.OrderByDescending(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase);

			return items.ToArray();
		}

		IEnumerable<IItemsSourceItem<T>> CreateItems(IEnumerable<T> values) => FilterItems(values?.Select(CreateNewItem));
	}

	public class ItemsSourceBase : ItemsSourceBase<object>
	{
		static Type GetParamType(Type type, Type genericInterfaceType)
		{
			if(type == null) return null;

			return type.GetInterfaces().Concat(type)
				 .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)
				 .Select(i => i.GetGenericArguments()[0])
				 .FirstOrDefault();
		}

		static readonly IItemsSource _emptySource = Create(new object[0]);

		static IItemsSource Create(IEnumerable values, bool? excludeObsolete, ListSortDirection? sortOrder, Func<IItemsSourceItem, bool> filter)
		{
			var itemType = GetParamType(values.GetType(), typeof(IEnumerable<>));
			var innerType = GetParamType(itemType, typeof(IItemsSourceItem<>));

			itemType ??= typeof(object);

			if(innerType != null)
				itemType = innerType;
			else if (typeof(IItemsSourceItem).IsAssignableFrom(itemType))
			{
				var arr = values.Cast<IItemsSourceItem>().ToArray();
				var types = arr.Select(i => i.GetType()).ToArray();
				var paramTypes = types.Select(t => GetParamType(t, typeof(IItemsSourceItem<>))).ToArray();

				if(paramTypes.Any(t => t == null))
					throw new InvalidOperationException("cant determine common item value type for: " + types.Select(t => t.Name).Join(","));

				itemType = GetCommonType(paramTypes);

				// ReSharper disable once PossibleNullReferenceException
				values = (IEnumerable) typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(typeof(IItemsSourceItem<>).Make(itemType)).Invoke(null, new object[] { arr });
			}

			var srcType = typeof(ItemsSourceBase<>).Make(itemType);

			excludeObsolete ??= true;

			return (IItemsSource) Activator.CreateInstance(
					srcType,
					BindingFlags.Instance | BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					new object[] { values, excludeObsolete, sortOrder, filter },
					null,
					null);
		}

		public static IItemsSource Create(object val, bool? excludeObsolete = null, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null)
		{
			switch (val)
			{
				case null:
					return _emptySource;

				case IItemsSource src:
					if((excludeObsolete == null || excludeObsolete == src.ExcludeObsolete) && (sortOrder == null || sortOrder == src.SortOrder) && filter == null)
						return src;

					return Create(src.Values, excludeObsolete, sortOrder, filter);

				case IEnumerable ie:
					return Create(ie, excludeObsolete, sortOrder, filter);

				default:
					throw new ArgumentException($"cannot create {typeof(IItemsSource).FullName} from '{val.GetType().FullName}'");
			}
		}

		public static Type GetCommonType(Type[] types)
		{
			if (types.Length == 0)
				return typeof(object);

			var type = types[0];

			for (var i = 1; i < types.Length; ++i)
			{
				if (types[i].IsAssignableFrom(type))
					type = types[i];
				else
					while (!type.IsAssignableFrom(types[i]))
						type = type.BaseType;
			}

			return type;
		}
	}

	[Obsolete("Use ItemsSourceBase directly")]
	public class EnumSource<T> : ItemsSourceBase<T>
		where T : Enum
	{
		public EnumSource() : base(true) { }

		public EnumSource(bool excludeObsolete) : base(excludeObsolete) { }

		public EnumSource(IEnumerable<T> values, bool excludeObsolete) : base(values, excludeObsolete, null, null) { }

		// ReSharper disable once IntroduceOptionalParameters.Global
		public EnumSource(IEnumerable<T> values) : this(values, true) { }

		public EnumSource(IEnumerable<IItemsSourceItem<T>> items) : base(items) { }
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
						? typeof(ItemsSourceBase<>).Make(type)
						: throw new ArgumentException("Type '{0}' must implement the '{1}' interface or be an enum.".Translate().Put(type, typeof(IItemsSource)), nameof(type));
		}
	}
}