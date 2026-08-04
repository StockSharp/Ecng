namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Custom HTML renderer for <see cref="CustomContainer"/> that turns the ::: containers into the
/// page sections product pages are built from:
/// <list type="bullet">
/// <item>alignment: :::center, :::left, :::right;</item>
/// <item>:::feature-left / :::feature-right — a two-column media+text section (add the "alt" argument
/// for a tinted full-width band);</item>
/// <item>:::cards — a responsive card grid, one card per ### heading;</item>
/// <item>:::stats — a row of big numbers, one per "value | label" line;</item>
/// <item>:::cta — the links rendered as buttons, the first one primary;</item>
/// <item>:::steps — a numbered walkthrough, one step per ### heading;</item>
/// <item>:::quote — a testimonial whose trailing em-dash line becomes the attribution.</item>
/// </list>
/// Anything else falls back to the default container rendering (e.g. :::spoiler).
/// </summary>
public class SectionContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
	private const string _revealClass = "ss-md-reveal";
	private const string _splitClass = "ss-md-split";

	private static readonly HashSet<string> _alignments = new(StringComparer.OrdinalIgnoreCase) { "center", "left", "right" };

	// A paragraph that is nothing but a diagram reference (before or after the formatter turns it into a
	// placeholder) also fills the media half.
	private static readonly Regex _diagramOnly = new(@"^\s*(?:@diagram\([^)]+\)|\{\{diagram:[^}]+\}\})\s*$", RegexOptions.Compiled);

	protected override void Write(HtmlRenderer renderer, CustomContainer obj)
	{
		var info = obj.Info?.Trim();
		var args = obj.Arguments?.Trim();

		if (info.IsEmpty())
		{
			WriteDefault(renderer, obj, info);
			return;
		}

		if (info.EqualsIgnoreCase("feature-left") || info.EqualsIgnoreCase("feature-right"))
		{
			WriteFeature(renderer, obj, info.EqualsIgnoreCase("feature-right") ? "right" : "left", IsAlt(args));
			return;
		}

		if (info.EqualsIgnoreCase("cards"))
		{
			WriteCards(renderer, obj);
			return;
		}

		if (info.EqualsIgnoreCase("stats"))
		{
			WriteStats(renderer, obj);
			return;
		}

		if (info.EqualsIgnoreCase("cta"))
		{
			WriteCta(renderer, obj);
			return;
		}

		if (info.EqualsIgnoreCase("steps"))
		{
			WriteSteps(renderer, obj);
			return;
		}

		if (info.EqualsIgnoreCase("quote"))
		{
			WriteQuote(renderer, obj);
			return;
		}

		if (info.EqualsIgnoreCase("split"))
		{
			WriteSplit(renderer, obj);
			return;
		}

		if (_alignments.Contains(info))
		{
			renderer.Write($"<div style=\"text-align:{info}\">");
			renderer.WriteChildren(obj);
			renderer.Write("</div>");
			return;
		}

		WriteDefault(renderer, obj, info);
	}

	// Default rendering for other custom containers (e.g. :::spoiler).
	private static void WriteDefault(HtmlRenderer renderer, CustomContainer obj, string info)
	{
		renderer.Write("<div");

		if (!info.IsEmpty())
			renderer.Write($" class=\"{info}\"");

		renderer.Write(">");
		renderer.WriteChildren(obj);
		renderer.Write("</div>");
	}

	private static bool IsAlt(string args)
		=> !args.IsEmpty() && args.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(a => a.EqualsIgnoreCase("alt"));

	// Renders a :::feature-left / :::feature-right section. The image side is explicit in the syntax so
	// authors control the layout per section (no automatic mirroring). The first media block becomes the
	// media half; everything else becomes the text half. The .ss-md-reveal class is the hook the site script
	// animates into view on scroll.
	private static void WriteFeature(HtmlRenderer renderer, CustomContainer obj, string side, bool alt)
	{
		Block media = null;

		foreach (var child in obj)
		{
			if (IsMediaBlock(child))
			{
				media = child;
				break;
			}
		}

		renderer.EnsureLine();
		renderer.Write($"<section class=\"ss-md-feature ss-md-feature--{side}");

		if (alt)
			renderer.Write(" ss-md-feature--alt");

		renderer.Write($" {_revealClass}\">");

		renderer.Write("<div class=\"ss-md-feature__media\">");
		if (media is not null)
			renderer.Write(media);
		renderer.Write("</div>");

		renderer.Write("<div class=\"ss-md-feature__text\">");
		foreach (var child in obj)
		{
			if (!ReferenceEquals(child, media))
				renderer.Write(child);
		}
		renderer.Write("</div>");

		renderer.WriteLine("</section>");
	}

	// Renders :::cards — one card per ### heading, the blocks after it are the card body.
	private static void WriteCards(HtmlRenderer renderer, CustomContainer obj)
	{
		renderer.EnsureLine();
		renderer.Write($"<div class=\"ss-md-cards {_revealClass}\">");

		foreach (var (heading, body) in GroupByHeading(obj))
		{
			renderer.Write("<article class=\"ss-md-card\">");

			if (heading is not null)
			{
				renderer.Write("<h3 class=\"ss-md-card__title\">");
				renderer.WriteLeafInline(heading);
				renderer.Write("</h3>");
			}

			foreach (var block in body)
				renderer.Write(block);

			renderer.Write("</article>");
		}

		renderer.WriteLine("</div>");
	}

	// Renders :::steps — same grouping as the cards, plus the ordinal that makes the sequence explicit.
	private static void WriteSteps(HtmlRenderer renderer, CustomContainer obj)
	{
		renderer.EnsureLine();
		renderer.Write($"<div class=\"ss-md-steps {_revealClass}\">");

		var number = 0;

		foreach (var (heading, body) in GroupByHeading(obj))
		{
			number++;

			renderer.Write("<div class=\"ss-md-step\">");
			renderer.Write($"<div class=\"ss-md-step__num\">{number}</div>");
			renderer.Write("<div class=\"ss-md-step__body\">");

			if (heading is not null)
			{
				renderer.Write("<h3 class=\"ss-md-step__title\">");
				renderer.WriteLeafInline(heading);
				renderer.Write("</h3>");
			}

			foreach (var block in body)
				renderer.Write(block);

			renderer.Write("</div></div>");
		}

		renderer.WriteLine("</div>");
	}

	// Renders :::stats — one big number per "value | label" line.
	private static void WriteStats(HtmlRenderer renderer, CustomContainer obj)
	{
		renderer.EnsureLine();
		renderer.Write($"<div class=\"ss-md-stats {_revealClass}\">");

		foreach (var child in obj)
		{
			if (child is not ParagraphBlock { Inline: not null } paragraph)
				continue;

			foreach (var line in GetText(paragraph.Inline).Split('\n'))
			{
				var text = line.Trim();

				if (text.IsEmpty())
					continue;

				var separator = text.IndexOf('|');
				var value = separator < 0 ? text : text[..separator].Trim();
				var label = separator < 0 ? string.Empty : text[(separator + 1)..].Trim();

				renderer.Write("<div class=\"ss-md-stat\"><div class=\"ss-md-stat__value\">");
				renderer.WriteEscape(value);
				renderer.Write("</div><div class=\"ss-md-stat__label\">");
				renderer.WriteEscape(label);
				renderer.Write("</div></div>");
			}
		}

		renderer.WriteLine("</div>");
	}

	// Renders :::cta — every link becomes a button, the first one is the primary action.
	private static void WriteCta(HtmlRenderer renderer, CustomContainer obj)
	{
		renderer.EnsureLine();
		renderer.Write($"<div class=\"ss-md-cta {_revealClass}\">");

		var first = true;

		foreach (var link in obj.Descendants<LinkInline>().Where(l => !l.IsImage))
		{
			renderer.Write(first ? "<a class=\"ss-md-btn ss-md-btn--primary\" href=\"" : "<a class=\"ss-md-btn\" href=\"");
			renderer.WriteEscapeUrl(link.GetDynamicUrl?.Invoke() ?? link.Url);
			renderer.Write("\">");
			renderer.WriteEscape(GetText(link));
			renderer.Write("</a>");

			first = false;
		}

		renderer.WriteLine("</div>");
	}

	// Renders :::split — screenshots of the same screen stacked on top of each other, so a single frame
	// shows both looks at once (the light and the dark theme). The first image is the base and the ones
	// after it are overlays the stylesheet clips diagonally; each image is written as its own paragraph
	// and rendered through the normal pipeline, so file-id references resolve as everywhere else.
	private static void WriteSplit(HtmlRenderer renderer, CustomContainer obj)
	{
		renderer.EnsureLine();
		renderer.Write($"<div class=\"{_splitClass} {_revealClass}\">");

		var isBase = true;

		foreach (var child in obj)
		{
			renderer.Write($"<div class=\"{_splitClass}__layer {_splitClass}__layer--{(isBase ? "base" : "over")}\">");
			renderer.Write(child);
			renderer.Write("</div>");

			isBase = false;
		}

		renderer.WriteLine("</div>");
	}

	// Renders :::quote — a testimonial whose trailing em-dash line is the attribution.
	private static void WriteQuote(HtmlRenderer renderer, CustomContainer obj)
	{
		ParagraphBlock attribution = null;

		for (var i = obj.Count - 1; i >= 0; i--)
		{
			if (obj[i] is not ParagraphBlock { Inline: not null } paragraph)
				continue;

			if (IsAttribution(GetText(paragraph.Inline)))
				attribution = paragraph;

			break;
		}

		renderer.EnsureLine();
		renderer.Write($"<blockquote class=\"ss-md-quote {_revealClass}\">");

		foreach (var child in obj)
		{
			if (!ReferenceEquals(child, attribution))
				renderer.Write(child);
		}

		if (attribution is not null)
		{
			renderer.Write("<footer class=\"ss-md-quote__by\">");
			renderer.WriteEscape(GetText(attribution.Inline).TrimStart('—', '–', '-', ' '));
			renderer.Write("</footer>");
		}

		renderer.WriteLine("</blockquote>");
	}

	private static bool IsAttribution(string text)
	{
		text = text.TrimStart();

		return text.StartsWith('—') || text.StartsWith('–') || text.StartsWith("--", StringComparison.Ordinal);
	}

	// Splits the container into "heading + the blocks that follow it" groups. Content before the first
	// heading forms an unnamed leading group so nothing is silently dropped.
	private static List<(HeadingBlock Heading, List<Block> Body)> GroupByHeading(CustomContainer obj)
	{
		var groups = new List<(HeadingBlock, List<Block>)>();
		HeadingBlock current = null;
		var body = new List<Block>();

		foreach (var child in obj)
		{
			if (child is HeadingBlock heading)
			{
				if (current is not null || body.Count > 0)
					groups.Add((current, body));

				current = heading;
				body = [];
				continue;
			}

			body.Add(child);
		}

		if (current is not null || body.Count > 0)
			groups.Add((current, body));

		return groups;
	}

	// True when a block can fill the media half: a standalone image paragraph, an embedded player, or a
	// diagram — so a section can show a live scheme or a video instead of a screenshot.
	private static bool IsMediaBlock(Block block)
	{
		if (block is IframeBlock)
			return true;

		// A stacked screenshot pair is media too, so it fills the media half instead of the text column.
		if (block is CustomContainer nested && nested.Info?.Trim().EqualsIgnoreCase("split") == true)
			return true;

		if (block is FencedCodeBlock fenced && fenced.Info?.Trim().EqualsIgnoreCase("diagram") == true)
			return true;

		if (block is not ParagraphBlock { Inline: not null } paragraph)
			return false;

		if (_diagramOnly.IsMatch(GetText(paragraph.Inline)))
			return true;

		var hasImage = false;

		foreach (var inline in paragraph.Inline)
		{
			switch (inline)
			{
				case LinkInline { IsImage: true } when !hasImage:
					hasImage = true;
					break;
				case LineBreakInline:
					break;
				case LiteralInline literal when literal.Content.ToString().Trim().Length == 0:
					break;
				default:
					return false;
			}
		}

		return hasImage;
	}

	// Flattens an inline tree to plain text, keeping the authored line breaks.
	private static string GetText(ContainerInline container)
	{
		var builder = new StringBuilder();

		Append(container);

		return builder.ToString();

		void Append(ContainerInline inlines)
		{
			foreach (var inline in inlines)
			{
				switch (inline)
				{
					case LiteralInline literal:
						builder.Append(literal.Content.ToString());
						break;
					case LineBreakInline:
						builder.AppendLine();
						break;
					// A counter, an entity reference or a diagram is not text yet -- it is a token the fetch
					// phase replaces. Keeping the token is what lets "@connector_count | connectors" read as
					// a live number instead of an empty box.
					case IPlaceholderInline placeholder:
						builder.Append(placeholder.Token);
						break;
					case ContainerInline nested:
						Append(nested);
						break;
				}
			}
		}
	}
}

/// <summary>
/// Markdig extension that replaces the default CustomContainer renderer with the section-aware one
/// (see <see cref="SectionContainerRenderer"/>).
/// </summary>
public class SectionBlockExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// No custom parser needed — Markdig's CustomContainer already handles :::
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			// Remove default CustomContainer renderer and replace with ours
			var existing = htmlRenderer.ObjectRenderers.FindExact<HtmlCustomContainerRenderer>();
			if (existing is not null)
				htmlRenderer.ObjectRenderers.Remove(existing);

			htmlRenderer.ObjectRenderers.AddIfNotAlready<SectionContainerRenderer>();
		}
	}
}
