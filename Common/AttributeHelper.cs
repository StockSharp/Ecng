namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public static class AttributeHelper
	{
		private static readonly Dictionary<Tuple<Type, ICustomAttributeProvider>, Attribute> _attrCache = new Dictionary<Tuple<Type, ICustomAttributeProvider>, Attribute>();

		public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider, bool inherit = true)
			where TAttribute : Attribute
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return (TAttribute)_attrCache.SafeAdd(new Tuple<Type, ICustomAttributeProvider>(typeof(TAttribute), provider),
				key => provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().FirstOrDefault());
		}

		public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this ICustomAttributeProvider provider, bool inherit = true)
			where TAttribute : Attribute
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
		}

		public static IEnumerable<Attribute> GetAttributes(this ICustomAttributeProvider provider, bool inherit = true)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetCustomAttributes(inherit).Cast<Attribute>();
		}

		private static V SafeAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> handler)
		{
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));


			if (!dictionary.TryGetValue(key, out V value))
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
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.GetAttribute<ObsoleteAttribute>() != null;
		}
	}
}