namespace Ecng.Net.Sitemap;

using System.Collections;

/// <summary>
/// A collection of XHTML links that prevents duplicate hreflang values.
/// Used to manage alternate language versions for a sitemap URL.
/// </summary>
public sealed class XhtmlLinkCollection : ICollection<XhtmlLink>
{
	private readonly Dictionary<string, XhtmlLink> _links = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Gets the number of XHTML links in the collection.
	/// </summary>
	public int Count => _links.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Adds an XHTML link to the collection.
	/// If a link with the same hreflang already exists, it will be replaced.
	/// </summary>
	/// <param name="item">The XHTML link to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
	public void Add(XhtmlLink item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		_links[item.Hreflang] = item;
	}

	/// <summary>
	/// Removes all XHTML links from the collection.
	/// </summary>
	public void Clear()
	{
		_links.Clear();
	}

	/// <summary>
	/// Determines whether the collection contains a specific XHTML link.
	/// </summary>
	/// <param name="item">The XHTML link to locate.</param>
	/// <returns>true if the link is found; otherwise, false.</returns>
	public bool Contains(XhtmlLink item)
	{
		if (item == null)
			return false;

		return _links.TryGetValue(item.Hreflang, out var existing) && 
		       string.Equals(existing.Href, item.Href, StringComparison.Ordinal);
	}

	/// <summary>
	/// Determines whether the collection contains a link with the specified hreflang.
	/// </summary>
	/// <param name="hreflang">The language code to search for.</param>
	/// <returns>true if a link with the specified hreflang is found; otherwise, false.</returns>
	public bool ContainsHreflang(string hreflang)
	{
		return _links.ContainsKey(hreflang);
	}

	/// <summary>
	/// Copies the XHTML links to an Array, starting at a particular Array index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
	public void CopyTo(XhtmlLink[] array, int arrayIndex)
	{
		_links.Values.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<XhtmlLink> GetEnumerator()
	{
		return _links.Values.GetEnumerator();
	}

	/// <summary>
	/// Removes the first occurrence of a specific XHTML link from the collection.
	/// </summary>
	/// <param name="item">The XHTML link to remove.</param>
	/// <returns>true if the link was successfully removed; otherwise, false.</returns>
	public bool Remove(XhtmlLink item)
	{
		if (item == null)
			return false;

		if (_links.TryGetValue(item.Hreflang, out var existing) && 
		    string.Equals(existing.Href, item.Href, StringComparison.Ordinal))
		{
			return _links.Remove(item.Hreflang);
		}

		return false;
	}

	/// <summary>
	/// Removes the XHTML link with the specified hreflang from the collection.
	/// </summary>
	/// <param name="hreflang">The language code of the link to remove.</param>
	/// <returns>true if the link was successfully removed; otherwise, false.</returns>
	public bool RemoveHreflang(string hreflang)
	{
		return _links.Remove(hreflang);
	}

	/// <summary>
	/// Gets the XHTML link with the specified hreflang.
	/// </summary>
	/// <param name="hreflang">The language code to search for.</param>
	/// <param name="link">When this method returns, contains the XHTML link with the specified hreflang, if found; otherwise, null.</param>
	/// <returns>true if a link with the specified hreflang is found; otherwise, false.</returns>
	public bool TryGetLink(string hreflang, out XhtmlLink link)
	{
		return _links.TryGetValue(hreflang, out link);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}