namespace Ecng.Tests.Markdown;

using System.Text.RegularExpressions;

using Ecng.Markdown;

[TestClass]
public class MarkdownTests : BaseTestClass
{
	private static readonly Dictionary<long, (string url, string name, string description)> _products = new()
	{
		[9] = ("/store/designer/", "S#.Designer", "Algorithmic trading strategy designer"),
		[456] = ("/store/shell/", "S#.Shell", "Ready-made trading robot framework"),
	};

	private static readonly Dictionary<long, (string url, string name, string description)> _users = new()
	{
		[1] = ("/users/stocksharp/", "StockSharp", "Official StockSharp account"),
		[123] = ("/users/john/", "John Smith", "Active community member"),
	};

	private static readonly Dictionary<long, (string url, string name, string description)> _topics = new()
	{
		[789] = ("/forum/getting-started/", "Getting Started Guide", "Beginner's guide to StockSharp"),
		[12374] = ("/forum/shell-overview/", "S#.Shell Overview", "Overview of S#.Shell features"),
	};

	private static readonly Dictionary<long, (string url, string name, string description)> _messages = new()
	{
		[51317] = ("/forum/getting-started/faq/#51317", "FAQ", "Frequently asked questions"),
	};

	private static readonly Dictionary<long, (string url, string name, string description)> _pages = new()
	{
		[239] = ("/documentation/quick-start/", "Quick Start", "Quick start documentation page"),
	};

	private static (string url, string name, string description) Resolve(Dictionary<long, (string url, string name, string description)> map, long id, string fallback)
		=> map.TryGetValue(id, out var v) ? v : ($"/{fallback}/{id}", $"{fallback}{id}", $"{fallback}{id}");

	// What the API resolves the site counters to: already formatted for the language being rendered.
	private static readonly Dictionary<SiteCounters, string> _counters = new()
	{
		[SiteCounters.Indicators] = "70",
		[SiteCounters.Connectors] = "93",
		[SiteCounters.Users] = "22 431",
		[SiteCounters.Strategies] = "156",
		[SiteCounters.Apps] = "12",
	};

	private const string _knownDiagram = "555";
	private const long _pendingVideo = 7;

	private static readonly Md2HtmlFormatter _formatter = new();

	private static ResolvedMarkdownData ResolveTestData(ParsedMarkdown parsed) => new()
	{
		Counters = parsed.CounterRefs.ToDictionary(c => c, c => _counters[c]),
		Entities = parsed.EntityRefs.ToDictionary(r => r, r => r.type switch
		{
			"product" or "product_name" => Resolve(_products, r.id, "product"),
			"user" => Resolve(_users, r.id, "user"),
			"topic" => Resolve(_topics, r.id, "topic"),
			"message" => Resolve(_messages, r.id, "message"),
			"page" => Resolve(_pages, r.id, "page"),
			_ => ($"/{r.type}/{r.id}", r.type, r.type),
		}),
		Files = parsed.FileIds.ToDictionary(id => id, id => ($"~/file/{id}/file.png", $"File{id}", $"File{id}")),
		Roles = parsed.RoleIds.ToDictionary(id => id, id => id == 1),
		// Either an address to play, or the already-localized reason it cannot be played. The markup around
		// it is the renderer's business. Id 0 stands for a video still being processed.
		Videos = parsed.VideoIds.ToDictionary(id => id, id => id == _pendingVideo
			? new ResolvedVideo(string.Empty, "Video is being processed")
			: new ResolvedVideo($"/video/{id}", string.Empty)),

		// The caller resolves a reference to the schema's address and nothing more. Turning that address into
		// markup is the renderer's job, so a non-HTML renderer can do it differently. Only _knownDiagram
		// resolves, so the unresolved path stays covered.
		Diagrams = parsed.DiagramRefs
			.Where(r => r == _knownDiagram)
			.ToDictionary(r => r, r => $"https://stocksharp.com/file/{r}/schema.json"),
	};

	private static string ToHtml(string text, bool allowHtml = false)
	{
		if (text.IsEmptyOrWhiteSpace())
			return text;

		var parsed = _formatter.Parse(text, allowHtml);
		var data = ResolveTestData(parsed);
		return _formatter.Render(parsed, data);
	}

	[TestMethod]
	public void Bold()
	{
		var html = ToHtml("**bold**");
		html.Contains("<strong>bold</strong>").AssertTrue();
	}

	[TestMethod]
	public void Italic()
	{
		var html = ToHtml("*italic*");
		html.Contains("<em>italic</em>").AssertTrue();
	}

	[TestMethod]
	public void Strikethrough()
	{
		var html = ToHtml("~~strikethrough~~");
		html.Contains("<del>strikethrough</del>").AssertTrue();
	}

	[TestMethod]
	public void Heading()
	{
		var html = ToHtml("## Heading 2");
		(html.Contains("<h2") && html.Contains("Heading 2</h2>")).AssertTrue();
	}

	[TestMethod]
	public void Link()
	{
		var html = ToHtml("[link](https://example.com)");
		html.Contains("href=\"https://example.com\"").AssertTrue();
		html.Contains("link</a>").AssertTrue();
	}

	[TestMethod]
	public void EmailAutoLink()
	{
		var html = ToHtml("info@stocksharp.com");
		html.Contains("href=\"mailto:info@stocksharp.com\"").AssertTrue();
		html.Contains(">info@stocksharp.com</a>").AssertTrue();
	}

	[TestMethod]
	public void EmailAutoLinkInSentence()
	{
		var html = ToHtml("Contact us at support@stocksharp.com for help.");
		html.Contains("href=\"mailto:support@stocksharp.com\"").AssertTrue();
		// The surrounding words must not be swallowed into the link.
		html.Contains("Contact us at ").AssertTrue();
		html.Contains(" for help.").AssertTrue();
	}

	[TestMethod]
	public void EmailWithSubaddressAndSubdomain()
	{
		var html = ToHtml("john.doe+tag@mail.example.co.uk");
		html.Contains("href=\"mailto:john.doe+tag@mail.example.co.uk\"").AssertTrue();
	}

	[TestMethod]
	public void EmailNotLinkedInInlineCode()
	{
		// Inside code the address is verbatim text, so it must stay unlinked.
		var html = ToHtml("`info@stocksharp.com`");
		html.Contains("mailto:").AssertFalse();
	}

	[TestMethod]
	public void ExplicitMailtoLinkStillWorks()
	{
		var html = ToHtml("[write us](mailto:info@stocksharp.com)");
		html.Contains("href=\"mailto:info@stocksharp.com\"").AssertTrue();
		html.Contains("write us</a>").AssertTrue();
	}

	[TestMethod]
	public void CodeBlock()
	{
		var html = ToHtml("```\nvar x = 1;\n```");
		html.Contains("<code>").AssertTrue();
		html.Contains("var x = 1;").AssertTrue();
	}

	[TestMethod]
	public void InlineCode()
	{
		var html = ToHtml("`code`");
		html.Contains("<code>code</code>").AssertTrue();
	}

	[TestMethod]
	public void AlphaList()
	{
		var html = ToHtml("a. First\nb. Second\nc. Third");
		html.Contains("<ol type=\"a\"").AssertTrue();
		html.Contains("<li>First</li>").AssertTrue();
		html.Contains("<li>Third</li>").AssertTrue();
	}

	[TestMethod]
	public void UpperAlphaList()
	{
		var html = ToHtml("A. First\nB. Second");
		html.Contains("<ol type=\"A\"").AssertTrue();
	}

	[TestMethod]
	public void RomanList()
	{
		var html = ToHtml("i. First\nii. Second\niii. Third");
		html.Contains("<ol type=\"i\"").AssertTrue();
		html.Contains("<li>Second</li>").AssertTrue();
	}

	[TestMethod]
	public void UpperRomanList()
	{
		var html = ToHtml("I. First\nII. Second");
		html.Contains("<ol type=\"I\"").AssertTrue();
	}

