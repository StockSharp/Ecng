namespace Ecng.Markdown.Extensions;

using Markdig.Extensions.Figures;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Pieces the landing sections share: recognising button and picture paragraphs, and writing blocks with the
/// classes the host's stylesheet gives them.
/// </summary>
static class LandingBlocks
{
	private const string _ctaClass = "ss-cta no-underline";
	private const string _primaryCtaClass = "ss-cta ss-cta--solid no-underline";

	/// <summary>A paragraph of links and nothing else, which a section shows as buttons.</summary>
	public static bool IsLinkOnly(Block block)
		=> block is ParagraphBlock { Inline: not null } paragraph && IsOnly(paragraph, l => !l.IsImage);

	/// <summary>A paragraph of pictures and nothing else.</summary>
	public static bool IsImageOnly(Block block)
		=> block is ParagraphBlock { Inline: not null } paragraph && IsOnly(paragraph, l => l.IsImage);

	/// <summary>A ```diagram fence, bare or inside a ^^^ figure.</summary>
	public static bool IsDiagram(Block block)
		=> block is FencedCodeBlock fence
			? fence.Info?.Trim().EqualsIgnoreCase(DiagramCodeBlockExtension.InfoKeyword) == true
			: block is Figure figure && figure.Any(IsDiagram);

	/// <summary>Writes a heading as the given tag, keeping the author's {#id .class}.</summary>
	public static void WriteHeading(HtmlRenderer renderer, HeadingBlock heading, string tag, string classes)
	{
		SectionBlocks.WriteOpenTag(renderer, tag, heading, classes);
		renderer.WriteLeafInline(heading);
		renderer.Write($"</{tag}>");
	}

	/// <summary>Writes a paragraph's text in the given tag, keeping the author's {#id .class}.</summary>
	public static void WriteText(HtmlRenderer renderer, LeafBlock block, string tag, string classes)
	{
		SectionBlocks.WriteOpenTag(renderer, tag, block, classes);
		renderer.WriteLeafInline(block);
		renderer.Write($"</{tag}>");
	}

	/// <summary>Writes the links of a link-only paragraph as buttons, the first one solid.</summary>
	public static void WriteActions(HtmlRenderer renderer, ParagraphBlock paragraph, string wrapperClass, bool withArrow)
	{
		renderer.Write($"<div class=\"{wrapperClass}\">");

		var isFirst = true;

		foreach (var link in paragraph.Inline.OfType<LinkInline>())
		{
			// href first: a host's link policy (away page, nofollow, new tab) rewrites only anchors that open with it.
			renderer.Write("<a href=\"");
			renderer.WriteEscapeUrl(GetUrl(link));
			renderer.Write($"\" class=\"{(isFirst ? _primaryCtaClass : _ctaClass)}\">");
			renderer.WriteChildren(link);

			if (isFirst && withArrow)
				renderer.Write(" <span>→</span>");

			renderer.Write("</a>");

			isFirst = false;
		}

		renderer.Write("</div>");
	}

	/// <summary>Writes a picture with the file id or address the author gave; the formatter resolves ids.</summary>
	public static void WriteImage(HtmlRenderer renderer, LinkInline image)
	{
		renderer.Write("<img src=\"");
		renderer.WriteEscapeUrl(GetUrl(image));
		renderer.Write("\" alt=\"");
		renderer.WriteEscape(SectionBlocks.GetText(image));
		renderer.Write("\" loading=\"lazy\" />");
	}

	/// <summary>The link's address, dynamic ones included.</summary>
	public static string GetUrl(LinkInline link)
		=> link.GetDynamicUrl?.Invoke() ?? link.Url;

	private static bool IsOnly(ParagraphBlock paragraph, Func<LinkInline, bool> isWanted)
	{
		var found = false;

		foreach (var inline in paragraph.Inline)
		{
			switch (inline)
			{
				case LinkInline link when isWanted(link):
					found = true;
					break;
				case LineBreakInline:
					break;
				case LiteralInline literal when literal.Content.ToString().IsEmptyOrWhiteSpace():
					break;
				default:
					return false;
			}
		}

		return found;
	}
}
