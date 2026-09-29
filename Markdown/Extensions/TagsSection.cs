namespace Ecng.Markdown.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// :::tags — a row of small labels, one list item each. An item written wholly in emphasis
/// (<c>- *macOS — in progress*</c>) names something not there yet and gets the muted look.
/// </summary>
public class TagsSection : ISectionBlock
{
	/// <inheritdoc />
	public string Name => "tags";

	/// <inheritdoc />
	public void Write(HtmlRenderer renderer, CustomContainer block)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(block);

		renderer.EnsureLine();
		SectionBlocks.WriteOpenTag(renderer, "div", block, "ss-tag-row");

		foreach (var item in block.OfType<ListBlock>().SelectMany(l => l.OfType<ListItemBlock>()))
		{
			if (item.OfType<ParagraphBlock>().FirstOrDefault() is not { Inline: not null } paragraph)
				continue;

			var soon = GetSoleEmphasis(paragraph.Inline);

			if (soon is null)
			{
				renderer.Write("<span class=\"ss-tag\">");
				renderer.WriteLeafInline(paragraph);
			}
			else
			{
				renderer.Write("<span class=\"ss-tag ss-tag--soon\">");
				renderer.WriteChildren(soon);
			}

			renderer.Write("</span>");
		}

		renderer.WriteLine("</div>");
	}

	private static EmphasisInline GetSoleEmphasis(ContainerInline inlines)
	{
		var meaningful = inlines.Where(i => i is not LiteralInline literal || !literal.Content.ToString().IsEmptyOrWhiteSpace()).ToArray();

		return meaningful.Length == 1 ? meaningful[0] as EmphasisInline : null;
	}
}
