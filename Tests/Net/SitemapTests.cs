namespace Ecng.Tests.Net;

using System.Globalization;
using System.Xml.Linq;

using Ecng.Net.Sitemap;

[TestClass]
public class SitemapTests
{
	#region SitemapNode Basic Tests

	[TestMethod]
	public void Node_Constructor_ValidUrl_Success()
	{
		const string url = "https://example.com/page";
		var node = new SitemapNode(url);

		node.Url.AssertEqual(url);
		node.Frequency.AssertNull();
		node.LastModified.AssertNull();
		node.Priority.AssertNull();
		node.AlternateLinks.AssertNotNull();
		node.AlternateLinks.Count.AssertEqual(0);
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

	#endregion

	#region XHTML Link Tests (hreflang support)

	[TestMethod]
	public void Node_XhtmlLinks_AddSingleAlternateLink()
	{
		var node = new SitemapNode("https://example.com/page");
		
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-fr", "fr"));
		
		node.AlternateLinks.Count.AssertEqual(1);
		node.AlternateLinks.TryGetLink("fr", out var link).AssertTrue();
		link.Href.AssertEqual("https://example.com/page-fr");
		link.Hreflang.AssertEqual("fr");
	}

	[TestMethod]
	public void Node_XhtmlLinks_AddMultipleAlternateLinks()
	{
		var node = new SitemapNode("https://example.com/page");
		
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-fr", "fr"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-de", "de"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-es", "es"));
		
		node.AlternateLinks.Count.AssertEqual(3);
		node.AlternateLinks.ContainsHreflang("fr").AssertTrue();
		node.AlternateLinks.ContainsHreflang("de").AssertTrue();
		node.AlternateLinks.ContainsHreflang("es").AssertTrue();
	}

	[TestMethod]
	public void XhtmlLink_Constructor_ValidParameters_Success()
	{
		var link = new XhtmlLink("https://example.com/page-fr", "fr");
		link.Href.AssertEqual("https://example.com/page-fr");
		link.Hreflang.AssertEqual("fr");
		link.Rel.AssertEqual("alternate");
	}

	[TestMethod]
	public void XhtmlLink_Constructor_InvalidHref_ThrowsException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new XhtmlLink(null, "fr"));
		Assert.ThrowsExactly<ArgumentNullException>(() => new XhtmlLink("", "fr"));
	}

	[TestMethod]
	public void XhtmlLink_Constructor_InvalidHreflang_ThrowsException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new XhtmlLink("https://example.com", null));
		Assert.ThrowsExactly<ArgumentNullException>(() => new XhtmlLink("https://example.com", ""));
	}

	[TestMethod]
	public void XhtmlLink_Hreflang_ValidLanguageCodes()
	{
		var validCodes = new[] { "en", "fr", "de", "es", "it", "pt", "ru", "zh", "ja", "ko", "ar" };
		
		foreach (var code in validCodes)
		{
			var link = new XhtmlLink($"https://example.com/{code}", code);
			link.Hreflang.AssertEqual(code);
		}
		
		validCodes.Length.AssertEqual(11);
	}

	[TestMethod]
	public void XhtmlLink_Hreflang_RegionalLanguageCodes()
	{
		var regionalCodes = new[] { "en-US", "en-GB", "fr-CA", "es-MX", "pt-BR", "zh-CN", "zh-TW" };
		
		foreach (var code in regionalCodes)
		{
			var link = new XhtmlLink($"https://example.com/{code}", code);
			link.Hreflang.AssertEqual(code);
		}
		
		regionalCodes.Length.AssertEqual(7);
	}

	[TestMethod]
	public void XhtmlLink_Hreflang_XDefault()
	{
		var link = new XhtmlLink("https://example.com/", "x-default");
		link.Hreflang.AssertEqual("x-default");
	}

	#endregion

	#region Sitemap Generation Tests

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
	public void Sitemap_NodeWithXhtmlLinks_ValidXml()
	{
		var node = new SitemapNode("https://example.com/page");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-fr", "fr"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-de", "de"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		document.AssertNotNull();
		var urlElement = document.Root.Elements().First();
		XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";

		var locElement = urlElement.Element(xmlns + "loc");
		locElement.Value.AssertEqual("https://example.com/page");

		var xhtmlLinks = urlElement.Elements(xhtmlNs + "link").ToList();
		xhtmlLinks.Count.AssertEqual(2);

		var frLink = xhtmlLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "fr");
		frLink.AssertNotNull();
		frLink.Attribute("href")?.Value.AssertEqual("https://example.com/page-fr");
		frLink.Attribute("rel")?.Value.AssertEqual("alternate");

		var deLink = xhtmlLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "de");
		deLink.AssertNotNull();
		deLink.Attribute("href")?.Value.AssertEqual("https://example.com/page-de");
	}

	[TestMethod]
	public void Sitemap_WithXhtmlNamespace_ValidXml()
	{
		var node = new SitemapNode("https://example.com/page");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page-fr", "fr"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		var xmlString = document.ToString();
		xmlString.Contains("xmlns:xhtml=\"http://www.w3.org/1999/xhtml\"").AssertTrue();
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

	#endregion

	#region Multilingual Sitemap Tests

	[TestMethod]
	public void Sitemap_MultilingualPages_AllLanguageVersions()
	{
		// Test complete multilingual setup
		var nodes = new List<SitemapNode>();

		// English page with alternates
		var enNode = new SitemapNode("https://example.com/page");
		enNode.AlternateLinks.Add(new XhtmlLink("https://example.com/page", "en"));
		enNode.AlternateLinks.Add(new XhtmlLink("https://example.com/fr/page", "fr"));
		enNode.AlternateLinks.Add(new XhtmlLink("https://example.com/de/page", "de"));
		nodes.Add(enNode);

		// French page with alternates
		var frNode = new SitemapNode("https://example.com/fr/page");
		frNode.AlternateLinks.Add(new XhtmlLink("https://example.com/page", "en"));
		frNode.AlternateLinks.Add(new XhtmlLink("https://example.com/fr/page", "fr"));
		frNode.AlternateLinks.Add(new XhtmlLink("https://example.com/de/page", "de"));
		nodes.Add(frNode);

		// German page with alternates
		var deNode = new SitemapNode("https://example.com/de/page");
		deNode.AlternateLinks.Add(new XhtmlLink("https://example.com/page", "en"));
		deNode.AlternateLinks.Add(new XhtmlLink("https://example.com/fr/page", "fr"));
		deNode.AlternateLinks.Add(new XhtmlLink("https://example.com/de/page", "de"));
		nodes.Add(deNode);

		var document = SitemapGenerator.GenerateSitemap(nodes);
		var urlElements = document.Root.Elements().ToList();
		urlElements.Count.AssertEqual(3);

		// Each URL should have the same alternate links
		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		foreach (var urlElement in urlElements)
		{
			var xhtmlLinks = urlElement.Elements(xhtmlNs + "link").ToList();
			xhtmlLinks.Count.AssertEqual(3);
			
			var enLink = xhtmlLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "en");
			enLink.AssertNotNull();
			
			var frLink = xhtmlLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "fr");
			frLink.AssertNotNull();
			
			var deLink = xhtmlLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "de");
			deLink.AssertNotNull();
		}
	}

	[TestMethod]
	public void Sitemap_XDefaultLanguage_CorrectHandling()
	{
		var node = new SitemapNode("https://example.com/");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/", "x-default"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/en/", "en"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/fr/", "fr"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		var urlElement = document.Root.Elements().First();
		var xDefaultLink = urlElement.Elements(xhtmlNs + "link")
		    .FirstOrDefault(e => e.Attribute("hreflang")?.Value == "x-default");
		xDefaultLink.AssertNotNull();
		xDefaultLink.Attribute("href")?.Value.AssertEqual("https://example.com/");
	}

	[TestMethod]
	public void Sitemap_RegionalLanguages_CorrectHreflangValues()
	{
		var node = new SitemapNode("https://example.com/en-us/");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/en-us/", "en-US"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/en-gb/", "en-GB"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/fr-ca/", "fr-CA"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		var urlElement = document.Root.Elements().First();
		var regionalLinks = urlElement.Elements(xhtmlNs + "link").ToList();
		
		var usLink = regionalLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "en-US");
		usLink.AssertNotNull();
		usLink.Attribute("href")?.Value.AssertEqual("https://example.com/en-us/");
		
		var gbLink = regionalLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "en-GB");
		gbLink.AssertNotNull();
		
		var caLink = regionalLinks.FirstOrDefault(e => e.Attribute("hreflang")?.Value == "fr-CA");
		caLink.AssertNotNull();
	}

	#endregion

	#region Advanced XHTML Link Tests

	[TestMethod]
	public void XhtmlLink_DuplicateHreflang_ReplacesExisting()
	{
		var node = new SitemapNode("https://example.com/page");
		
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/fr1/", "fr"));
		node.AlternateLinks.Count.AssertEqual(1);
		
		// Adding duplicate hreflang should replace the existing one
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/fr2/", "fr"));
		node.AlternateLinks.Count.AssertEqual(1);
		
		node.AlternateLinks.TryGetLink("fr", out var link).AssertTrue();
		link.Href.AssertEqual("https://example.com/fr2/");
	}

	[TestMethod]
	public void XhtmlLink_CaseInsensitiveHreflang_HandledCorrectly()
	{
		var node = new SitemapNode("https://example.com/page");
		
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/", "en-US"));
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/", "en-us"));
		
		// Should be treated as the same hreflang (case-insensitive)
		node.AlternateLinks.Count.AssertEqual(1);
		node.AlternateLinks.ContainsHreflang("EN-US").AssertTrue();
		node.AlternateLinks.ContainsHreflang("en-us").AssertTrue();
	}

	[TestMethod]
	public void XhtmlLink_ToString_ReturnsCorrectFormat()
	{
		var link = new XhtmlLink("https://example.com/page-fr", "fr");
		link.ToString().AssertEqual("fr: https://example.com/page-fr");
	}

	[TestMethod]
	public void XhtmlLink_Equals_BasedOnHreflang()
	{
		var link1 = new XhtmlLink("https://example.com/page1", "fr");
		var link2 = new XhtmlLink("https://example.com/page2", "fr");
		var link3 = new XhtmlLink("https://example.com/page1", "de");
		
		link1.Equals(link2).AssertTrue(); // Same hreflang
		link1.Equals(link3).AssertFalse(); // Different hreflang
	}

	[TestMethod]
	public void Sitemap_XhtmlLinksWithSpecialCharacters_ProperEscaping()
	{
		var node = new SitemapNode("https://example.com/page");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page with spaces?query=test", "fr"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		var urlElement = document.Root.Elements().First();
		var xhtmlLink = urlElement.Elements(xhtmlNs + "link").First();
		var href = xhtmlLink.Attribute("href")?.Value;
		href.AssertNotNull();
		href.Contains("example.com").AssertTrue();
	}

	[TestMethod]
	public void Sitemap_XhtmlLinksUnicodeUrls_ProperHandling()
	{
		var node = new SitemapNode("https://example.com/тест");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/тест/fr", "fr"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		var urlElement = document.Root.Elements().First();
		var xhtmlLink = urlElement.Elements(xhtmlNs + "link").First();
		var href = xhtmlLink.Attribute("href")?.Value;
		href.AssertNotNull();
		// URL should be properly escaped
		href.Contains("example.com").AssertTrue();
	}

	#endregion
	#region Frequency and Priority Tests

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

	#endregion

	#region Sitemap Index Tests

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

	#endregion

	#region Validation and Limits Tests

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

	#endregion

	#region DateTime and Timezone Tests

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

	#endregion

	#region XML Structure and Namespace Tests

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
	public void Xml_XhtmlNamespaceWhenNeeded()
	{
		var nodeWithLinks = new SitemapNode("https://example.com/page");
		nodeWithLinks.AlternateLinks.Add(new XhtmlLink("https://example.com/fr/", "fr"));

		var nodeWithoutLinks = new SitemapNode("https://example.com/simple");

		// Document with XHTML links should include XHTML namespace
		var docWithLinks = SitemapGenerator.GenerateSitemap([nodeWithLinks]);
		var xmlWithLinks = docWithLinks.ToString();
		xmlWithLinks.Contains("xmlns:xhtml=\"http://www.w3.org/1999/xhtml\"").AssertTrue();

		// Document without XHTML links should not include XHTML namespace
		var docWithoutLinks = SitemapGenerator.GenerateSitemap([nodeWithoutLinks]);
		var xmlWithoutLinks = docWithoutLinks.ToString();
		xmlWithoutLinks.Contains("xmlns:xhtml").AssertFalse();
	}

	#endregion

	#region Edge Cases and Error Handling

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
	public void Sitemap_ManyXhtmlLinks_HandledCorrectly()
	{
		var node = new SitemapNode("https://example.com/page");
		
		// Add many alternate links
		for (int i = 0; i < 50; i++)
		{
			node.AlternateLinks.Add(new XhtmlLink($"https://example.com/lang{i}/page", $"lang{i}"));
		}
		
		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);
		
		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		var urlElement = document.Root.Elements().First();
		var xhtmlLinks = urlElement.Elements(xhtmlNs + "link").ToList();
		xhtmlLinks.Count.AssertEqual(50);
	}

	[TestMethod]
	public void XhtmlLink_SelfReference_AllowedAndValid()
	{
		var node = new SitemapNode("https://example.com/page");
		node.AlternateLinks.Add(new XhtmlLink("https://example.com/page", "en"));

		var nodes = new List<SitemapNode> { node };
		var document = SitemapGenerator.GenerateSitemap(nodes);

		XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";
		var urlElement = document.Root.Elements().First();
		var selfLink = urlElement.Elements(xhtmlNs + "link")
		    .FirstOrDefault(e => e.Attribute("href")?.Value == "https://example.com/page");
		selfLink.AssertNotNull();
		selfLink.Attribute("hreflang")?.Value.AssertEqual("en");
	}

	[TestMethod]
	public void XhtmlLinkCollection_RemoveOperations_WorkCorrectly()
	{
		var collection = new XhtmlLinkCollection();
		var link1 = new XhtmlLink("https://example.com/fr", "fr");
		var link2 = new XhtmlLink("https://example.com/de", "de");
		
		collection.Add(link1);
		collection.Add(link2);
		collection.Count.AssertEqual(2);
		
		// Remove by object
		collection.Remove(link1).AssertTrue();
		collection.Count.AssertEqual(1);
		collection.ContainsHreflang("fr").AssertFalse();
		
		// Remove by hreflang
		collection.RemoveHreflang("de").AssertTrue();
		collection.Count.AssertEqual(0);
	}

	[TestMethod]
	public void XhtmlLinkCollection_CollectionOperations_WorkCorrectly()
	{
		var collection = new XhtmlLinkCollection();
		var links = new[]
		{
			new XhtmlLink("https://example.com/fr", "fr"),
			new XhtmlLink("https://example.com/de", "de"),
			new XhtmlLink("https://example.com/es", "es")
		};
		
		foreach (var link in links)
		{
			collection.Add(link);
		}
		
		// Test Contains
		collection.Contains(links[0]).AssertTrue();
		
		// Test CopyTo
		var array = new XhtmlLink[5];
		collection.CopyTo(array, 1);
		array[1].AssertNotNull();
		array[2].AssertNotNull();
		array[3].AssertNotNull();
		array[0].AssertNull();
		array[4].AssertNull();
		
		// Test enumeration
		var enumeratedLinks = collection.ToList();
		enumeratedLinks.Count.AssertEqual(3);
		
		// Test Clear
		collection.Clear();
		collection.Count.AssertEqual(0);
	}

	#endregion
}