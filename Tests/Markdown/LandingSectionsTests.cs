namespace Ecng.Tests.Markdown;

using System.Text.RegularExpressions;

using Ecng.Markdown;
using Ecng.Markdown.Extensions;

/// <summary>
/// The landing sections a host adds with <see cref="LandingExtension"/>: :::hero, :::index, :::pillar, :::tags and
/// :::section.
/// </summary>
[TestClass]
public class LandingSectionsTests : BaseTestClass
{
	private static readonly Md2HtmlFormatter _formatter = new([new LandingExtension()]);

	private const string _landing = """
		::::hero
		For teams

		# Build it once

		One platform for everything.

		[Start](#start) [Write to us](mailto:team@example.org)

		:::index Parts
		- [First](#first) The first part
		- [Second](#second) — the second part
		:::

		A note under the buttons.
		::::

		::::pillar {#first}
		![Main shot](https://cdn.example/main.png)

		First

		## The first part

		What it does.

		- One
		- Two

		:::tags
		- Alpha
		- *Beta — soon*
		:::
		::::

		::::pillar band flip {#second}
		![Wide](https://cdn.example/a.png)

		![Strip one](https://cdn.example/b.png) ![Strip two](https://cdn.example/c.png)

		## The second part
		::::

		:::section dashed narrow {#start}
		## Get started

		Pick a plan.

		[Go](#go)
		:::

		:::section center
		## Questions?

		[Write to us](mailto:team@example.org)
		:::
		""";

	private static string ToHtml(string text, bool allowHtml = true)
		=> _formatter.Render(_formatter.Parse(text, allowHtml), new());

	private static void Has(string html, string fragment)
		=> html.Contains(fragment).AssertTrue($"no {fragment} in: {html}");

	private static void InOrder(string html, params string[] fragments)
	{
		var at = -1;

		foreach (var fragment in fragments)
		{
			var next = html.IndexOf(fragment, at + 1, StringComparison.Ordinal);
			(next > at).AssertTrue($"{fragment} is missing or out of order in: {html}");
			at = next;
		}
	}

	[TestMethod]
	public void Hero_LaysOutTheOpening()
	{
		var html = ToHtml(_landing);

		InOrder(html,
			"<section class=\"ss-hero ss-hero--compact\"><div class=\"ss-wrap\">",
			"<span class=\"ss-eyebrow\">For teams</span>",
			"<h1 id=\"build-it-once\" class=\"ss-display ss-display--lg ss-hero-title\">Build it once</h1>",
			"<p class=\"ss-lede ss-hero-lede\">One platform for everything.</p>",
			"<div class=\"ss-hero-actions\"><a href=\"#start\" class=\"ss-cta ss-cta--solid no-underline\">Start <span>→</span></a><a href=\"mailto:team@example.org\" class=\"ss-cta no-underline\">Write to us</a></div>",
			"<nav class=\"ss-pillar-index\"",
			"<p class=\"ss-hero-note\">A note under the buttons.</p>",
			"</div></section>");
	}

	[TestMethod]
	public void Index_NumbersEveryPartAndTrimsTheSeparator()
	{
		var html = ToHtml(_landing);

		Regex.IsMatch(html, "<nav class=\"ss-pillar-index\"[^>]* aria-label=\"Parts\"").AssertTrue($"got: {html}");
		InOrder(html,
			"<a href=\"#first\" class=\"ss-pillar-index__item no-underline\"><span class=\"ss-pillar-index__num\">01</span><span class=\"ss-pillar-index__title\">First</span><span class=\"ss-pillar-index__sub\">The first part</span></a>",
			"<a href=\"#second\" class=\"ss-pillar-index__item no-underline\"><span class=\"ss-pillar-index__num\">02</span><span class=\"ss-pillar-index__title\">Second</span><span class=\"ss-pillar-index__sub\">the second part</span></a>",
			"</nav>");
	}

	[TestMethod]
	public void Pillar_PictureOnOneSideTextOnTheOther()
	{
		var html = ToHtml(_landing);

		InOrder(html,
			"<section id=\"first\" class=\"ss-pillar\"><div class=\"ss-wrap\"><div class=\"ss-pillar__grid\">",
			"<div class=\"ss-pillar__media\"><figure class=\"ss-detail-shot\"><img src=\"https://cdn.example/main.png\" alt=\"Main shot\" loading=\"lazy\" /></figure></div>",
			"<div class=\"ss-pillar__text\"><span class=\"ss-pillar__num\">01</span>",
			"<span class=\"ss-eyebrow\">First</span>",
			"<h2 id=\"the-first-part\" class=\"ss-display ss-display--md\">The first part</h2>",
			"<p class=\"ss-pillar__lede\">What it does.</p>",
			"<ul class=\"ss-feature-list ss-feature-list--cols ss-pillar__list\"><li>One</li><li>Two</li></ul>",
			"<div class=\"ss-tag-row\">",
			"</section>");
	}

