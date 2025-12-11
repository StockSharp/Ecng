namespace Ecng.Net.Sitemap;

using System.Globalization;
using System.Xml.Linq;

/// <summary>
/// Generates sitemap XML.
/// </summary>
public static class SitemapGenerator
{
	private const string _sitemapsNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
	private const string _xhtmlNamespace = "http://www.w3.org/1999/xhtml";

	/// <summary>
	/// The maximum number of sitemaps a sitemap index file can contain.
	/// </summary>
	public const int MaximumSitemapCount = 50000;

	/// <summary>
	/// The maximum size of a sitemap file in bytes (10MB).
	/// </summary>
	public const int MaximumSitemapSizeInBytes = 10485760;

	private const string _timeFormat = "yyyy-MM-ddTHH:mm:sszzz";

	/// <summary>
	/// Gets the sitemap index XML document, containing links to all the sitemap XML documents.
	/// </summary>
	/// <param name="sitemaps">The collection of sitemaps containing their index and nodes.</param>
	/// <returns>The sitemap index XML document, containing links to all the sitemap XML documents.</returns>
	public static XDocument GenerateSitemapIndex(IEnumerable<string> sitemaps)
	{
		XNamespace xmlns = _sitemapsNamespace;
		var root = new XElement(xmlns + "sitemapindex");

		foreach (var sitemap in sitemaps)
		{
			var sitemapElement = new XElement(
				xmlns + "sitemap",
				new XElement(xmlns + "loc", sitemap),
					new XElement(xmlns + "lastmod",
						DateTime.UtcNow.ToString(_timeFormat)));

			root.Add(sitemapElement);
		}

		var document = new XDocument(root);
		return document;
	}

	/// <summary>
	/// Gets the sitemap XML document for the specified set of nodes.
	/// </summary>
	/// <param name="sitemapNodes">The sitemap nodes.</param>
	/// <returns>The sitemap XML document for the specified set of nodes.</returns>
	public static XDocument GenerateSitemap(IEnumerable<SitemapNode> sitemapNodes)
	{
		var nodes = sitemapNodes.ToList();
		var hasXhtmlLinks = nodes.Any(n => n.AlternateLinks.Count > 0);

		XNamespace xmlns = _sitemapsNamespace;
		XNamespace xhtmlNs = _xhtmlNamespace;

		var root = new XElement(xmlns + "urlset");
		
		// Add XHTML namespace if any nodes have alternate links
		if (hasXhtmlLinks)
		{
			root.Add(new XAttribute(XNamespace.Xmlns + "xhtml", _xhtmlNamespace));
		}

		var count = 0;

		foreach (var sitemapNode in nodes)
		{
			var urlElement = new XElement(xmlns + "url");

			// Add basic sitemap elements
			urlElement.Add(new XElement(xmlns + "loc", sitemapNode.Url.UrlEscape()));

			// Add XHTML alternate links
			if (sitemapNode.AlternateLinks.Count > 0)
			{
				foreach (var alternateLink in sitemapNode.AlternateLinks)
				{
					var xhtmlLink = new XElement(xhtmlNs + "link");
					xhtmlLink.SetAttributeValue("rel", alternateLink.Rel);
					xhtmlLink.SetAttributeValue("hreflang", alternateLink.Hreflang);
					xhtmlLink.SetAttributeValue("href", alternateLink.Href.UrlEscape());
					urlElement.Add(xhtmlLink);
				}
			}

			// Add optional sitemap elements
			if (sitemapNode.LastModified is not null)
			{
				urlElement.Add(new XElement(xmlns + "lastmod",
					sitemapNode.LastModified.Value.ToUniversalTime().ToString(_timeFormat)));
			}

			if (sitemapNode.Frequency is not null)
			{
				urlElement.Add(new XElement(xmlns + "changefreq",
					sitemapNode.Frequency.Value.ToString().ToLowerInvariant()));
			}

			if (sitemapNode.Priority is not null)
			{
				urlElement.Add(new XElement(xmlns + "priority",
					sitemapNode.Priority.Value.ToString("F1", CultureInfo.InvariantCulture)));
			}

			root.Add(urlElement);

			count++;
			CheckSitemapCount(count);
		}

		var document = new XDocument(root);
		return document;
	}

	/// <summary>
	/// Checks the size of the XML sitemap document. If it is over 10MB, logs an error.
	/// </summary>
	/// <param name="size">The sitemap XML document size.</param>
	public static void CheckDocumentSize(int size)
	{
		if (size >= MaximumSitemapSizeInBytes)
			throw new ArgumentOutOfRangeException(nameof(size), size, "Sitemap exceeds the maximum size of 10MB.");
	}

	/// <summary>
	/// Checks the count of the number of sitemaps. If it is over 50,000, logs an error.
	/// </summary>
	/// <param name="sitemapCount">The sitemap count.</param>
	public static void CheckSitemapCount(int sitemapCount)
	{
		if (sitemapCount > MaximumSitemapCount)
			throw new ArgumentOutOfRangeException(nameof(sitemapCount), sitemapCount, "Sitemap index file exceeds the maximum number of allowed sitemaps of 50,000.");
	}
}