namespace Ecng.Net.Sitemap;

/// <summary>
/// Represents an XHTML link element for sitemap alternate language pages (hreflang).
/// Used to specify alternate language versions of the same content for multilingual websites.
/// </summary>
public sealed class XhtmlLink
{
	/// <summary>
	/// Initializes a new instance of the <see cref="XhtmlLink"/> class.
	/// </summary>
	/// <param name="href">The URL of the alternate language version.</param>
	/// <param name="hreflang">The language code for this alternate version (e.g., "en", "fr", "en-US", "x-default").</param>
	/// <exception cref="ArgumentNullException">Thrown when href or hreflang is null or empty.</exception>
	public XhtmlLink(string href, string hreflang)
	{
		if (href.IsEmpty())
			throw new ArgumentNullException(nameof(href));
		
		if (hreflang.IsEmpty())
			throw new ArgumentNullException(nameof(hreflang));

		Href = href;
		Hreflang = hreflang;
	}

	/// <summary>
	/// Gets the URL of the alternate language version.
	/// This URL must be absolute and properly formatted.
	/// </summary>
	/// <value>The alternate page URL.</value>
	public string Href { get; }

	/// <summary>
	/// Gets the language code for this alternate version.
	/// Valid values include ISO 639-1 language codes (e.g., "en", "fr"),
	/// regional language codes (e.g., "en-US", "fr-CA"),
	/// and the special value "x-default" for the default language version.
	/// </summary>
	/// <value>The language code.</value>
	public string Hreflang { get; }

	/// <summary>
	/// Gets the relationship type for this link.
	/// Always returns "alternate" for XHTML sitemap links.
	/// </summary>
	/// <value>The relationship type.</value>
	public string Rel => "alternate";

	/// <summary>
	/// Returns a string representation of this XHTML link.
	/// </summary>
	/// <returns>A string in the format "hreflang: href".</returns>
	public override string ToString()
	{
		return $"{Hreflang}: {Href}";
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// Two XhtmlLink objects are considered equal if they have the same Hreflang value.
	/// </summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		if (obj is XhtmlLink other)
		{
			return string.Equals(Hreflang, other.Hreflang, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <returns>A hash code for this instance.</returns>
	public override int GetHashCode()
	{
		return Hreflang?.ToLowerInvariant().GetHashCode() ?? 0;
	}
}