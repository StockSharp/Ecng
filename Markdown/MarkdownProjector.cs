namespace Ecng.Markdown;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ecng.Common;
using Ecng.Markdown.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Turns a parsed document into the host-neutral tree the desktop renderers consume.
/// </summary>
/// <remarks>
/// Everything that needs a decision rather than a drawing happens here: role gating, resolving a reference to
/// an address, choosing between a playable video and the reason it is not playable. A host renderer then only
/// maps a node onto its own controls, which is what keeps WPF and Avalonia from drifting apart.
/// </remarks>
public static class MarkdownProjector
{
	/// <summary>
	/// Projects a parsed document.
	/// </summary>
	/// <param name="parsed">Parsed markdown.</param>
	/// <param name="data">Resolved references.</param>
	/// <returns>Host-neutral document.</returns>
	public static MdDocument Project(ParsedMarkdown parsed, ResolvedMarkdownData data)
	{
		ArgumentNullException.ThrowIfNull(parsed);
		ArgumentNullException.ThrowIfNull(data);

		return new(ProjectBlocks(parsed.Document, data));
	}

	private static IReadOnlyList<MdBlock> ProjectBlocks(IEnumerable<Block> blocks, ResolvedMarkdownData data)
	{
		var result = new List<MdBlock>();

		foreach (var block in blocks)
		{
			if (ProjectBlock(block, data) is { } projected)
				result.AddRange(projected);
		}

		return result;
	}

	private static IReadOnlyList<MdBlock> ProjectBlock(Block block, ResolvedMarkdownData data)
	{
		switch (block)
		{
			case HeadingBlock heading:
				return [new MdHeading(heading.Level, ProjectInlines(heading.Inline, data))];

			case ParagraphBlock paragraph:
			{
				var inlines = ProjectInlines(paragraph.Inline, data);

				// A paragraph that held nothing but a diagram reference is a diagram, not a paragraph wrapping
				// one: hosts lay a schema out as a block, and a stray empty paragraph around it shows up as a
				// gap nobody put there.
				if (inlines.Count == 1 && inlines[0] is MdDiagramPlaceholder placeholder)
					return [new MdDiagram(placeholder.Source, string.Empty)];

				return inlines.Count == 0 ? [] : [new MdParagraph(inlines)];
			}

			case CodeBlock code:
				return [ProjectCode(code)];

			case ListBlock list:
				return [new MdList(list.IsOrdered, list.BulletType, [.. list.OfType<ListItemBlock>().Select(i => new MdListItem(ProjectBlocks(i, data)))])];

			case QuoteBlock quote:
				return [new MdQuote(ProjectBlocks(quote, data))];

			case Table table:
				return [ProjectTable(table, data)];

			case ThematicBreakBlock:
				return [new MdThematicBreak()];

			// ":::spoiler Title" arrives as a custom container, not as the SpoilerBlock the parser in the
			// markdown library defines - that extension is not in the pipeline, and the HTML renderer turns
			// the container into a <details> afterwards. A host folds it the same way, from the same node.
			case CustomContainer container when container.Info?.Trim().EqualsIgnoreCase("spoiler") == true:
				return [new MdSpoiler(container.Arguments?.Trim() ?? string.Empty, ProjectBlocks(container, data))];

			// The ":::" sections are what a product page is built from, and each one means something the
			// blocks inside it do not say on their own - which figure is a value and which its label, which
			// block is the illustration and which the text about it. Reading that here is what lets a host
			// draw a page rather than a run of paragraphs.
			case CustomContainer container when ProjectSection(container, data) is { } section:
				return [section];

			// A desktop host has no browser to embed a page in, so the reader gets the address instead of
			// nothing: dropping the block would erase content the author put there with no trace of why.
			case IframeBlock iframe:
			{
				// Except when the page being embedded is a video, which is how a product card carries one.
				// Read as a bare frame it becomes a line of address text where the author put a player.
				if (!VideoThumbnail.For(iframe.Url).IsEmpty())
					return [new MdParagraph([new MdVideo(iframe.Url, string.Empty)])];

				return [new MdEmbed(iframe.Url ?? string.Empty, iframe.Width ?? 0, iframe.Height ?? 0)];
			}

			// A container this projector does not know by name still holds content the reader is meant to see,
			// so its children are projected rather than dropped. Losing a paragraph because its wrapper was
			// unfamiliar is the one failure that leaves no trace on screen.
			case ContainerBlock container:
				return ProjectBlocks(container, data);

			default:
				return [];
		}
	}

