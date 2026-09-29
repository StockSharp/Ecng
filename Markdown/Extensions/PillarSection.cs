namespace Ecng.Markdown.Extensions;

using System.Globalization;

using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.Figures;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// :::pillar [band] [flip] {#anchor} — one numbered part of a landing: a picture or a diagram on one side, the
/// text on the other. Picture paragraphs fill the media side (the first picture large, the rest as a strip),
/// as does a ```diagram fence, bare or in a ^^^ figure whose caption labels it. On the text side a paragraph
/// above the heading is the eyebrow, the paragraph after the heading the lede and the first list the features.
/// </summary>
public class PillarSection : ISectionBlock
{
	/// <inheritdoc />
	public string Name => "pillar";

	/// <inheritdoc />
	public void Write(HtmlRenderer renderer, CustomContainer block)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(block);

		var diagram = block.FirstOrDefault(LandingBlocks.IsDiagram);
		var pictures = block.Where(LandingBlocks.IsImageOnly).ToArray();

		renderer.EnsureLine();
		SectionBlocks.WriteOpenTag(renderer, "section", block, SectionBlocks.HasArgument(block, "band") ? "ss-pillar ss-pillar--band" : "ss-pillar");
		renderer.Write("<div class=\"ss-wrap\">");
		renderer.Write(SectionBlocks.HasArgument(block, "flip") ? "<div class=\"ss-pillar__grid ss-pillar__grid--flip\">" : "<div class=\"ss-pillar__grid\">");

		renderer.Write("<div class=\"ss-pillar__media\">");

		if (diagram is not null)
			WriteDiagram(renderer, diagram);

		WritePictures(renderer, [.. pictures.SelectMany(p => ((ParagraphBlock)p).Inline.OfType<LinkInline>())]);

		renderer.Write("</div>");

		renderer.Write("<div class=\"ss-pillar__text\">");
		renderer.Write($"<span class=\"ss-pillar__num\">{GetNumber(block).ToString("00", CultureInfo.InvariantCulture)}</span>");

		var hasTitle = false;
		var hasLede = false;
		var hasList = false;

		foreach (var child in block)
		{
			if (ReferenceEquals(child, diagram) || pictures.Contains(child))
				continue;

			switch (child)
			{
				case HeadingBlock heading when !hasTitle:
					LandingBlocks.WriteHeading(renderer, heading, "h2", "ss-display ss-display--md");
					hasTitle = true;
					break;
				case ParagraphBlock paragraph when !hasTitle:
					LandingBlocks.WriteText(renderer, paragraph, "span", "ss-eyebrow");
					break;
				case ParagraphBlock paragraph when !hasLede:
					LandingBlocks.WriteText(renderer, paragraph, "p", "ss-pillar__lede");
					hasLede = true;
					break;
				case ListBlock list when !hasList:
					WriteFeatures(renderer, list);
					hasList = true;
					break;
				default:
					renderer.Write(child);
					break;
			}
		}

		renderer.Write("</div></div></div>");
		renderer.WriteLine("</section>");
	}

	// A pillar's number is its place among the pillars of the same parent.
	private static int GetNumber(CustomContainer block)
	{
		var number = 1;

		foreach (var sibling in block.Parent)
		{
			if (ReferenceEquals(sibling, block))
				break;

			if (sibling is CustomContainer container && container.Info?.Trim().EqualsIgnoreCase("pillar") == true)
				number++;
		}

		return number;
	}

	private static void WriteDiagram(HtmlRenderer renderer, Block media)
	{
		var figure = media as Figure;
		var fence = figure is null ? (FencedCodeBlock)media : figure.OfType<FencedCodeBlock>().First(LandingBlocks.IsDiagram);
		var caption = figure?.OfType<FigureCaption>().FirstOrDefault();

		var attributes = fence.GetAttributes();

		if (attributes.Classes?.Contains("ss-arch__diagram") != true)
			attributes.AddClass("ss-arch__diagram");

		attributes.AddPropertyIfNotExist("data-diagram-kind", "document");
		attributes.AddPropertyIfNotExist("data-diagram-export", "off");
		attributes.AddPropertyIfNotExist("role", "img");

		var label = caption?.Inline is null ? null : SectionBlocks.GetText(caption.Inline).Trim();

		if (!label.IsEmpty())
			attributes.AddPropertyIfNotExist("aria-label", label);

		renderer.Write("<figure class=\"ss-arch\">");
		renderer.Write(fence);

		if (caption is not null)
		{
			renderer.Write("<figcaption>");
			renderer.WriteLeafInline(caption);
			renderer.Write("</figcaption>");
		}

		renderer.Write("</figure>");
	}

	private static void WritePictures(HtmlRenderer renderer, IReadOnlyList<LinkInline> pictures)
	{
		if (pictures.Count == 0)
			return;

		renderer.Write("<figure class=\"ss-detail-shot\">");
		LandingBlocks.WriteImage(renderer, pictures[0]);
		renderer.Write("</figure>");

		if (pictures.Count == 1)
			return;

		renderer.Write("<div class=\"ss-pillar__strip\">");

		foreach (var picture in pictures.Skip(1))
			LandingBlocks.WriteImage(renderer, picture);

		renderer.Write("</div>");
	}

	private static void WriteFeatures(HtmlRenderer renderer, ListBlock list)
	{
		SectionBlocks.WriteOpenTag(renderer, "ul", list, "ss-feature-list ss-feature-list--cols ss-pillar__list");

		foreach (var item in list.OfType<ListItemBlock>())
		{
			renderer.Write("<li>");

			foreach (var child in item)
			{
				if (child is ParagraphBlock paragraph)
					renderer.WriteLeafInline(paragraph);
				else
					renderer.Write(child);
			}

			renderer.Write("</li>");
		}

		renderer.Write("</ul>");
	}
}