	[TestMethod]
	public void AlphaListStaysAlphaThroughI()
	{
		// A list a..i must remain one alpha list: "i." here is the 9th letter, not roman 1.
		var html = ToHtml("a. a\nb. b\nc. c\nd. d\ne. e\nf. f\ng. g\nh. h\ni. i");
		html.Contains("<ol type=\"a\"").AssertTrue();
		html.Contains("<ol type=\"i\"").AssertFalse();
	}

	[TestMethod]
	public void NumberedListStillWorks()
	{
		var html = ToHtml("1. First\n2. Second");
		html.Contains("<ol").AssertTrue();
		html.Contains("<ol type").AssertFalse();
		html.Contains("<li>First</li>").AssertTrue();
	}

	[TestMethod]
	public void AbbreviationIsNotAList()
	{
		// "e.g." has no space after the marker dot, so it must stay prose, not a letter list.
		var html = ToHtml("e.g. this is prose");
		html.Contains("<ol").AssertFalse();
	}


	[TestMethod]
	public void Table()
	{
		var html = ToHtml("| A | B |\n|---|---|\n| 1 | 2 |");
		(html.Contains("<table>") || html.Contains("<table")).AssertTrue();
		html.Contains("<td>").AssertTrue();
	}

	[TestMethod]
	public void Table_AfterParagraphWithoutBlankLine()
	{
		// A table glued directly to the preceding paragraph (no blank line between) must still render.
		var html = ToHtml("Payment systems:\n| A | B |\n| --- | --- |\n| 1 | 2 |");
		(html.Contains("<table>") || html.Contains("<table")).AssertTrue();
	}

	[TestMethod]
	public void Table_WithHtmlInCells()
	{
		// Exact shape of the migrated payment-systems table on /payways: a cell starting with a block
		// HTML tag (<div>) around an image, rendered with HTML allowed. Markdig otherwise treats the
		// "<div>" line as an HTML block and never sees the table, leaving raw "|" / "---" text.
		var html = ToHtml("Payment systems:\n| <div>A</div> | <div>B</div> |\n| --- | --- |\n", allowHtml: true);
		(html.Contains("<table>") || html.Contains("<table")).AssertTrue();
	}

	[TestMethod]
	public void Table_PaywaysExact()
	{
		// The exact markdown of the /payways payment-systems table: image-in-div cells with trailing
		// text, glued to the intro paragraph, rendered with HTML allowed.
		var md = "Payment systems:\n"
			+ "| <div>![](126887)</div> MIR; | <div>![](126888)</div> VISA International; | <div>![](126889)</div> Mastercard Worldwide; | <div>![](126890)</div> JCB; |\n"
			+ "| --- | --- | --- | --- |\n"
			+ "For payment you will be redirected to the gateway.";
		var html = ToHtml(md, allowHtml: true);
		(html.Contains("<table>") || html.Contains("<table")).AssertTrue();
	}

	[TestMethod]
	public void Table_InsideAlignBlock()
	{
		// A pipe table wrapped in a :::center align block (legacy "centered table" layout) must still
		// render as a table, not leave the "|" / "---" rows as literal text inside the centered block.
		var md = ":::center\n| A | B |\n| --- | --- |\n| 1 | 2 |\n:::";
		var html = ToHtml(md);
		html.Contains("text-align:center").AssertTrue($"Expected centered block, got: {html}");
		(html.Contains("<table>") || html.Contains("<table")).AssertTrue($"Expected a table inside the align block, got: {html}");
	}

	[TestMethod]
	public void Table_InsideAlignBlock_WithHtmlCells()
	{
		// Exact shape of the migrated office-photo table on /company/contacts/: an image-in-div table
		// centered with :::center. The table must render instead of emitting raw "|" / "---" text.
		var md = ":::center\n| <div>![](103848)</div> | | |\n| --- | --- | --- |\n:::";
		var html = ToHtml(md, allowHtml: true);
		html.Contains("text-align:center").AssertTrue($"Expected centered block, got: {html}");
		(html.Contains("<table>") || html.Contains("<table")).AssertTrue($"Expected a table inside the align block, got: {html}");
	}

	[TestMethod]
	public void FeatureSection_Left_SplitsImageAndText()
	{
		// :::feature-left renders a two-column scroll-reveal section: the first image goes to the media
		// half, the rest (heading + text) to the text half. The image side is explicit in the syntax
		// (feature-left = image on the left), so authors control the layout per section, no auto-mirror.
		var md = ":::feature-left\n![](https://cdn.example/shot.png)\n\n## Title\n\nText paragraph.\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-feature").AssertTrue($"Expected a feature section, got: {html}");
		html.Contains("ss-md-feature--left").AssertTrue($"Expected the image-left variant, got: {html}");
		html.Contains("ss-md-feature__media").AssertTrue($"Expected a media half, got: {html}");
		html.Contains("ss-md-feature__text").AssertTrue($"Expected a text half, got: {html}");
		html.Contains("ss-md-reveal").AssertTrue($"Expected the scroll-reveal hook class, got: {html}");

