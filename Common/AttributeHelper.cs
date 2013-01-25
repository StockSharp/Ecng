namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public static class AttributeHelper
	{
		private static readonly Dictionary<Tuple<Type, ICustomAttributeProvider>, Attribute> _attrCache = new Dictionary<Tuple<Type, ICustomAttributeProvider>, Attribute>();

		#region GetAttribute

		public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider)
			where TAttribute : Attribute
		{
			return provider.GetAttribute<TAttribute>(true);
		}

		public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider, bool inherit)
			where TAttribute : Attribute
		{
			return (TAttribute)_attrCache.SafeAdd(new Tuple<Type, ICustomAttributeProvider>(typeof(TAttribute), provider),
				key => provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().FirstOrDefault());
		}

		#endregion

		#region GetAttributes

		public static TAttribute[] GetAttributes<TAttribute>(this ICustomAttributeProvider provider)
			where TAttribute : Attribute
		{
			return provider.GetAttributes<TAttribute>(true);
		}

		public static TAttribute[] GetAttributes<TAttribute>(this ICustomAttributeProvider provider, bool inherit)
			where TAttribute : Attribute
		{
			return (TAttribute[])provider.GetCustomAttributes(typeof(TAttribute), inherit);
		}

		#endregion

		private static V SafeAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> handler)
		{
			if (dictionary == null)
				throw new ArgumentNullException("dictionary");

			if (handler == null)
				throw new ArgumentNullException("handler");

			V value;

			if (!dictionary.TryGetValue(key, out value))
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
	}
}