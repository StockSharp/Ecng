namespace Ecng.Markdown.Extensions;

using System.Globalization;

using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// :::index Label — a numbered table of contents, one list item per part:
/// <c>- [Back office](#backoffice) Clients, money, risk</c>. The words after the name label the navigation for
/// screen readers.
/// </summary>
public class IndexSection : ISectionBlock
{
	/// <inheritdoc />
	public string Name => "index";

	/// <inheritdoc />
	public void Write(HtmlRenderer renderer, CustomContainer block)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(block);

		var label = block.Arguments?.Trim();

		if (!label.IsEmpty())
			block.GetAttributes().AddPropertyIfNotExist("aria-label", label);

		renderer.EnsureLine();
		SectionBlocks.WriteOpenTag(renderer, "nav", block, "ss-pillar-index");

		var number = 0;

		foreach (var item in block.OfType<ListBlock>().SelectMany(l => l.OfType<ListItemBlock>()))
		{
			if (item.OfType<ParagraphBlock>().FirstOrDefault()?.Inline?.OfType<LinkInline>().FirstOrDefault(l => !l.IsImage) is not { } link)
				continue;

			number++;

			renderer.Write("<a href=\"");
			renderer.WriteEscapeUrl(LandingBlocks.GetUrl(link));
			renderer.Write("\" class=\"ss-pillar-index__item no-underline\"><span class=\"ss-pillar-index__num\">");
			renderer.Write(number.ToString("00", CultureInfo.InvariantCulture));
			renderer.Write("</span><span class=\"ss-pillar-index__title\">");
			renderer.WriteChildren(link);
			renderer.Write("</span><span class=\"ss-pillar-index__sub\">");
			WriteSub(renderer, link);
			renderer.Write("</span></a>");
		}

		renderer.WriteLine("</nav>");
	}

	// The text after the link, without the separator an author may put between them.
	private static void WriteSub(HtmlRenderer renderer, LinkInline link)
	{
		var isFirst = true;

		for (var inline = link.NextSibling; inline is not null; inline = inline.NextSibling)
		{
			if (isFirst && inline is LiteralInline literal)
				renderer.WriteEscape(literal.Content.ToString().TrimStart(' ', '—', '–', '-', ':'));
			else
				renderer.Write(inline);

			isFirst = false;
		}
	}
}
