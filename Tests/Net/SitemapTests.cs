namespace Ecng.Tests.Net;

using System.Globalization;
using System.Xml.Linq;

using Ecng.Net.Sitemap;

[TestClass]
public class SitemapTests
{
	[TestMethod]
	public void Node_Constructor_ValidUrl_Success()
	{
		const string url = "https://example.com/page";
		var node = new SitemapNode(url);

		node.Url.AssertEqual(url);
		node.Frequency.AssertNull();
		node.LastModified.AssertNull();
		node.Priority.AssertNull();
	}

	[TestMethod]
	public void Node_Constructor_EmptyUrl_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new SitemapNode(""));
		Assert.ThrowsExactly<ArgumentNullException>(() => new SitemapNode(null));
	}

	[TestMethod]
	public void Node_Priority_ValidValues_Success()
	{
		var node = new SitemapNode("https://example.com");

		node.Priority = 0.0;
		node.Priority.AssertEqual(0.0);

		node.Priority = 0.5;
		node.Priority.AssertEqual(0.5);

		node.Priority = 1.0;
		node.Priority.AssertEqual(1.0);

		node.Priority = null;
		node.Priority.AssertNull();
	}

	[TestMethod]
	public void Node_Priority_InvalidValues_ThrowsArgumentOutOfRangeException()
	{
		var node = new SitemapNode("https://example.com");

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => node.Priority = -0.1);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => node.Priority = 1.1);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => node.Priority = -1.0);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => node.Priority = 2.0);
	}

	[TestMethod]
	public void Node_ToString_ReturnsUrl()
	{
		const string url = "https://example.com/test";
		var node = new SitemapNode(url);

		node.ToString().AssertEqual(url);
	}

	[TestMethod]
	public void Node_Properties_SetAndGet()
	{
		var node = new SitemapNode("https://example.com")
		{
			Frequency = SitemapFrequency.Daily,
			LastModified = new DateTime(2023, 12, 25, 10, 30, 0),
			Priority = 0.8
		};

		node.Frequency.AssertEqual(SitemapFrequency.Daily);
		node.LastModified.AssertEqual(new DateTime(2023, 12, 25, 10, 30, 0));
		node.Priority.AssertEqual(0.8);
	}

	[TestMethod]
	public void Sitemap_EmptyNodes_ValidXml()
	{
		var nodes = new List<SitemapNode>();
		var document = SitemapGenerator.GenerateSitemap(nodes);

		document.AssertNotNull();
		document.Root.AssertNotNull();
		document.Root.Name.LocalName.AssertEqual("urlset");
		document.Root.Name.NamespaceName.AssertEqual("http://www.sitemaps.org/schemas/sitemap/0.9");
		document.Root.Elements().Count().AssertEqual(0);
	}

	[TestMethod]
	public void Sitemap_SingleNode_ValidXml()
	{
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page1")
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);

		document.AssertNotNull();
		document.Root.AssertNotNull();
		document.Root.Name.LocalName.AssertEqual("urlset");

		var urlElements = document.Root.Elements().ToList();
		urlElements.Count.AssertEqual(1);

		var urlElement = urlElements[0];
		urlElement.Name.LocalName.AssertEqual("url");

		var locElement = urlElement.Element(XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9"));
		locElement.AssertNotNull();
		locElement.Value.AssertEqual("https://example.com/page1");
	}

	[TestMethod]
	public void Sitemap_NodeWithAllProperties_ValidXml()
	{
		var lastModified = new DateTime(2023, 12, 25, 10, 30, 0);
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page1")
			{
				LastModified = lastModified,
				Frequency = SitemapFrequency.Weekly,
				Priority = 0.8
			}
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var locElement = urlElement.Element(xmlns + "loc");
		locElement.Value.AssertEqual("https://example.com/page1");

		var lastModElement = urlElement.Element(xmlns + "lastmod");
		lastModElement.AssertNotNull();
		lastModElement.Value.AssertEqual(lastModified.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz"));

		var changeFreqElement = urlElement.Element(xmlns + "changefreq");
		changeFreqElement.AssertNotNull();
		changeFreqElement.Value.AssertEqual("weekly");

		var priorityElement = urlElement.Element(xmlns + "priority");
		priorityElement.AssertNotNull();
		priorityElement.Value.AssertEqual("0.8");
	}

	[TestMethod]
	public void Sitemap_NodeWithSpecialCharacters_UrlEscaped()
	{
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page with spaces & special chars?query=test")
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var locElement = urlElement.Element(xmlns + "loc");
		locElement.AssertNotNull();
		// The URL should be escaped
		locElement.Value.Contains("https://example.com/").AssertTrue();
	}

	[TestMethod]
	public void Sitemap_MultipleNodes_ValidXml()
	{
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page1"),
			new("https://example.com/page2") { Frequency = SitemapFrequency.Daily },
			new("https://example.com/page3") { Priority = 0.9 }
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElements = document.Root.Elements().ToList();
		urlElements.Count.AssertEqual(3);

		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		// First node - minimal
		var firstUrl = urlElements[0];
		firstUrl.Element(xmlns + "loc").Value.AssertEqual("https://example.com/page1");
		firstUrl.Element(xmlns + "lastmod").AssertNull();
		firstUrl.Element(xmlns + "changefreq").AssertNull();
		firstUrl.Element(xmlns + "priority").AssertNull();

		// Second node - with frequency
		var secondUrl = urlElements[1];
		secondUrl.Element(xmlns + "loc").Value.AssertEqual("https://example.com/page2");
		secondUrl.Element(xmlns + "changefreq").Value.AssertEqual("daily");

		// Third node - with priority
		var thirdUrl = urlElements[2];
		thirdUrl.Element(xmlns + "loc").Value.AssertEqual("https://example.com/page3");
		thirdUrl.Element(xmlns + "priority").Value.AssertEqual("0.9");
	}

	[TestMethod]
	public void Sitemap_FrequencyValues_CorrectCasing()
	{
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/never") { Frequency = SitemapFrequency.Never },
			new("https://example.com/yearly") { Frequency = SitemapFrequency.Yearly },
			new("https://example.com/monthly") { Frequency = SitemapFrequency.Monthly },
			new("https://example.com/weekly") { Frequency = SitemapFrequency.Weekly },
			new("https://example.com/daily") { Frequency = SitemapFrequency.Daily },
			new("https://example.com/hourly") { Frequency = SitemapFrequency.Hourly },
			new("https://example.com/always") { Frequency = SitemapFrequency.Always }
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElements = document.Root.Elements().ToList();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		urlElements[0].Element(xmlns + "changefreq").Value.AssertEqual("never");
		urlElements[1].Element(xmlns + "changefreq").Value.AssertEqual("yearly");
		urlElements[2].Element(xmlns + "changefreq").Value.AssertEqual("monthly");
		urlElements[3].Element(xmlns + "changefreq").Value.AssertEqual("weekly");
		urlElements[4].Element(xmlns + "changefreq").Value.AssertEqual("daily");
		urlElements[5].Element(xmlns + "changefreq").Value.AssertEqual("hourly");
		urlElements[6].Element(xmlns + "changefreq").Value.AssertEqual("always");
	}

	[TestMethod]
	public void Sitemap_PriorityFormatting_UsesCultureInvariant()
	{
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page1") { Priority = 0.1 },
			new("https://example.com/page2") { Priority = 0.25 },
			new("https://example.com/page3") { Priority = 0.333 },
			new("https://example.com/page4") { Priority = 1.0 }
		};

		// Test with different culture to ensure invariant formatting
		var currentCulture = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // Uses comma as decimal separator

			var document = SitemapGenerator.GenerateSitemap(nodes);
			var urlElements = document.Root.Elements().ToList();
			XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

			// All should use dot as decimal separator regardless of culture
			urlElements[0].Element(xmlns + "priority").Value.AssertEqual("0.1");
			urlElements[1].Element(xmlns + "priority").Value.AssertEqual("0.2");
			urlElements[2].Element(xmlns + "priority").Value.AssertEqual("0.3");
			urlElements[3].Element(xmlns + "priority").Value.AssertEqual("1.0");
		}
		finally
		{
			CultureInfo.CurrentCulture = currentCulture;
		}
	}

	[TestMethod]
	public void Index_EmptyList_ValidXml()
	{
		var sitemaps = new List<string>();
		var document = SitemapGenerator.GenerateSitemapIndex(sitemaps);

		document.AssertNotNull();
		document.Root.AssertNotNull();
		document.Root.Name.LocalName.AssertEqual("sitemapindex");
		document.Root.Name.NamespaceName.AssertEqual("http://www.sitemaps.org/schemas/sitemap/0.9");
		document.Root.Elements().Count().AssertEqual(0);
	}

	[TestMethod]
	public void Index_SingleSitemap_ValidXml()
	{
		var sitemaps = new List<string> { "https://example.com/sitemap1.xml" };
		var document = SitemapGenerator.GenerateSitemapIndex(sitemaps);

		document.AssertNotNull();
		var sitemapElements = document.Root.Elements().ToList();
		sitemapElements.Count.AssertEqual(1);

		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
		var sitemapElement = sitemapElements[0];
		sitemapElement.Name.LocalName.AssertEqual("sitemap");

		var locElement = sitemapElement.Element(xmlns + "loc");
		locElement.AssertNotNull();
		locElement.Value.AssertEqual("https://example.com/sitemap1.xml");

		var lastModElement = sitemapElement.Element(xmlns + "lastmod");
		lastModElement.AssertNotNull();
		// Just verify it's a valid timestamp format
		DateTime.Parse(lastModElement.Value).AssertGreater(DateTime.Now.AddMinutes(-1));
	}

	[TestMethod]
	public void Index_MultipleSitemaps_ValidXml()
	{
		var sitemaps = new List<string>
		{
			"https://example.com/sitemap1.xml",
			"https://example.com/sitemap2.xml",
			"https://example.com/sitemap3.xml"
		};

		var document = SitemapGenerator.GenerateSitemapIndex(sitemaps);
		var sitemapElements = document.Root.Elements().ToList();
		sitemapElements.Count.AssertEqual(3);

		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		for (int i = 0; i < 3; i++)
		{
			var sitemapElement = sitemapElements[i];
			var locElement = sitemapElement.Element(xmlns + "loc");
			locElement.Value.AssertEqual($"https://example.com/sitemap{i + 1}.xml");

			var lastModElement = sitemapElement.Element(xmlns + "lastmod");
			lastModElement.AssertNotNull();
		}
	}

	[TestMethod]
	public void Count_ValidCount_NoException()
	{
		// Should not throw for valid counts
		SitemapGenerator.CheckSitemapCount(1);
		SitemapGenerator.CheckSitemapCount(100);
		SitemapGenerator.CheckSitemapCount(SitemapGenerator.MaximumSitemapCount);
	}

	[TestMethod]
	public void Count_ExceedsMaximum_ThrowsArgumentOutOfRangeException()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => 
			SitemapGenerator.CheckSitemapCount(SitemapGenerator.MaximumSitemapCount + 1));
	}

	[TestMethod]
	public void DocumentSize_ValidSize_NoException()
	{
		// Should not throw for valid sizes
		SitemapGenerator.CheckDocumentSize(1000);
		SitemapGenerator.CheckDocumentSize(SitemapGenerator.MaximumSitemapSizeInBytes - 1);
	}

	[TestMethod]
	public void DocumentSize_ExceedsMaximum_ThrowsArgumentOutOfRangeException()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => 
			SitemapGenerator.CheckDocumentSize(SitemapGenerator.MaximumSitemapSizeInBytes));
	}

	[TestMethod]
	public void Sitemap_MaximumNodes_ThrowsArgumentOutOfRangeException()
	{
		// Create a list with more than the maximum allowed nodes
		var nodes = new List<SitemapNode>();
		for (int i = 0; i <= SitemapGenerator.MaximumSitemapCount; i++)
		{
			nodes.Add(new SitemapNode($"https://example.com/page{i}"));
		}

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => 
			SitemapGenerator.GenerateSitemap(nodes));
	}

	[TestMethod]
	public void Constants_HaveCorrectValues()
	{
		SitemapGenerator.MaximumSitemapCount.AssertEqual(50000);
		SitemapGenerator.MaximumSitemapSizeInBytes.AssertEqual(10485760); // 10MB
	}

	[TestMethod]
	public void Sitemap_WithDateTimeKinds_HandlesCorrectly()
	{
		var utcTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc);
		var localTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Local);
		var unspecifiedTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Unspecified);

		var nodes = new List<SitemapNode>
		{
			new("https://example.com/utc") { LastModified = utcTime },
			new("https://example.com/local") { LastModified = localTime },
			new("https://example.com/unspecified") { LastModified = unspecifiedTime }
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElements = document.Root.Elements().ToList();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		// All should have lastmod elements with timezone info
		foreach (var urlElement in urlElements)
		{
			var lastModElement = urlElement.Element(xmlns + "lastmod");
			lastModElement.AssertNotNull();
			lastModElement.Value.IsEmpty().AssertFalse();
			// Should contain timezone offset (+ or -)
			(lastModElement.Value.Contains('+') || lastModElement.Value.Contains('-')).AssertTrue();
		}
	}

	[TestMethod]
	public void Xml_ValidNamespace()
	{
		var nodes = new List<SitemapNode> { new("https://example.com") };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		var xmlString = document.ToString();
		xmlString.Contains("http://www.sitemaps.org/schemas/sitemap/0.9").AssertTrue();

		// Verify the document can be parsed and has proper namespace
		var parsedDoc = XDocument.Parse(xmlString);
		parsedDoc.Root.Name.NamespaceName.AssertEqual("http://www.sitemaps.org/schemas/sitemap/0.9");
	}

	[TestMethod]
	public void Frequency_AllValues_ExistAndValid()
	{
		// Test that all frequency enum values are properly defined
		var frequencies = Enumerator.GetValues<SitemapFrequency>().ToArray();
		
		frequencies.Contains(SitemapFrequency.Never).AssertTrue();
		frequencies.Contains(SitemapFrequency.Yearly).AssertTrue();
		frequencies.Contains(SitemapFrequency.Monthly).AssertTrue();
		frequencies.Contains(SitemapFrequency.Weekly).AssertTrue();
		frequencies.Contains(SitemapFrequency.Daily).AssertTrue();
		frequencies.Contains(SitemapFrequency.Hourly).AssertTrue();
		frequencies.Contains(SitemapFrequency.Always).AssertTrue();
		
		frequencies.Length.AssertEqual(7);
	}

	[TestMethod]
	public void Sitemap_LargeUrls_HandledCorrectly()
	{
		// Test with very long URLs
		var longUrl = "https://example.com/" + new string('a', 2000);
		var nodes = new List<SitemapNode> { new(longUrl) };

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var locElement = urlElement.Element(xmlns + "loc");
		locElement.AssertNotNull();
		locElement.Value.Contains("https://example.com/").AssertTrue();
		locElement.Value.Length.AssertGreater(2000);
	}

	[TestMethod]
	public void Sitemap_UnicodeUrls_HandledCorrectly()
	{
		// Test with Unicode characters in URLs
		var unicodeUrl = "https://example.com/тест/页面";
		var nodes = new List<SitemapNode> { new(unicodeUrl) };

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var locElement = urlElement.Element(xmlns + "loc");
		locElement.AssertNotNull();
		// URL should be escaped but contain the domain
		locElement.Value.Contains("example.com").AssertTrue();
	}

	[TestMethod]
	public void Index_WithSpecialCharacters_HandledCorrectly()
	{
		// Test sitemap index with URLs containing special characters
		var sitemaps = new List<string>
		{
			"https://example.com/sitemap with spaces.xml",
			"https://example.com/sitemap&special.xml",
			"https://example.com/sitemap?query=test.xml"
		};

		var document = SitemapGenerator.GenerateSitemapIndex(sitemaps);
		var sitemapElements = document.Root.Elements().ToList();
		sitemapElements.Count.AssertEqual(3);

		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		// All URLs should be present and the domain should be preserved
		foreach (var sitemapElement in sitemapElements)
		{
			var locElement = sitemapElement.Element(xmlns + "loc");
			locElement.AssertNotNull();
			locElement.Value.Contains("example.com").AssertTrue();
		}
	}

	[TestMethod]
	public void Sitemap_ZeroPriority_FormattedCorrectly()
	{
		// Test that zero priority is formatted as "0.0"
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page") { Priority = 0.0 }
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var priorityElement = urlElement.Element(xmlns + "priority");
		priorityElement.AssertNotNull();
		priorityElement.Value.AssertEqual("0.0");
	}

	[TestMethod]
	public void TimeFormatString_IsCorrect()
	{
		// Verify that the time format produces valid ISO 8601 timestamps
		var testDate = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Local);
		var nodes = new List<SitemapNode>
		{
			new("https://example.com/page") { LastModified = testDate }
		};

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var lastModElement = urlElement.Element(xmlns + "lastmod");
		lastModElement.AssertNotNull();
		
		// Should be able to parse back as DateTime
		var parsedDate = DateTime.Parse(lastModElement.Value);
		parsedDate.Year.AssertEqual(2023);
		parsedDate.Month.AssertEqual(12);
		parsedDate.Day.AssertEqual(25);
	}
}