	private static MdBlock ProjectSection(CustomContainer container, ResolvedMarkdownData data)
	{
		var info = container.Info?.Trim() ?? string.Empty;

		if (info.EqualsIgnoreCase("stats"))
			return new MdStats(ReadStats(container, data));

		if (info.EqualsIgnoreCase("cards"))
			return new MdCards([.. GroupByHeading(container, data)]);

		if (info.EqualsIgnoreCase("steps"))
			return new MdSteps([.. GroupByHeading(container, data)]);

		if (info.EqualsIgnoreCase("cta"))
			return new MdCta([.. container.Descendants<LinkInline>().Where(l => !l.IsImage)
				.Select(l => new MdLink(l.Url, l.Title ?? string.Empty, ProjectInlines(l, data)))]);

		if (info.EqualsIgnoreCase("quote"))
			return ProjectTestimonial(container, data);

		if (info.EqualsIgnoreCase("split"))
			return new MdSplit([.. container.Select(child => ProjectBlock(child, data))]);

		if (info.EqualsIgnoreCase("feature-left") || info.EqualsIgnoreCase("feature-right"))
			return ProjectFeature(container, data, info.EqualsIgnoreCase("feature-right"));

		if (info.EqualsIgnoreCase("center"))
			return new MdSection(MdAlignments.Center, ProjectBlocks(container, data));

		if (info.EqualsIgnoreCase("left"))
			return new MdSection(MdAlignments.Left, ProjectBlocks(container, data));

		if (info.EqualsIgnoreCase("right"))
			return new MdSection(MdAlignments.Right, ProjectBlocks(container, data));

		return null;
	}

	private static IReadOnlyList<MdStat> ReadStats(CustomContainer container, ResolvedMarkdownData data)
	{
		var result = new List<MdStat>();

		foreach (var paragraph in container.OfType<ParagraphBlock>().Where(p => p.Inline is not null))
		{
			// Read through the projection rather than off the raw inlines. A figure is very often a site
			// counter - "@strategy_count | ready-made strategies" - and a reader that only picked up literal
			// text would drop exactly the number the row exists to show.
			foreach (var line in GetLines(paragraph.Inline, data))
			{
				var text = line.Trim();

				if (text.IsEmpty())
					continue;

				var separator = text.IndexOf('|');

				result.Add(separator < 0
					? new(text, string.Empty)
					: new(text[..separator].Trim(), text[(separator + 1)..].Trim()));
			}
		}

		return result;
	}

	private static IEnumerable<MdCard> GroupByHeading(CustomContainer container, ResolvedMarkdownData data)
	{
		var title = Array.Empty<MdInline>() as IReadOnlyList<MdInline>;
		var body = new List<Block>();

		foreach (var child in container)
		{
			if (child is HeadingBlock heading)
			{
				if (title.Count > 0 || body.Count > 0)
					yield return new(title, ProjectBlocks(body, data));

				title = ProjectInlines(heading.Inline, data);
				body = [];
				continue;
			}

			body.Add(child);
		}

		if (title.Count > 0 || body.Count > 0)
			yield return new(title, ProjectBlocks(body, data));
	}

	private static MdBlock ProjectTestimonial(CustomContainer container, ResolvedMarkdownData data)
	{
		// The attribution is the last paragraph and only when it opens with a dash - the mark an author uses
		// to sign a quotation. A last paragraph without one is part of what is being quoted.
		var last = container.LastOrDefault() as ParagraphBlock;
		var attribution = string.Empty;

		if (last?.Inline is not null && GetPlainText(last.Inline).TrimStart() is { } text &&
			(text.StartsWith('—') || text.StartsWith('–') || text.StartsWith("--", StringComparison.Ordinal)))
		{
			attribution = text.TrimStart('—', '–', '-', ' ');
		}

		var children = attribution.IsEmpty()
			? ProjectBlocks(container, data)
			: ProjectBlocks(container.Where(b => !ReferenceEquals(b, last)), data);

		return new MdTestimonial(children, attribution);
	}