	[TestMethod]
	public void Pillar_BandFlipAndTheStrip()
	{
		var html = ToHtml(_landing);

		InOrder(html,
			"<section id=\"second\" class=\"ss-pillar ss-pillar--band\"><div class=\"ss-wrap\"><div class=\"ss-pillar__grid ss-pillar__grid--flip\">",
			"<figure class=\"ss-detail-shot\"><img src=\"https://cdn.example/a.png\" alt=\"Wide\" loading=\"lazy\" /></figure>",
			"<div class=\"ss-pillar__strip\"><img src=\"https://cdn.example/b.png\" alt=\"Strip one\" loading=\"lazy\" /><img src=\"https://cdn.example/c.png\" alt=\"Strip two\" loading=\"lazy\" /></div>",
			"<span class=\"ss-pillar__num\">02</span>",
			"<h2 id=\"the-second-part\" class=\"ss-display ss-display--md\">The second part</h2>");
	}

	[TestMethod]
	public void Tags_AnItemInEmphasisIsNotThereYet()
	{
		var html = ToHtml(_landing);

		Has(html, "<div class=\"ss-tag-row\"><span class=\"ss-tag\">Alpha</span><span class=\"ss-tag ss-tag--soon\">Beta — soon</span></div>");
	}

	[TestMethod]
	public void Section_HeadLedeAndButtons()
	{
		var html = ToHtml(_landing);

		InOrder(html,
			"<section id=\"start\" class=\"ss-section ss-section--dashed\"><div class=\"ss-wrap ss-wrap--narrow\">",
			"<div class=\"ss-section-head\"><h2 id=\"get-started\" class=\"ss-display ss-display--md\">Get started</h2><p class=\"ss-lede\">Pick a plan.</p></div>",
			"<div class=\"ss-section-actions\"><a href=\"#go\" class=\"ss-cta ss-cta--solid no-underline\">Go</a></div>",
			"</div></section>",
			"<section class=\"ss-section ss-section--center\"><div class=\"ss-wrap\">",
			"<div class=\"ss-section-head\"><h2 id=\"questions\" class=\"ss-display ss-display--md\">Questions?</h2></div>",
			"<div class=\"ss-section-actions\"><a href=\"mailto:team@example.org\" class=\"ss-cta ss-cta--solid no-underline\">Write to us</a></div>");
	}

	[TestMethod]
	public void Pillar_DiagramFigureIsLabelledByItsCaption()
	{
		var html = ToHtml("::::pillar\n^^^\n```diagram\n{\"version\":1,\"nodes\":[]}\n```\n^^^ Order path\n\n## Core\n::::");

		InOrder(html, "<div class=\"ss-pillar__media\"><figure class=\"ss-arch\">", "</figure></div>");
		Regex.IsMatch(html, "<div class=\"ss-diagram-host[^\"]*ss-arch__diagram[^\"]*\"[^>]*>").AssertTrue($"got: {html}");
		Has(html, "data-diagram-kind=\"document\"");
		Has(html, "data-diagram-export=\"off\"");
		Has(html, "role=\"img\"");
		Has(html, "aria-label=\"Order path\"");
		Has(html, "<figcaption>Order path</figcaption>");
	}

	[TestMethod]
	public void Pillar_BareDiagramFenceHasNoCaption()
	{
		var html = ToHtml(":::pillar\n```diagram\n{\"version\":1,\"nodes\":[]}\n```\n\n## Core\n:::");

		Has(html, "<figure class=\"ss-arch\">");
		Has(html, "data-diagram-export=\"off\"");
		html.Contains("<figcaption").AssertFalse($"got: {html}");
		html.Contains("aria-label").AssertFalse($"got: {html}");
	}

	// The host's link policy (an away page, nofollow, a new tab) rewrites anchors that open with href, so a button
	// written any other way would let an untrusted text link out directly, javascript: included.
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void Links_OpenWithHref(bool allowHtml)
	{
		const string text = """
			:::hero
			# Title

			[Go](javascript:alert(1)) [Away](https://example.org)
			:::

			:::index Parts
			- [Part](https://example.org/part) sub
			:::

			:::section
			## Head

			[Out](https://example.org/out)
			:::
			""";

		var anchors = Regex.Matches(ToHtml(text, allowHtml), @"<a\b[^>]*>").Select(m => m.Value).ToArray();

		anchors.Length.AreEqual(4);

		foreach (var anchor in anchors)
			anchor.StartsWith("<a href=\"", StringComparison.Ordinal).AssertTrue(anchor);
	}

	[TestMethod]
	public void WithoutTheExtension_TheyArePlainContainers()
	{
		var plain = new Md2HtmlFormatter();
		var html = plain.Render(plain.Parse(":::hero\n# Title\n:::", true), new());

		Has(html, "<div class=\"hero\">");
		html.Contains("ss-hero").AssertFalse($"got: {html}");
	}

	[TestMethod]
	public void Clean_LeavesNoFence()
	{
		var plain = _formatter.Clean(_landing);

		plain.Contains(":::").AssertFalse($"got: {plain}");
		plain.Contains("Build it once").AssertTrue($"got: {plain}");
		plain.Contains("Pick a plan.").AssertTrue($"got: {plain}");
	}
}
