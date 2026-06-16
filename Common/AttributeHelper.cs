namespace Ecng.Common;

using System.Collections.Concurrent;

/// <summary>
/// Provides helper methods for working with custom attributes including caching support.
/// </summary>
public static class AttributeHelper
{
	// Read on a hot path from many threads (GetAttribute is called widely, incl. from Converter)
	// while ProxyTypes-style registration / ClearCache mutate it; a plain Dictionary is not safe
	// for concurrent read-during-write, so use a ConcurrentDictionary.
	private static readonly ConcurrentDictionary<(Type, ICustomAttributeProvider, bool), Attribute> _attrCache = [];

	/// <summary>
	/// Gets or sets a value indicating whether attribute caching is enabled.
	/// </summary>
	public static bool CacheEnabled { get; set; } = true;

	/// <summary>
	/// Clears the internal attribute cache.
	/// </summary>
	public static void ClearCache() => _attrCache.Clear();

	/// <summary>
	/// Retrieves the first custom attribute of the specified type, optionally searching the ancestors.
	/// If caching is enabled, the attribute is stored and reused.
	/// </summary>
	/// <typeparam name="TAttribute">The type of the custom attribute to retrieve.</typeparam>
	/// <param name="provider">The attribute provider to search.</param>
	/// <param name="inherit">true to inspect the ancestors of the provider; otherwise, false.</param>
	/// <returns>
	/// The first attribute of type <typeparamref name="TAttribute"/> found on the provider; otherwise, null.
	/// </returns>
	public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider provider, bool inherit = true)
		where TAttribute : Attribute
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		TAttribute GetAttribute()
			=> provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().FirstOrDefault();

		if (!CacheEnabled)
			return GetAttribute();

		return (TAttribute)_attrCache.GetOrAdd(new(typeof(TAttribute), provider, inherit),
			_ => GetAttribute());
	}

	/// <summary>
	/// Retrieves all custom attributes of the specified type from the provider, optionally searching the ancestors.
	/// </summary>
	/// <typeparam name="TAttribute">The type of the custom attributes to retrieve.</typeparam>
	/// <param name="provider">The attribute provider to search.</param>
	/// <param name="inherit">true to inspect the ancestors of the provider; otherwise, false.</param>
	/// <returns>An enumerable collection of attributes of type <typeparamref name="TAttribute"/>.</returns>
	public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this ICustomAttributeProvider provider, bool inherit = true)
		where TAttribute : Attribute
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return provider.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>();
	}

	/// <summary>
	/// Retrieves all custom attributes from the provider, optionally searching the ancestors.
	/// </summary>
	/// <param name="provider">The attribute provider to search.</param>
	/// <param name="inherit">true to inspect the ancestors of the provider; otherwise, false.</param>
	/// <returns>An enumerable collection of custom attributes.</returns>
	public static IEnumerable<Attribute> GetAttributes(this ICustomAttributeProvider provider, bool inherit = true)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return provider.GetCustomAttributes(inherit).Cast<Attribute>();
	}

	/// <summary>
	/// Determines whether the provider is marked with the <see cref="ObsoleteAttribute"/>.
	/// </summary>
	/// <param name="provider">The attribute provider to check.</param>
	/// <returns>true if the provider has the <see cref="ObsoleteAttribute"/>; otherwise, false.</returns>
	public static bool IsObsolete(this ICustomAttributeProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return provider.GetAttribute<ObsoleteAttribute>() != null;
	}

	/// <summary>
	/// Determines whether the provider is marked as browsable via the <see cref="BrowsableAttribute"/>.
	/// </summary>
	/// <param name="provider">The attribute provider to check.</param>
	/// <returns>
	/// true if the <see cref="BrowsableAttribute"/> is not present or its <see cref="BrowsableAttribute.Browsable"/> property is true; otherwise, false.
	/// </returns>
	public static bool IsBrowsable(this ICustomAttributeProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return provider.GetAttribute<BrowsableAttribute>()?.Browsable != false;
	}
}