	private static MdBlock ProjectFeature(CustomContainer container, ResolvedMarkdownData data, bool isMediaRight)
	{
		var media = container.FirstOrDefault(IsMedia);

		return new MdFeature(
			media is null ? [] : ProjectBlock(media, data),
			ProjectBlocks(container.Where(b => !ReferenceEquals(b, media)), data),
			isMediaRight,
			container.Arguments?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(a => a.EqualsIgnoreCase("alt")) == true);
	}

	// A block fills the visual half when it is nothing but a picture: a lone image, an embed, a diagram, or
	// a stacked pair of screenshots. A paragraph that also carries words is text, however it starts.
	private static bool IsMedia(Block block)
	{
		if (block is IframeBlock)
			return true;

		if (block is CustomContainer nested && nested.Info?.Trim().EqualsIgnoreCase("split") == true)
			return true;

		if (block is FencedCodeBlock fenced && fenced.Info?.Trim().EqualsIgnoreCase("diagram") == true)
			return true;

		if (block is not ParagraphBlock { Inline: not null } paragraph)
			return false;

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

	private static MdBlock ProjectCode(CodeBlock code)
	{
		var language = (code as FencedCodeBlock)?.Info ?? string.Empty;
		var text = code.Lines.ToString();

		// A ```diagram fence carries the schema itself instead of pointing at one.
		return language.EqualsIgnoreCase("diagram")
			? new MdDiagram(string.Empty, text)
			: new MdCodeBlock(text, language);
	}

	private static MdTable ProjectTable(Table table, ResolvedMarkdownData data)
	{
		var rows = new List<IReadOnlyList<MdTableCell>>();
		var hasHeader = false;

		foreach (var row in table.OfType<TableRow>())
		{
			if (row.IsHeader)
				hasHeader = true;

			rows.Add([.. row.OfType<TableCell>().Select(c => new MdTableCell(ProjectBlocks(c, data)))]);
		}

		return new(rows, hasHeader);
	}

	private static IReadOnlyList<MdInline> ProjectInlines(ContainerInline container, ResolvedMarkdownData data)
	{
		var result = new List<MdInline>();

		if (container is null)
			return result;

		foreach (var inline in container)
		{
			if (ProjectInline(inline, data) is { } projected)
				result.AddRange(projected);
		}

		return result;
	}

	private static IReadOnlyList<MdInline> ProjectInline(Inline inline, ResolvedMarkdownData data)
	{
		switch (inline)
		{
			case LiteralInline literal:
				return [new MdText(literal.Content.ToString())];

			case EmphasisInline emphasis:
				return [new MdEmphasis(ToKind(emphasis), ProjectInlines(emphasis, data))];

			case CodeInline code:
				return [new MdCodeSpan(code.Content)];

			case LineBreakInline:
				return [new MdLineBreak()];

			case LinkInline link:
				return link.IsImage
					? [new MdImage(ResolveTarget(link.Url, data), GetPlainText(link))]
					: [new MdLink(ResolveTarget(link.Url, data), link.Title ?? string.Empty, ProjectInlines(link, data))];

			case DiagramInline diagram:
				return data.Diagrams.TryGetValue(diagram.Ref, out var src) && !src.IsEmpty()
					? [new MdDiagramPlaceholder(src)]
					// Nothing resolved it, so the author's own token stays visible - the same choice the HTML
					// renderer makes, and for the same reason: a silent hole has nobody to blame it on.
					: [new MdText($"@diagram({diagram.Ref})")];

			case VideoInline video:
				return data.Videos.TryGetValue(video.FileId, out var resolved)
					? [new MdVideo(resolved.Url, resolved.UnavailableText)]
					: [];

			case SiteCounterInline counter:
				return data.Counters.TryGetValue(counter.Counter, out var count)
					? [new MdText(count)]
					: [new MdText($"@{counter.Counter.ToString().ToLowerInvariant()}")];

			case EntityReferenceInline entity:
				return ProjectEntity(entity, data);

			case StyledInline styled:
				return [new MdStyledText(styled.Content ?? string.Empty, styled.Color ?? string.Empty,
					styled.FontSize ?? string.Empty, styled.FontFamily ?? string.Empty)];

			case RoleBlockInline role:
				// The reader either holds the role or never learns the fragment existed.
				return data.Roles.TryGetValue(role.RoleId, out var granted) && granted
					? ProjectInlines(role, data)
					: [];

			case ContainerInline container:
				return ProjectInlines(container, data);

			default:
				return [];
		}
	}

	private static IReadOnlyList<MdInline> ProjectEntity(EntityReferenceInline entity, ResolvedMarkdownData data)
	{
		if (!data.Entities.TryGetValue(entity.EntityType, out var byId)
			|| !byId.TryGetValue(entity.EntityId, out var resolved)
			|| resolved is null)
		{
			return [new MdText($"@{entity.EntityType}({entity.EntityId})")];
		}

		var name = resolved.Name ?? string.Empty;

		// The name-only form is a word in a sentence, not something to click.
		return entity.EntityType.EqualsIgnoreCase(Md2HtmlFormatter.ProductNameEntity)
			? [new MdText(name)]
			: [new MdLink(resolved.Url ?? string.Empty, name, [new MdText(name)])];
	}

	private static MdEmphasisKinds ToKind(EmphasisInline emphasis)
		=> emphasis.DelimiterChar switch
		{
			'~' => emphasis.DelimiterCount == 2 ? MdEmphasisKinds.Strikethrough : MdEmphasisKinds.Subscript,
			'^' => MdEmphasisKinds.Superscript,
			_ => emphasis.DelimiterCount >= 2 ? MdEmphasisKinds.Bold : MdEmphasisKinds.Italic,
		};

	// An author writes a picture or a download as the file's id - "![shot](149282)" - and only the server
	// knows where that file lives. Left as written, the id reaches a host as an address, which is why a card
	// showed alt text where every picture was meant to be.
	private static string ResolveTarget(string url, ResolvedMarkdownData data)
	{
		if (url.IsEmpty() || !long.TryParse(url, out var fileId))
			return url;

		if (!data.Files.TryGetValue(fileId, out var file) || file.Url.IsEmpty())
			return url;

		// "~/" is how the server writes a path relative to its own root; a desktop reader has no such root.
		return file.Url.StartsWith("~/", StringComparison.Ordinal) ? file.Url[1..] : file.Url;
	}

	private static string GetPlainText(ContainerInline container)
		=> container.Descendants<LiteralInline>().Select(l => l.Content.ToString()).Join(string.Empty);

	// Where a line ends matters here: a stats section puts one figure on each, and flattening them would
	// turn three figures into one very long one. Everything else on the line goes through the projection, so
	// a counter or an entity reference contributes the text it resolved to rather than nothing at all.
	private static IEnumerable<string> GetLines(ContainerInline container, ResolvedMarkdownData data)
	{
		var builder = new StringBuilder();

		foreach (var inline in container)
		{
			if (inline is LineBreakInline)
			{
				yield return builder.ToString();
				builder.Clear();
				continue;
			}

			foreach (var projected in ProjectInline(inline, data))
				builder.Append(Flatten(projected));
		}

		yield return builder.ToString();
	}

	private static string Flatten(MdInline inline) => inline switch
	{
		MdText text => text.Text,
		MdCodeSpan code => code.Text,
		MdStyledText styled => styled.Text,
		MdEmphasis emphasis => emphasis.Children.Select(Flatten).Join(string.Empty),
		MdLink link => link.Children.Select(Flatten).Join(string.Empty),
		_ => string.Empty,
	};

	/// <summary>
	/// A resolved diagram reference while it is still inside a paragraph; lifted to a block right after.
	/// </summary>
	private sealed class MdDiagramPlaceholder(string source) : MdInline
	{
		public string Source { get; } = source;
	}
}
