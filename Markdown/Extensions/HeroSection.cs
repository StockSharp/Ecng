namespace Ecng.Markdown.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Syntax;

/// <summary>
/// :::hero — the page opening. A paragraph above the heading is the eyebrow, the first heading the title, the
/// text after it the lede and a paragraph of links the buttons. A nested block (:::index) is written as is,
/// and text after the buttons or the nested block is the note under them.
/// </summary>
public class HeroSection : ISectionBlock
{
	/// <inheritdoc />
	public string Name => "hero";

	/// <inheritdoc />
	public void Write(HtmlRenderer renderer, CustomContainer block)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(block);

		renderer.EnsureLine();
		SectionBlocks.WriteOpenTag(renderer, "section", block, "ss-hero ss-hero--compact");
		renderer.Write("<div class=\"ss-wrap\">");

		var hasTitle = false;
		var isNote = false;

		foreach (var child in block)
		{
			switch (child)
			{
				case HeadingBlock heading when !hasTitle:
					LandingBlocks.WriteHeading(renderer, heading, "h1", "ss-display ss-display--lg ss-hero-title");
					hasTitle = true;
					break;
				case ParagraphBlock paragraph when !hasTitle:
					LandingBlocks.WriteText(renderer, paragraph, "span", "ss-eyebrow");
					break;
				case ParagraphBlock paragraph when LandingBlocks.IsLinkOnly(paragraph):
					LandingBlocks.WriteActions(renderer, paragraph, "ss-hero-actions", true);
					isNote = true;
					break;
				case ParagraphBlock paragraph:
					LandingBlocks.WriteText(renderer, paragraph, "p", isNote ? "ss-hero-note" : "ss-lede ss-hero-lede");
					break;
				case CustomContainer:
					renderer.Write(child);
					isNote = true;
					break;
				default:
					renderer.Write(child);
					break;
			}
		}

		renderer.Write("</div>");
		renderer.WriteLine("</section>");
	}
}