		var mediaIdx = html.IndexOf("ss-md-feature__media", StringComparison.Ordinal);
		var textIdx = html.IndexOf("ss-md-feature__text", StringComparison.Ordinal);
		var imgIdx = html.IndexOf("<img", StringComparison.Ordinal);
		var headingIdx = html.IndexOf("<h2", StringComparison.Ordinal);
		(mediaIdx < textIdx).AssertTrue($"Media half must come before the text half, got: {html}");
		(imgIdx > mediaIdx && imgIdx < textIdx).AssertTrue($"The image must live in the media half, got: {html}");
		(headingIdx > textIdx).AssertTrue($"The heading must live in the text half, got: {html}");
	}

	[TestMethod]
	public void SplitBlock_LayersImages()
	{
		// :::split lays screenshots of the same screen on top of each other so one frame shows both looks
		// (the light and the dark theme). The first image is the base, every later one is an overlay the
		// stylesheet clips diagonally.
		var md = ":::split\n![](https://cdn.example/dark.png)\n\n![](https://cdn.example/light.png)\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-split").AssertTrue($"Expected a split block, got: {html}");
		html.Contains("ss-md-split__layer--base").AssertTrue($"Expected a base layer, got: {html}");
		html.Contains("ss-md-split__layer--over").AssertTrue($"Expected an overlay layer, got: {html}");

		var baseIdx = html.IndexOf("ss-md-split__layer--base", StringComparison.Ordinal);
		var overIdx = html.IndexOf("ss-md-split__layer--over", StringComparison.Ordinal);
		(baseIdx < overIdx).AssertTrue($"The first image must be the base layer, got: {html}");

		Regex.Matches(html, "<img").Count.AssertEqual(2, $"Both images must survive, got: {html}");
	}

	[TestMethod]
	public void SplitBlock_FillsTheMediaHalfOfAFeature()
	{
		// A split pair is a media block like a plain image, so it can be the media half of a feature
		// section instead of ending up in the text column.
		var md = ":::feature-right\n:::split\n![](https://cdn.example/dark.png)\n\n![](https://cdn.example/light.png)\n:::\n\n## Themes\n\nText.\n:::";
		var html = ToHtml(md, allowHtml: true);

		var mediaIdx = html.IndexOf("ss-md-feature__media", StringComparison.Ordinal);
		var textIdx = html.IndexOf("ss-md-feature__text", StringComparison.Ordinal);
		var splitIdx = html.IndexOf("ss-md-split", StringComparison.Ordinal);

		(mediaIdx >= 0 && textIdx > mediaIdx).AssertTrue($"Expected both halves, got: {html}");
		(splitIdx > mediaIdx && splitIdx < textIdx).AssertTrue($"The split pair must live in the media half, got: {html}");
	}

	[TestMethod]
	public void FeatureSection_Right_Variant()
	{
		// :::feature-right is the mirror (image on the right). No auto-alternation: the author states the
		// side explicitly per section, so a page can keep every image on one side if desired.
		var md = ":::feature-right\n![](https://cdn.example/shot.png)\n\n## Title\n\nText.\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-feature--right").AssertTrue($"Expected the image-right variant, got: {html}");
		html.Contains("ss-md-feature__media").AssertTrue($"Expected a media half, got: {html}");
		html.Contains("ss-md-feature__text").AssertTrue($"Expected a text half, got: {html}");
	}

	[TestMethod]
	public void FeatureSection_AltTone_AddsBandModifier()
	{
		// ":::feature-left alt" puts the section on a tinted full-width band, so a long page reads as a
		// sequence of sections instead of one continuous column.
		var md = ":::feature-left alt\n![](https://cdn.example/shot.png)\n\n## Title\n\nText.\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-feature--left").AssertTrue($"Expected the image-left variant, got: {html}");
		html.Contains("ss-md-feature--alt").AssertTrue($"Expected the tinted-band modifier, got: {html}");
	}

	[TestMethod]
	public void Cards_EachHeadingBecomesACard()
	{
		// :::cards turns every "### heading + body" into one card of a responsive grid.
		var md = ":::cards\n### Backtesting\nAny data types.\n\n### Live trading\nMany brokers.\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-cards").AssertTrue($"Expected the cards grid, got: {html}");
		html.Contains("ss-md-reveal").AssertTrue($"Expected the scroll-reveal hook, got: {html}");

		var cards = Regex.Matches(html, "class=\"ss-md-card\"").Count;
		cards.AreEqual(2, $"Expected two cards, got: {html}");

		html.Contains("Backtesting").AssertTrue($"Expected the first card title, got: {html}");
		html.Contains("Any data types.").AssertTrue($"Expected the first card body, got: {html}");
		html.Contains("Live trading").AssertTrue($"Expected the second card title, got: {html}");
	}

	[TestMethod]
	public void Stats_SplitValueAndLabel()
	{
		// :::stats renders one big number per line: "value | label".
		var md = ":::stats\n70+ | indicators\n100+ | connectors\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-stats").AssertTrue($"Expected the stats row, got: {html}");

		var stats = Regex.Matches(html, "class=\"ss-md-stat\"").Count;
		stats.AreEqual(2, $"Expected two stats, got: {html}");

		html.Contains(">70+<").AssertTrue($"Expected the first value, got: {html}");
		html.Contains(">indicators<").AssertTrue($"Expected the first label, got: {html}");
		html.Contains(">100+<").AssertTrue($"Expected the second value, got: {html}");
	}

	[TestMethod]
	public void Stats_ValueFromASiteCounter()
	{
		// A row of big numbers is exactly where a hand-typed figure goes stale, so the counters have to
		// survive it: the value is a counter, and the label names a product.
		var md = ":::stats\n@connector_count | connectors\n@indicator_count | indicators\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("{{count:").AssertFalse($"The internal placeholder leaked: {html}");
		html.Contains($">{_counters[SiteCounters.Connectors]}<").AssertTrue($"Expected the connectors count, got: {html}");
		html.Contains($">{_counters[SiteCounters.Indicators]}<").AssertTrue($"Expected the indicators count, got: {html}");
	}

	[TestMethod]
	public void Stats_LabelFromAProductName()
	{
		var md = ":::stats\n1 | @product_name(9)\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("{{entity:").AssertFalse($"The internal placeholder leaked: {html}");
		html.Contains(_products[9].name).AssertTrue($"Expected the product name in the label, got: {html}");
	}

	[TestMethod]
	public void Cta_FirstLinkIsPrimary()
	{
		// :::cta turns the links into buttons; the first one is the primary call to action.
		var md = ":::cta\n[Download](/download) [Docs](/doc)\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-cta").AssertTrue($"Expected the CTA row, got: {html}");
		html.Contains("ss-md-btn--primary").AssertTrue($"Expected a primary button, got: {html}");

		var primary = Regex.Matches(html, "ss-md-btn--primary").Count;
		primary.AreEqual(1, $"Only the first link may be primary, got: {html}");

		var buttons = Regex.Matches(html, "class=\"ss-md-btn").Count;
		buttons.AreEqual(2, $"Expected two buttons, got: {html}");

		html.Contains("href=\"/download\"").AssertTrue($"Expected the download link, got: {html}");
		html.Contains(">Docs<").AssertTrue($"Expected the secondary label, got: {html}");
	}

	[TestMethod]
	public void Steps_AreNumberedInOrder()
	{
		// :::steps is an ordered walkthrough: the numbering encodes a real sequence, not decoration.
		var md = ":::steps\n### Install\nGet the installer.\n\n### Connect\nAdd a broker.\n\n### Trade\nRun it.\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-steps").AssertTrue($"Expected the steps list, got: {html}");

		var steps = Regex.Matches(html, "class=\"ss-md-step\"").Count;
		steps.AreEqual(3, $"Expected three steps, got: {html}");

		html.Contains(">1<").AssertTrue($"Expected the first ordinal, got: {html}");
		html.Contains(">3<").AssertTrue($"Expected the last ordinal, got: {html}");

		var first = html.IndexOf("Install", StringComparison.Ordinal);
		var last = html.IndexOf("Trade", StringComparison.Ordinal);
		(first < last).AssertTrue($"Steps must keep their authored order, got: {html}");
	}

	[TestMethod]
	public void Quote_LastDashLineBecomesAttribution()
	{
		// :::quote renders a testimonial; a trailing line that starts with an em dash is the attribution.
		var md = ":::quote\nBuilt an arbitrage scheme in one evening.\n\n— Alex K.\n:::";
		var html = ToHtml(md, allowHtml: true);

		html.Contains("ss-md-quote").AssertTrue($"Expected the quote block, got: {html}");
		html.Contains("<footer").AssertTrue($"Expected an attribution footer, got: {html}");
		html.Contains("Alex K.").AssertTrue($"Expected the attribution text, got: {html}");

		var quoteIdx = html.IndexOf("Built an arbitrage", StringComparison.Ordinal);
		var footerIdx = html.IndexOf("<footer", StringComparison.Ordinal);
		(quoteIdx < footerIdx).AssertTrue($"The attribution must follow the quote, got: {html}");
	}

	[TestMethod]
	public void Video_YouTube_RendersCompactPosterCard()
	{
		// A YouTube embed is heavy and dominates the column, so it renders as a compact poster card that
		// only loads the player once the reader asks for it (the site script opens it in an overlay).
		var html = ToHtml("::iframe{url=https://www.youtube.com/embed/dkf4ZzBp2ZA}", allowHtml: true);

		html.Contains("ss-md-video").AssertTrue($"Expected the compact video card, got: {html}");
		html.Contains("dkf4ZzBp2ZA").AssertTrue($"Expected the video id to survive, got: {html}");
		html.Contains("img.youtube.com/vi/dkf4ZzBp2ZA").AssertTrue($"Expected the poster thumbnail, got: {html}");
		html.Contains("<iframe").AssertFalse($"The player must not load until requested, got: {html}");
	}

	[TestMethod]
	[DataRow("https://player.vimeo.com/video/69308006", "https://player.vimeo.com/video/69308006")]
	[DataRow("https://vimeo.com/69308006", "https://player.vimeo.com/video/69308006")]
	[DataRow("https://rutube.ru/video/abc123def/", "https://rutube.ru/play/embed/abc123def")]
	[DataRow("https://rutube.ru/play/embed/abc123def", "https://rutube.ru/play/embed/abc123def")]
	public void Video_OtherHosts_AlsoOpenInTheOverlay(string url, string expectedPlayer)
	{
		// Vimeo and Rutube get the same treatment as YouTube: no player until the reader asks for one. They
		// expose no thumbnail at a predictable url, so the card shows a blank poster instead of an image.
		var html = ToHtml($"::iframe{{url={url}}}", allowHtml: true);

		html.Contains("class=\"ss-md-video").AssertTrue($"Expected the video card, got: {html}");
		html.Contains($"data-video=\"{expectedPlayer}\"").AssertTrue($"Expected the player url, got: {html}");
		html.Contains("ss-md-video--noposter").AssertTrue($"Expected the blank-poster variant, got: {html}");
		html.Contains("<iframe").AssertFalse($"The player must not load with the page, got: {html}");
	}

	[TestMethod]
	public void Video_VkVideo_KeepsItsEmbedParameters()
	{
		// A VK embed carries its identity in the query string, so the url must survive untouched.
		var url = "https://vk.com/video_ext.php?oid=-1234&id=567&hash=abc";
		var html = ToHtml($"::iframe{{url={url}}}", allowHtml: true);

		html.Contains("class=\"ss-md-video").AssertTrue($"Expected the video card, got: {html}");
		html.Contains("oid=-1234").AssertTrue($"Expected the embed parameters, got: {html}");
		html.Contains("hash=abc").AssertTrue($"Expected the embed parameters, got: {html}");
	}

	[TestMethod]
	[DataRow("https://www.google.com/maps/d/u/0/embed?mid=1H8T0dpt")]
	[DataRow("https://example.com/player")]
	public void Video_NonVideoEmbed_StaysAnIframe(string url)
	{
		// Only video hosts become a play card. A map or any other embedded page is content the reader is
		// meant to see in place, not something to hide behind a play button.
		var html = ToHtml($"::iframe{{url={url}}}", allowHtml: true);

		html.Contains("<iframe").AssertTrue($"Expected a plain iframe, got: {html}");
		html.Contains(url).AssertTrue($"Expected the source url, got: {html}");
		html.Contains("ss-md-video").AssertFalse($"A non-video embed must not become a poster card, got: {html}");
	}

	[TestMethod]
	public void Clean_StripsTheNewSectionMarkers()
	{
		// Plain-text excerpts (search, mail digests) must not leak the container syntax.
		var text = _formatter.Clean(":::cards\n### Backtesting\nAny data types.\n:::\n\n:::stats\n70+ | indicators\n:::");

		text.Contains(":::").AssertFalse($"Container markers must be stripped, got: {text}");
		text.Contains("Backtesting").AssertTrue($"Content must survive, got: {text}");
	}

	[TestMethod]
	public void EntityReferenceUser()
	{
		var html = ToHtml("Hello @user(123)");
		html.Contains("href=\"/users/john/\"").AssertTrue($"Expected friendly user URL, got: {html}");
		html.Contains("John Smith").AssertTrue($"Expected user display name, got: {html}");
		html.Contains("title=\"Active community member\"").AssertTrue($"Expected user description in title, got: {html}");
	}

	[TestMethod]
	public void EntityReferenceProduct()
	{
		var html = ToHtml("Check @product(456)");
		html.Contains("href=\"/store/shell/\"").AssertTrue($"Expected friendly product URL, got: {html}");
		html.Contains("S#.Shell").AssertTrue($"Expected product name, got: {html}");
		html.Contains("title=\"Ready-made trading robot framework\"").AssertTrue($"Expected product description in title, got: {html}");
	}

	[TestMethod]
	public void EntityReferenceTopic()
	{
		var html = ToHtml("See @topic(789)");
		html.Contains("href=\"/forum/getting-started/\"").AssertTrue($"Expected friendly topic URL, got: {html}");
		html.Contains("Getting Started Guide").AssertTrue($"Expected topic name, got: {html}");
	}

	[TestMethod]
	public void RoleBlockAuthorized()
	{
		var html = ToHtml("@role(1){secret content}");
		html.Contains("secret content").AssertTrue();
	}

	[TestMethod]
	public void RoleBlockUnauthorized()
	{
		var html = ToHtml("@role(999){hidden}");
		html.Contains("hidden").AssertFalse();
	}

	[TestMethod]
	public void RoleBlock_MultilineContent_NoCarriageReturnLeak()
	{
		// The role block content is captured verbatim from the source; with CRLF input it must not
		// leak a raw \r into the rendered HTML (the rest of the document is normalized to \n).
		var html = ToHtml("@role(1){line one\r\n\r\nline two}");
		html.Contains("\r").AssertFalse($"CR leaked into output: {html}");
		html.Contains("line one").AssertTrue();
		html.Contains("line two").AssertTrue();
	}

	[TestMethod]
	public void ActivateRule_Authorized_KeepsContent()
	{
		const string text = "normal text @role(1){secret}";
		var roleIds = _formatter.CollectInlineRoleIds(text);
		var roles = roleIds.ToDictionary(id => id, id => id == 1);
		var result = _formatter.ActivateRule(text, roles);
		result.Contains("secret").AssertTrue();
		result.Contains("normal text").AssertTrue();
	}

	[TestMethod]
	public void ActivateRule_Unauthorized_RemovesContent()
	{
		const string text = "normal text @role(999){hidden}";
		var roleIds = _formatter.CollectInlineRoleIds(text);
		var roles = roleIds.ToDictionary(id => id, id => id == 1);
		var result = _formatter.ActivateRule(text, roles);
		result.Contains("hidden").AssertFalse();
		result.Contains("normal text").AssertTrue();
	}

	[TestMethod]
	public void ActivateRule_PreservesRawMarkdown()
	{
		const string text = "**bold** @role(1){secret}";
		var roleIds = _formatter.CollectInlineRoleIds(text);
		var roles = roleIds.ToDictionary(id => id, id => id == 1);
		var result = _formatter.ActivateRule(text, roles);
		// Should NOT convert to HTML — must stay as raw markdown
		result.Contains("**bold**").AssertTrue($"Expected raw markdown preserved, got: {result}");
		result.Contains("<strong>").AssertFalse($"Should not contain HTML tags, got: {result}");
		result.Contains("secret").AssertTrue();
	}

	[TestMethod]
	public void ActivateRule_NoRoles_ReturnsUnchanged()
	{
		const string input = "**bold** text with no roles";
		var result = _formatter.ActivateRule(input, []);
		result.AreEqual(input);
	}

	[TestMethod]
	public void ActivateRule_MultipleRoles_Mixed()
	{
		const string text = "start @role(1){visible} middle @role(999){hidden} end";
		var roleIds = _formatter.CollectInlineRoleIds(text);
		var roles = roleIds.ToDictionary(id => id, id => id == 1);
		var result = _formatter.ActivateRule(text, roles);
		result.Contains("visible").AssertTrue();
		result.Contains("hidden").AssertFalse();
		result.Contains("start").AssertTrue();
		result.Contains("middle").AssertTrue();
		result.Contains("end").AssertTrue();
	}

	// A role block can wrap a whole multi-block section: an ATX heading carrying a {#anchor}
	// plus a pipe table, with the closing brace only at the very end. The content therefore holds
	// balanced { } pairs ({#anchor}, directive attributes), so the gate must match the closing brace
	// by depth, not stop at the first "}". An unauthorized viewer must not receive any of it.
	private const string _multiBlockRoleSection =
		"intro\n\n@role({0}){{\n## Extended {{#extended}}\n\n| Tag | Desc |\n| --- | --- |\n| page | p |\n}}\n\nouter";

	[TestMethod]
	public void ActivateRule_Unauthorized_RemovesMultiBlockSection()
	{
		var text = string.Format(_multiBlockRoleSection, 999);
		var result = _formatter.ActivateRule(text, new Dictionary<long, bool> { [999] = false });
		result.Contains("Extended").AssertFalse($"heading leaked: {result}");
		result.Contains("Tag").AssertFalse($"table header leaked: {result}");
		result.Contains("page").AssertFalse($"table body leaked: {result}");
		result.Contains("intro").AssertTrue();
		result.Contains("outer").AssertTrue();
	}

	[TestMethod]
	public void ActivateRule_Authorized_KeepsMultiBlockSection()
	{
		var text = string.Format(_multiBlockRoleSection, 1);
		var result = _formatter.ActivateRule(text, new Dictionary<long, bool> { [1] = true });
		result.Contains("## Extended {#extended}").AssertTrue($"heading dropped: {result}");
		result.Contains("| Tag | Desc |").AssertTrue($"table dropped: {result}");
		result.Contains("@role").AssertFalse($"wrapper not unwrapped: {result}");
		result.Contains("intro").AssertTrue();
		result.Contains("outer").AssertTrue();
	}

	[TestMethod]
	public void Video()
	{
		var html = ToHtml("@vss(42)");
		html.Contains("<video").AssertTrue();
		html.Contains("/video/42").AssertTrue();
	}

	[TestMethod]
	public void Video_NotReady_ShowsTheReasonInsteadOfAPlayer()
	{
		// A video still being processed has no address to play. The caller says why in the reader's language --
		// only it knows the language -- and the renderer decides how that reads on a page. Rendering nothing
		// would leave the reader wondering whether the author forgot the video.
		var html = ToHtml($"@vss({_pendingVideo})");

		html.Contains("<video").AssertFalse($"There is nothing to play yet: {html}");
		html.Contains("Video is being processed").AssertTrue($"Expected the caller's reason, got: {html}");
	}

	[TestMethod]
	public void CleanText()
	{
		var plain = _formatter.Clean("**bold** and *italic*");
		plain.Contains("**").AssertFalse();
		plain.Contains("*").AssertFalse();
		plain.Contains("bold").AssertTrue();
		plain.Contains("italic").AssertTrue();
	}

	[TestMethod]
	public void CleanText_StripsInlineHtml()
	{
		var plain = _formatter.Clean("<span style=\"font-size:36pt\">S#.Designer</span> - **free** designer");
		plain.Contains("<span").AssertFalse();
		plain.Contains("</span>").AssertFalse();
		plain.Contains("S#.Designer").AssertTrue();
		plain.Contains("free").AssertTrue();
		plain.Contains("**").AssertFalse();
	}

	[TestMethod]
	public void CleanText_StripsDiv()
	{
		var plain = _formatter.Clean("<div align=\"center\">\n\n<span style=\"color:green\">Welcome!</span>\n\n</div>");
		plain.Contains("<div").AssertFalse();
		plain.Contains("<span").AssertFalse();
		plain.Contains("Welcome!").AssertTrue();
	}

	[TestMethod]
	public void CleanText_StripsIframe()
	{
		var plain = _formatter.Clean("<iframe width=\"640\" src=\"https://youtube.com/embed/abc\"></iframe> Some text");
		plain.Contains("<iframe").AssertFalse();
		plain.Contains("Some text").AssertTrue();
	}

	[TestMethod]
	public void CleanText_MixedMarkdownAndHtml()
	{
		var plain = _formatter.Clean("[<span style=\"color:red\">S#.Edu</span>](http://stocksharp.com/edu/)");
		plain.Contains("<span").AssertFalse();
		plain.Contains("</span>").AssertFalse();
		plain.Contains("[").AssertFalse();
		plain.Contains("S#.Edu").AssertTrue();
	}

	[TestMethod]
	public void CleanText_DecodesHtmlEntities()
	{
		var plain = _formatter.Clean("Text with &amp; and &lt;tag&gt; entities");
		plain.Contains("&amp;").AssertFalse();
		plain.Contains("&lt;").AssertFalse();
		plain.Contains("&gt;").AssertFalse();
	}

	[TestMethod]
	public void FindPictureById()
	{
		var fileId = _formatter.FindPicture("![alt](123)");
		fileId.AreEqual(123L);
	}

	[TestMethod]
	public void FindPictureByUrl()
	{
		var fileId = _formatter.FindPicture("![image](/file/456/image.png)");
		fileId.AreEqual(456L);
	}

	[TestMethod]
	public void FindPictureNone()
	{
		var fileId = _formatter.FindPicture("no images here");
		fileId.AssertNull();
	}

	[TestMethod]
	public void EmptyText()
	{
		var html = ToHtml("");
		html.AreEqual("");
	}

	[TestMethod]
	public void SpoilerBlock()
	{
		var html = ToHtml(":::spoiler Click me\nhidden content\n:::");
		// Markdig renders custom containers, our formatter converts them to details/summary
		html.Contains("hidden content").AssertTrue($"Expected 'hidden content' in: {html}");
	}

	[TestMethod]
	public void ComplexMessage()
	{
		var md = """
			## StockSharp Trading Platform

			Welcome to **StockSharp** — the *premier* ~~old~~ **modern** trading platform.
			Check out our product @product(9) and visit @topic(12374) for details.

			### Getting Started

			First, contact @user(1) or read @page(239) for instructions.
			See also @message(51317) for common questions and @file(122179) for downloads.

			Here is some `inline code` and a [documentation link](https://doc.stocksharp.com).

			```csharp
			// Create connector
			var connector = new Connector();
			connector.Connect();

			// Subscribe to trades
			connector.NewTrade += trade =>
			{
			    Console.WriteLine($"Trade: {trade.Price}");
			};
			```

			> **Important:** Always test your strategies in paper trading mode before going live.
			> This will save you from unexpected losses.

			### Supported Features

			| Feature | Status | Notes |
			|---------|--------|-------|
			| Real-time data | Active | Via connectors |
			| Backtesting | Active | Historical data |
			| Paper trading | Active | Risk-free |
			| Live trading | Active | Use with caution |

			**Unordered list:**

			- Market orders
			- Limit orders
			- Stop orders
			- Iceberg orders

			**Ordered list:**

			1. Install S#.Designer
			2. Configure connector
			3. Create strategy
			4. Run backtest

			---

			### Media Content

			Here is an image: ![Trading Chart](122179)

			And a video tutorial: @vss(42)

			### Access Control

			@role(1){This section is visible to authorized users only.

			Admin panel: [Settings](/admin/settings)}

			@role(999){This content is hidden from regular users.}

			:::spoiler Advanced Configuration
			For advanced users: modify the `appsettings.json` file
			to customize connector parameters.
			:::

			*Last updated by @user(1). For questions visit @topic(12374).*
			""";

		const string expected =
			"<h2 id=\"stocksharp-trading-platform\">StockSharp Trading Platform</h2>\n" +
			"<p>Welcome to <strong>StockSharp</strong> — the <em>premier</em> <del>old</del> <strong>modern</strong> trading platform.\n" +
			"Check out our product <a href=\"/store/designer/\" title=\"Algorithmic trading strategy designer\">S#.Designer</a> and visit <a href=\"/forum/shell-overview/\" title=\"Overview of S#.Shell features\">S#.Shell Overview</a> for details.</p>\n" +
			"<h3 id=\"getting-started\">Getting Started</h3>\n" +
			"<p>First, contact <a href=\"/users/stocksharp/\" title=\"Official StockSharp account\">StockSharp</a> or read <a href=\"/documentation/quick-start/\" title=\"Quick start documentation page\">Quick Start</a> for instructions.\n" +
			"See also <a href=\"/forum/getting-started/faq/#51317\" title=\"Frequently asked questions\">FAQ</a> for common questions and <a href=\"/file/122179/file.png\" title=\"File122179\">File122179</a> for downloads.</p>\n" +
			"<p>Here is some <code>inline code</code> and a <a href=\"https://doc.stocksharp.com\">documentation link</a>.</p>\n" +
			"<pre><code class=\"language-csharp\">// Create connector\n" +
			"var connector = new Connector();\n" +
			"connector.Connect();\n" +
			"\n" +
			"// Subscribe to trades\n" +
			"connector.NewTrade += trade =&gt;\n" +
			"{\n" +
			"    Console.WriteLine($&quot;Trade: {trade.Price}&quot;);\n" +
			"};\n" +
			"</code></pre>\n" +
			"<blockquote>\n" +
			"<p><strong>Important:</strong> Always test your strategies in paper trading mode before going live.\n" +
			"This will save you from unexpected losses.</p>\n" +
			"</blockquote>\n" +
			"<h3 id=\"supported-features\">Supported Features</h3>\n" +
			"<table>\n" +
			"<thead>\n" +
			"<tr>\n" +
			"<th>Feature</th>\n" +
			"<th>Status</th>\n" +
			"<th>Notes</th>\n" +
			"</tr>\n" +
			"</thead>\n" +
			"<tbody>\n" +
			"<tr>\n" +
			"<td>Real-time data</td>\n" +
			"<td>Active</td>\n" +
			"<td>Via connectors</td>\n" +
			"</tr>\n" +
			"<tr>\n" +
			"<td>Backtesting</td>\n" +
			"<td>Active</td>\n" +
			"<td>Historical data</td>\n" +
			"</tr>\n" +
			"<tr>\n" +
			"<td>Paper trading</td>\n" +
			"<td>Active</td>\n" +
			"<td>Risk-free</td>\n" +
			"</tr>\n" +
			"<tr>\n" +
			"<td>Live trading</td>\n" +
			"<td>Active</td>\n" +
			"<td>Use with caution</td>\n" +
			"</tr>\n" +
			"</tbody>\n" +
			"</table>\n" +
			"<p><strong>Unordered list:</strong></p>\n" +
			"<ul>\n" +
			"<li>Market orders</li>\n" +
			"<li>Limit orders</li>\n" +
			"<li>Stop orders</li>\n" +
			"<li>Iceberg orders</li>\n" +
			"</ul>\n" +
			"<p><strong>Ordered list:</strong></p>\n" +
			"<ol>\n" +
			"<li>Install S#.Designer</li>\n" +
			"<li>Configure connector</li>\n" +
			"<li>Create strategy</li>\n" +
			"<li>Run backtest</li>\n" +
			"</ol>\n" +
			"<hr />\n" +
			"<h3 id=\"media-content\">Media Content</h3>\n" +
			"<p>Here is an image: <img src=\"/file/122179/file.png\" alt=\"Trading Chart\" /></p>\n" +
			"<p>And a video tutorial: <video src=\"/video/42\"></video></p>\n" +
			"<h3 id=\"access-control\">Access Control</h3>\n" +
			"<p>This section is visible to authorized users only.\n" +
			"\n" +
			"Admin panel: [Settings](/admin/settings)</p>\n" +
			"<p>Admin panel: <a href=\"/admin/settings\">Settings</a>}</p>\n" +
			"<p></p>\n" +
			"<div class=\"spoiler\"><p>For advanced users: modify the <code>appsettings.json</code> file\n" +
			"to customize connector parameters.</p>\n" +
			"</div>\n" +
			"<p><em>Last updated by <a href=\"/users/stocksharp/\" title=\"Official StockSharp account\">StockSharp</a>. For questions visit <a href=\"/forum/shell-overview/\" title=\"Overview of S#.Shell features\">S#.Shell Overview</a>.</em></p>\n";

		var html = ToHtml(md);

		html.AreEqual(expected);
	}

	[TestMethod]
	public void ImageFileId_ResolvesToUrl()
	{
		var html = ToHtml("![Источники](103306)");
		html.Contains("src=\"/file/103306/file.png\"").AssertTrue($"Expected resolved file URL, got: {html}");
		html.Contains("alt=\"Источники\"").AssertTrue($"Expected alt text, got: {html}");
	}

	[TestMethod]
	public void ImageFileId_NoAlt_ResolvesToUrl()
	{
		var html = ToHtml("![](12345)");
		html.Contains("src=\"/file/12345/file.png\"").AssertTrue($"Expected resolved file URL, got: {html}");
	}

	[TestMethod]
	public void ImageUrl_NotChanged()
	{
		var html = ToHtml("![pic](https://example.com/img.png)");
		html.Contains("src=\"https://example.com/img.png\"").AssertTrue($"Expected original URL, got: {html}");
	}

	[TestMethod]
	public void RawHtmlImg_NumericSrc_ResolvesToUrl()
	{
		// Raw <img> is only honoured for trusted authors (allowHtml); for untrusted content it is escaped.
		var html = ToHtml("<img src=\"103306\">", allowHtml: true);
		html.Contains("src=\"103306\"").AssertFalse($"Raw numeric src should be resolved, got: {html}");
		html.Contains("src=\"/file/103306/file.png\"").AssertTrue($"Expected resolved file URL, got: {html}");
	}

	[TestMethod]
	public void RawHtmlImg_NumericSrc_WithAttrs_ResolvesToUrl()
	{
		var html = ToHtml("<img class=\"screenshot\" src=\"103306\" alt=\"test\">", allowHtml: true);
		html.Contains("src=\"103306\"").AssertFalse($"Raw numeric src should be resolved, got: {html}");
		html.Contains("src=\"/file/103306/file.png\"").AssertTrue($"Expected resolved file URL, got: {html}");
	}

	[TestMethod]
	public void StyledInline_NestedImage_ResolvesFileId()
	{
		var html = ToHtml(":[![Sources](103306)]{float=left}");
		html.Contains("src=\"103306\"").AssertFalse($"Styled inline image should have resolved src, got: {html}");
		html.Contains("/file/103306/file.png").AssertTrue($"Expected resolved file URL, got: {html}");
	}

	[TestMethod]
	public void StyledInline_NestedEntityRef_Resolves()
	{
		var html = ToHtml(":[Check @product(9) here]{color=red}");
		html.Contains("href=\"/store/designer/\"").AssertTrue($"Expected entity ref resolved inside styled inline, got: {html}");
		html.Contains("S#.Designer").AssertTrue($"Expected product name, got: {html}");
	}

	[TestMethod]
	public void Link_NumericHref_ResolvesToFileUrl()
	{
		var html = ToHtml("[questionnaire](103306)");
		html.Contains("href=\"103306\"").AssertFalse($"Numeric href should be resolved, got: {html}");
		html.Contains("href=\"/file/103306/file.png\"").AssertTrue($"Expected resolved file URL in href, got: {html}");
	}

	[TestMethod]
	public void MathInline_RendersToSpan()
	{
		var html = ToHtml("The formula $E = mc^2$ is famous.");
		html.Contains("class=\"math\"").AssertTrue($"Expected math class in: {html}");
		html.Contains("E = mc^2").AssertTrue($"Expected math content in: {html}");
	}

	[TestMethod]
	public void MathBlock_RendersToDiv()
	{
		var html = ToHtml("$$\n\\int_0^1 f(x) dx\n$$");
		html.Contains("class=\"math\"").AssertTrue($"Expected math class in: {html}");
		html.Contains("\\int_0^1 f(x) dx").AssertTrue($"Expected math content in: {html}");
	}

	[TestMethod]
	public void MermaidDiagram_RendersToDiv()
	{
		var html = ToHtml("```mermaid\ngraph TD\nA-->B\n```");
		html.Contains("class=\"mermaid\"").AssertTrue($"Expected mermaid class in: {html}");
		html.Contains("graph TD").AssertTrue($"Expected mermaid content in: {html}");
	}

	// --- Designer diagram by reference: @diagram(file id or URL) ---

	[TestMethod]
	public void DiagramRef_Unresolved_KeepsTheAuthorsToken()
	{
		// Nothing could resolve this reference. The page then says so by keeping what the author wrote, the
		// way an unavailable counter does: rendering nothing leaves a hole nobody can explain -- which is how
		// a documented "@diagram(URL)" turned into an empty table cell -- and leaking the internal
		// placeholder is worse still.
		var html = ToHtml("@diagram(122179)");
		html.Contains("{{diagram").AssertFalse($"The internal placeholder leaked: {html}");
		html.Contains("@diagram(122179)").AssertTrue($"Expected the author's token back, got: {html}");
	}

	[TestMethod]
	public void DiagramRef_Resolved_BuildsTheHostFromAnAddressAlone()
	{
		// What the caller resolves is an address, not markup. The renderer is what decides that an HTML page
		// shows a diagram through a host div the browser script fills in; a desktop renderer reads the same
		// address and draws the schema itself. Handing back ready-made HTML would force every other renderer
		// to either parse it or ignore it.
		var html = ToHtml($"@diagram({_knownDiagram})");

		html.Contains("class=\"ss-diagram-host\"").AssertTrue($"Expected the diagram host, got: {html}");
		html.Contains($"data-diagram-src=\"https://stocksharp.com/file/{_knownDiagram}/schema.json\"")
			.AssertTrue($"Expected the resolved address on the host, got: {html}");
		html.Contains($"@diagram({_knownDiagram})").AssertFalse($"The token should be gone once resolved: {html}");
	}

	[TestMethod]
	public void DiagramRef_InCodeSpan_StaysLiteral()
	{
		// Writing about the markup has to be possible: inside a code span the reference is text, not a
		// diagram, so a help page can quote it.
		var html = ToHtml("`@diagram(122179)`");
		html.Contains("<code>@diagram(122179)</code>").AssertTrue($"Expected a literal code span, got: {html}");
	}

	// --- Inline Designer diagram: ```diagram fenced block with the schema JSON embedded ---

	private const string _diagramSchema =
		"{\"Content\":{\"Value\":{\"Scheme\":{\"Model\":{\"Nodes\":[{\"Key\":\"n1\",\"TypeId\":\"Security\"}],\"Links\":[]}}}}}";

	[TestMethod]
	public void InlineDiagram_Trusted_RendersHostWithEmbeddedJson()
	{
		// A ```diagram fenced block lets a trusted author (content manager, allowHtml) paste the Designer
		// schema JSON straight into the message; it renders a diagram host carrying that JSON for the client.
		var html = ToHtml("```diagram\n" + _diagramSchema + "\n```", allowHtml: true);
		html.Contains("class=\"ss-diagram-host\"").AssertTrue($"Expected diagram host, got: {html}");
		html.Contains("application/json").AssertTrue($"Expected embedded JSON payload, got: {html}");
		html.Contains("\"Nodes\"").AssertTrue($"Expected the schema JSON preserved, got: {html}");
	}

	[TestMethod]
	public void InlineDiagram_Untrusted_RendersHostWithEmbeddedJson()
	{
		// A schema is a diagram, not markup: pasting one is no more dangerous than posting a picture, so a
		// forum or blog author gets the same live host a content manager does. What keeps that safe is the
		// payload escaping below, which holds for either author.
		var html = ToHtml("```diagram\n" + _diagramSchema + "\n```", allowHtml: false);
		html.Contains("class=\"ss-diagram-host\"").AssertTrue($"Expected diagram host, got: {html}");
		html.Contains("application/json").AssertTrue($"Expected embedded JSON payload, got: {html}");
		html.Contains("\"Nodes\"").AssertTrue($"Expected the schema JSON preserved, got: {html}");
	}

	[TestMethod]
	public void InlineDiagram_EscapesScriptClose()
	{
		// JSON whose content contains a </script> sequence must not break out of the embedding script tag.
		var html = ToHtml("```diagram\n{\"Content\":{\"Value\":{\"Scheme\":{\"Model\":{\"Nodes\":[{\"Key\":\"</script>\"}]}}}}}\n```", allowHtml: true);
		html.Contains("class=\"ss-diagram-host\"").AssertTrue($"Expected diagram host, got: {html}");
		// The only literal </script> in the output must be the real closing tag (followed by </div>), never
		// the one from the JSON payload (which would be followed by a quote).
		html.Contains("</script>\"").AssertFalse($"Raw </script> from the payload must be escaped, got: {html}");
	}

	[TestMethod]
	public void InlineDiagram_Untrusted_EscapesScriptClose()
	{
		// The same guard where it matters most: the author whose input nobody vetted. This is what makes the
		// block safe to hand to everyone rather than the pipeline it happens to be registered in.
		var html = ToHtml("```diagram\n{\"Content\":{\"Value\":{\"Scheme\":{\"Model\":{\"Nodes\":[{\"Key\":\"</script>\"}]}}}}}\n```", allowHtml: false);
		html.Contains("class=\"ss-diagram-host\"").AssertTrue($"Expected diagram host, got: {html}");
		html.Contains("</script>\"").AssertFalse($"Raw </script> from the payload must be escaped, got: {html}");
	}

	[TestMethod]
	public void GenericAttributes_HeadingCenterAlign()
	{
		var html = ToHtml("## Centered {style=\"text-align:center\"}");
		html.Contains("text-align:center").AssertTrue($"Expected center alignment on heading, got: {html}");
	}

	[TestMethod]
	public void GenericAttributes_InlineCenterAlign()
	{
		var html = ToHtml("[centered text]{style=\"text-align:center\"}");
		html.Contains("text-align:center").AssertTrue($"Expected center alignment, got: {html}");
	}

	[TestMethod]
	public void GenericAttributes_InlineColor()
	{
		var html = ToHtml("[red text]{style=\"color:red\"}");
		html.Contains("color:red").AssertTrue($"Expected color style, got: {html}");
	}

	[TestMethod]
	public void GenericAttributes_InlineFontSize()
	{
		var html = ToHtml("[big text]{style=\"font-size:24px\"}");
		html.Contains("font-size:24px").AssertTrue($"Expected font-size, got: {html}");
	}

	[TestMethod]
	public void InsertedText_Underline()
	{
		var html = ToHtml("++underlined++");
		html.Contains("<ins>underlined</ins>").AssertTrue($"Expected <ins> tag, got: {html}");
	}

	[TestMethod]
	public void MarkedText_Highlight()
	{
		var html = ToHtml("==highlighted==");
		html.Contains("<mark>highlighted</mark>").AssertTrue($"Expected <mark> tag, got: {html}");
	}

	// --- Styled inline: :[text]{color=red size=24pt font=Arial} ---

	[TestMethod]
	public void StyledInline_Color()
	{
		var html = ToHtml("normal :[red text]{color=red} normal");
		html.Contains("style=").AssertTrue($"Expected style attribute, got: {html}");
		html.Contains("color:red").AssertTrue($"Expected color:red in style, got: {html}");
		html.Contains("red text").AssertTrue($"Expected text content, got: {html}");
	}

	[TestMethod]
	public void StyledInline_FontSize()
	{
		var html = ToHtml(":[big text]{size=36pt}");
		html.Contains("font-size:36pt").AssertTrue($"Expected font-size in style, got: {html}");
		html.Contains("big text").AssertTrue($"Expected text content, got: {html}");
	}

	[TestMethod]
	public void StyledInline_FontFamily()
	{
		var html = ToHtml(":[custom font]{font=Arial}");
		html.Contains("font-family:Arial").AssertTrue($"Expected font-family in style, got: {html}");
		html.Contains("custom font").AssertTrue($"Expected text content, got: {html}");
	}

	[TestMethod]
	public void StyledInline_Combined()
	{
		var html = ToHtml(":[styled]{color=blue size=24pt font=Verdana}");
		html.Contains("color:blue").AssertTrue($"Expected color:blue, got: {html}");
		html.Contains("font-size:24pt").AssertTrue($"Expected font-size:24pt, got: {html}");
		html.Contains("font-family:Verdana").AssertTrue($"Expected font-family:Verdana, got: {html}");
		html.Contains("styled").AssertTrue($"Expected text content, got: {html}");
	}

	[TestMethod]
	public void StyledInline_NestedMarkdown()
	{
		var html = ToHtml(":[**bold** and *italic*]{color=red}");
		html.Contains("color:red").AssertTrue($"Expected color:red, got: {html}");
		html.Contains("<strong>bold</strong>").AssertTrue($"Expected bold inside styled, got: {html}");
		html.Contains("<em>italic</em>").AssertTrue($"Expected italic inside styled, got: {html}");
	}

	[TestMethod]
	public void StyledInline_WithEntityRef()
	{
		var html = ToHtml(":[Check @product(9)]{color=green}");
		html.Contains("color:green").AssertTrue($"Expected color:green, got: {html}");
		html.Contains("S#.Designer").AssertTrue($"Expected resolved product name, got: {html}");
	}

	[TestMethod]
	public void CleanText_StyledInline()
	{
		var plain = _formatter.Clean(":[styled text]{color=red}");
		plain.Contains("styled text").AssertTrue($"Expected text content, got: {plain}");
		plain.Contains("color=").AssertFalse($"Should not contain attribute, got: {plain}");
		plain.Contains("{").AssertFalse($"Should not contain braces, got: {plain}");
	}

	[TestMethod]
	public void CleanText_StyledInline_Combined()
	{
		var plain = _formatter.Clean("normal :[big red]{color=red size=24pt} text");
		plain.Contains("big red").AssertTrue($"Expected text content, got: {plain}");
		plain.Contains("normal").AssertTrue($"Expected surrounding text, got: {plain}");
		plain.Contains("text").AssertTrue($"Expected surrounding text, got: {plain}");
		plain.Contains("color").AssertFalse($"Should not contain attributes, got: {plain}");
	}

	// --- Align block: :::center / :::left / :::right ---

	[TestMethod]
	public void AlignBlock_Center()
	{
		var html = ToHtml(":::center\ncentered content\n:::");
		html.Contains("text-align:center").AssertTrue($"Expected text-align:center, got: {html}");
		html.Contains("centered content").AssertTrue($"Expected content, got: {html}");
	}

	[TestMethod]
	public void AlignBlock_Right()
	{
		var html = ToHtml(":::right\nright-aligned\n:::");
		html.Contains("text-align:right").AssertTrue($"Expected text-align:right, got: {html}");
		html.Contains("right-aligned").AssertTrue($"Expected content, got: {html}");
	}

	[TestMethod]
	public void AlignBlock_Left()
	{
		var html = ToHtml(":::left\nleft-aligned\n:::");
		html.Contains("text-align:left").AssertTrue($"Expected text-align:left, got: {html}");
		html.Contains("left-aligned").AssertTrue($"Expected content, got: {html}");
	}

	[TestMethod]
	public void CleanText_AlignBlock()
	{
		var plain = _formatter.Clean(":::center\ncentered content\n:::");
		plain.Contains("centered content").AssertTrue($"Expected content, got: {plain}");
		plain.Contains(":::").AssertFalse($"Should not contain block markers, got: {plain}");
	}

	// --- Iframe block: ::iframe{url=... width=640 height=390} ---

	[TestMethod]
	public void IframeBlock_YouTube()
	{
		// YouTube renders as a compact poster card: the player itself is loaded by the site script when the
		// reader clicks it, so the page carries one thumbnail instead of an embedded player. An authored
		// width caps the card.
		var html = ToHtml("::iframe{url=https://www.youtube.com/embed/abc123 width=640 height=390}");
		html.Contains("class=\"ss-md-video\"").AssertTrue($"Expected the poster card, got: {html}");
		html.Contains("data-video=\"https://www.youtube.com/embed/abc123\"").AssertTrue($"Expected the player url, got: {html}");
		html.Contains("img.youtube.com/vi/abc123/hqdefault.jpg").AssertTrue($"Expected the poster image, got: {html}");
		html.Contains("max-width:640px").AssertTrue($"Expected the authored width to cap the card, got: {html}");
		html.Contains("<iframe").AssertFalse($"The player must not load with the page, got: {html}");
	}

	[TestMethod]
	public void IframeBlock_Vimeo()
	{
		// Vimeo is a video host, so it renders as a play card like YouTube; it just has no thumbnail url.
		var html = ToHtml("::iframe{url=https://player.vimeo.com/video/12345 width=640 height=390}");
		html.Contains("class=\"ss-md-video").AssertTrue($"Expected the video card, got: {html}");
		html.Contains("data-video=\"https://player.vimeo.com/video/12345\"").AssertTrue($"Expected the player url, got: {html}");
		html.Contains("max-width:640px").AssertTrue($"Expected the authored width to cap the card, got: {html}");
		html.Contains("<iframe").AssertFalse($"The player must not load with the page, got: {html}");
	}

	[TestMethod]
	public void IframeBlock_DefaultSize()
	{
		var html = ToHtml("::iframe{url=https://example.com/embed}");
		html.Contains("<iframe").AssertTrue($"Expected iframe tag, got: {html}");
		html.Contains("src=\"https://example.com/embed\"").AssertTrue($"Expected src, got: {html}");
	}

	[TestMethod]
	public void CleanText_IframeBlock()
	{
		var plain = _formatter.Clean("Some text\n\n::iframe{url=https://youtube.com/embed/abc width=640 height=390}\n\nmore text");
		plain.Contains("<iframe").AssertFalse($"Should not contain iframe tag, got: {plain}");
		plain.Contains("::iframe").AssertFalse($"Should not contain directive, got: {plain}");
		plain.Contains("Some text").AssertTrue($"Expected surrounding text, got: {plain}");
		plain.Contains("more text").AssertTrue($"Expected surrounding text, got: {plain}");
	}

	// --- Float inline: :[text]{float=left} ---

	[TestMethod]
	public void StyledInline_Float()
	{
		var html = ToHtml(":[floated image content]{float=left}");
		html.Contains("float:left").AssertTrue($"Expected float:left, got: {html}");
		html.Contains("floated image content").AssertTrue($"Expected content, got: {html}");
	}

	/// <summary>P#3 — raw HTML/JS in markdown source must not survive into rendered output. Fails
	/// today: the pipeline (UseAdvancedExtensions, no DisableHtml) passes &lt;script&gt; through.</summary>
	[TestMethod]
	public void P3_Markdown_RawScript_NotPassedThrough()
	{
		var formatter = new Md2HtmlFormatter();

		const string evil = "<script>alert('xss')</script>";
		var parsed = formatter.Parse(evil, allowHtml: false);
		var html = formatter.Render(parsed, new ResolvedMarkdownData());

		// Untrusted markdown must never render an executable script element.
		IsFalse(html.ContainsIgnoreCase("<script"), $"got: {html}");
	}

	// --- Site counters: @indicator_count and friends ---

	[TestMethod]
	public void SiteCounters_AreSubstituted()
	{
		var html = ToHtml("@indicator_count @connector_count @user_count @strategy_count @app_count");

		foreach (var expected in _counters.Values)
			html.Contains(expected).AssertTrue($"Expected {expected}, got: {html}");

		// The source token must be gone -- a number that stayed as markup is a number nobody reads.
		IsFalse(html.Contains("_count"), $"got: {html}");
	}

	[TestMethod]
	public void SiteCounter_InsideASentence()
	{
		var html = ToHtml("The platform supports @connector_count connectors today.");

		html.Contains($"supports {_counters[SiteCounters.Connectors]} connectors").AssertTrue($"got: {html}");
	}

	/// <summary>
	/// A counter that could not be obtained must not turn into a number: an invented "0 connectors" reads
	/// as a fact. The source token stays instead, which is visible to whoever edits the page.
	/// </summary>
	[TestMethod]
	public void SiteCounter_WithoutAValue_IsNotInvented()
	{
		const string text = "We support @connector_count connectors.";

		var parsed = _formatter.Parse(text, allowHtml: false);
		var html = _formatter.Render(parsed, new ResolvedMarkdownData());

		IsFalse(html.Contains(">0<") || html.Contains(" 0 "), $"a missing counter was rendered as zero: {html}");
		html.Contains("@connector_count").AssertTrue($"got: {html}");
	}

	// --- @product_name(id): the localized name, not a link ---

	[TestMethod]
	public void ProductName_IsTheNameAndNotALink()
	{
		var html = ToHtml("@product_name(9)");

		html.Contains("S#.Designer").AssertTrue($"got: {html}");
		IsFalse(html.Contains("<a "), $"the name must not be a link: {html}");
		IsFalse(html.Contains("/store/designer/"), $"the name must not carry the url: {html}");
	}

	[TestMethod]
	public void ProductName_AndProductLink_Coexist()
	{
		var html = ToHtml("Read about @product(9) in @product_name(456).");

		// The link keeps its href; the bare name next to it stays plain text.
		html.Contains("href=\"/store/designer/\"").AssertTrue($"got: {html}");
		html.Contains("S#.Shell").AssertTrue($"got: {html}");
		IsFalse(html.Contains("href=\"/store/shell/\""), $"got: {html}");
	}

	[TestMethod]
	public void ProductName_IsHtmlEscaped()
	{
		// A name is content, not markup: whatever the catalogue holds must not become live html.
		var parsed = _formatter.Parse("@product_name(777)", allowHtml: false);

		var html = _formatter.Render(parsed, new ResolvedMarkdownData
		{
			Entities = { [("product_name", 777L)] = (string.Empty, "<b>Bold</b> & co", string.Empty) },
		});

		IsFalse(html.Contains("<b>Bold</b>"), $"got: {html}");
		html.Contains("&amp; co").AssertTrue($"got: {html}");
	}
}
