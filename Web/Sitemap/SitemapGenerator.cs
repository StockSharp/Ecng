namespace Ecng.Web.Sitemap
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Xml.Linq;

	/// <summary>
	/// Generates sitemap XML.
	/// </summary>
	public static class SitemapGenerator
	{
		private const string _sitemapsNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

		/// <summary>
		/// The maximum number of sitemaps a sitemap index file can contain.
		/// </summary>
		public const int MaximumSitemapCount = 50000;

		///// <summary>
		///// The maximum number of sitemap nodes allowed in a sitemap file. The absolute maximum allowed is 50,000
		///// according to the specification. See http://www.sitemaps.org/protocol.html but the file size must also be
		///// less than 10MB. After some experimentation, a maximum of 25,000 nodes keeps the file size below 10MB.
		///// </summary>
		//private const int _maximumSitemapNodeCount = 25000;

		/// <summary>
		/// The maximum size of a sitemap file in bytes (10MB).
		/// </summary>
		public const int MaximumSitemapSizeInBytes = 10485760;

		///// <summary>
		///// Gets the collection of XML sitemap documents for the current site. If there are less than 25,000 sitemap
		///// nodes, only one sitemap document will exist in the collection, otherwise a sitemap index document will be
		///// the first entry in the collection and all other entries will be sitemap XML documents.
		///// </summary>
		///// <param name="sitemapNodes">The sitemap nodes for the current site.</param>
		///// <returns>A collection of XML sitemap documents.</returns>
		//protected virtual IList<string> GetSitemapDocuments(ICollection<SitemapNode> sitemapNodes)
		//{
		//	var sitemapCount = (int)Math.Ceiling(sitemapNodes.Count / (double)_maximumSitemapNodeCount);

		//	CheckSitemapCount(sitemapCount);

		//	var sitemaps = Enumerable
		//		.Range(0, sitemapCount)
		//		.ToDictionary(
		//			x => x + 1,
		//			x => sitemapNodes.Skip(x * _maximumSitemapNodeCount).Take(_maximumSitemapNodeCount));

		//	var sitemapDocuments = new List<string>(sitemapCount);

		//	if (sitemapCount > 1)
		//	{
		//		var xml = GetSitemapIndexDocument(sitemaps);
		//		sitemapDocuments.Add(xml);
		//	}

		//	foreach (var sitemap in sitemaps)
		//	{
		//		var xml = GetSitemapDocument(sitemap.Value);
		//		sitemapDocuments.Add(xml);
		//	}

		//	return sitemapDocuments;
		//}

		///// <summary>
		///// Gets the URL to the sitemap with the specified index.
		///// </summary>
		///// <param name="index">The index.</param>
		///// <returns></returns>
		///// <exception cref="NotImplementedException"></exception>
		//protected abstract string GetSitemapUrl(int index);

		///// <summary>
		///// Logs warnings when a sitemap exceeds the maximum size of 10MB or if the sitemap index file exceeds the
		///// maximum number of allowed sitemaps. No exceptions are thrown in this case as the sitemap file is still
		///// generated correctly and search engines may still read it.
		///// </summary>
		///// <param name="exception">The exception to log.</param>
		//protected virtual void LogWarning(Exception exception)
		//{
		//}

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
				//// Get the latest LastModified DateTime from the sitemap nodes or null if there is none.
				//var lastModified = sitemap.Value
				//	.Select(x => x.LastModified)
				//	.Where(x => x.HasValue)
				//	.DefaultIfEmpty()
				//	.Max();

				var sitemapElement = new XElement(
					xmlns + "sitemap",
					new XElement(xmlns + "loc", sitemap),
						new XElement(xmlns + "lastmod",
							DateTime.Now.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")));

				root.Add(sitemapElement);
			}

			var document = new XDocument(root);
			//var xml = document.ToString();
			//CheckDocumentSize(xml);
			return document;
		}

		/// <summary>
		/// Gets the sitemap XML document for the specified set of nodes.
		/// </summary>
		/// <param name="sitemapNodes">The sitemap nodes.</param>
		/// <returns>The sitemap XML document for the specified set of nodes.</returns>
		public static XDocument GenerateSitemap(IEnumerable<SitemapNode> sitemapNodes)
		{
			XNamespace xmlns = _sitemapsNamespace;
			var root = new XElement(xmlns + "urlset");

			var count = 0;

			foreach (var sitemapNode in sitemapNodes)
			{
				var urlElement = new XElement(
					xmlns + "url",
					new XElement(xmlns + "loc", Uri.EscapeUriString(sitemapNode.Url)),
					sitemapNode.LastModified == null ? null : new XElement(
						xmlns + "lastmod",
						sitemapNode.LastModified.Value.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz")),
					sitemapNode.Frequency == null ? null : new XElement(
						xmlns + "changefreq",
						sitemapNode.Frequency.Value.ToString().ToLowerInvariant()),
					sitemapNode.Priority == null ? null : new XElement(
						xmlns + "priority",
						sitemapNode.Priority.Value.ToString("F1", CultureInfo.InvariantCulture)));

				root.Add(urlElement);

				count++;
				CheckSitemapCount(count);
			}

			var document = new XDocument(root);
			//var xml = document.ToString();
			//CheckDocumentSize(xml);
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
}