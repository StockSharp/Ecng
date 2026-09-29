namespace Ecng.Markdown.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Syntax;

/// <summary>
/// :::section [dashed] [center] [narrow] — a full-width page section. A leading heading and the paragraph after
/// it form the section head, a paragraph of links becomes buttons, everything else is written as is.
/// </summary>
public class LandingSection : ISectionBlock
{
	/// <inheritdoc />
	public string Name => "section";

	/// <inheritdoc />
	public void Write(HtmlRenderer renderer, CustomContainer block)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(block);

		var classes = "ss-section";

		if (SectionBlocks.HasArgument(block, "dashed"))
			classes += " ss-section--dashed";

		if (SectionBlocks.HasArgument(block, "center"))
			classes += " ss-section--center";

		renderer.EnsureLine();
		SectionBlocks.WriteOpenTag(renderer, "section", block, classes);
		renderer.Write(SectionBlocks.HasArgument(block, "narrow") ? "<div class=\"ss-wrap ss-wrap--narrow\">" : "<div class=\"ss-wrap\">");

		var start = 0;

		if (block.Count > 0 && block[0] is HeadingBlock heading)
		{
			renderer.Write("<div class=\"ss-section-head\">");
			LandingBlocks.WriteHeading(renderer, heading, "h2", "ss-display ss-display--md");
			start = 1;

			if (block.Count > 1 && block[1] is ParagraphBlock lede && !LandingBlocks.IsLinkOnly(lede))
			{
				LandingBlocks.WriteText(renderer, lede, "p", "ss-lede");
				start = 2;
			}

			renderer.Write("</div>");
		}

		for (var i = start; i < block.Count; i++)
		{
			if (block[i] is ParagraphBlock paragraph && LandingBlocks.IsLinkOnly(paragraph))
				LandingBlocks.WriteActions(renderer, paragraph, "ss-section-actions", false);
			else
				renderer.Write(block[i]);
		}

		renderer.Write("</div>");
		renderer.WriteLine("</section>");
	}
}
