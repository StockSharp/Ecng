namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;

	public static class AttributeHelper
	{
		private static readonly Dictionary<Tuple<Type, ICustomAttributeProvider>, Attribute> _attrCache = new();

		public static bool CacheEnabled { get; set; } = true;

		public static void ClearCache() => _attrCache.Clear();

		public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider, bool inherit = true)
			where TAttribute : Attribute
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			TAttribute GetAttribute()
				=> provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().FirstOrDefault();

			if (!CacheEnabled)
				return GetAttribute();

			return (TAttribute)_attrCache.SafeAdd(new Tuple<Type, ICustomAttributeProvider>(typeof(TAttribute), provider),
				key => GetAttribute());
		}

		public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this ICustomAttributeProvider provider, bool inherit = true)
			where TAttribute : Attribute
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
		}

		public static IEnumerable<Attribute> GetAttributes(this ICustomAttributeProvider provider, bool inherit = true)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetCustomAttributes(inherit).Cast<Attribute>();
		}

		private static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler)
		{
			if (dictionary is null)
				throw new ArgumentNullException(nameof(dictionary));

			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			if (!dictionary.TryGetValue(key, out var value))
			{
				lock (dictionary)
				{
					if (!dictionary.TryGetValue(key, out value))
					{
						value = handler(key);
						dictionary.Add(key, value);
					}
				}
			}

			return value;
		}

		public static bool IsObsolete(this ICustomAttributeProvider provider)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetAttribute<ObsoleteAttribute>() != null;
		}

		public static bool IsBrowsable(this ICustomAttributeProvider provider)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetAttribute<BrowsableAttribute>()?.Browsable != false;
		}
	}
}