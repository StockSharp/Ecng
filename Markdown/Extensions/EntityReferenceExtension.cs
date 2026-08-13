namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

/// <summary>
/// AST node for an "@type(id)" entity reference (user, product, topic, message, page, file).
/// </summary>
public class EntityReferenceInline : LeafInline, IPlaceholderInline
{
	/// <summary>
	/// The referenced entity type (user, product_name, product, topic, message, page, file).
	/// </summary>
	public string EntityType { get; set; }

	/// <summary>
	/// The referenced entity id.
	/// </summary>
	public long EntityId { get; set; }

	string IPlaceholderInline.Token => $"{{{{entity:{EntityType}:{EntityId}}}}}";
}

/// <summary>
/// Parses "@type(id)" into an <see cref="EntityReferenceInline"/>.
/// </summary>
public class EntityReferenceParser : InlineParser
{
	// "product_name" comes before "product": the alternation is ordered, and the shorter name would otherwise
	// match first and then fail on the "_" where it expects "(".
	private static readonly Regex _regex = new(@"@(user|product_name|product|topic|message|page|file)\((\d+)\)", RegexOptions.Compiled);

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityReferenceParser"/> class.
	/// </summary>
	public EntityReferenceParser()
	{
		OpeningCharacters = ['@'];
	}

	/// <inheritdoc />
	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var start = slice.Start;
		var text = slice.Text;
		var remaining = text[start..];

		var match = _regex.Match(remaining);
		if (!match.Success || match.Index != 0)
			return false;

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		processor.Inline = new EntityReferenceInline
		{
			EntityType = match.Groups[1].Value,
			EntityId = match.Groups[2].Value.To<long>(),
			Span = new(startPos, startPos + match.Length - 1),
			Line = line,
			Column = col,
		};
		slice.Start += match.Length;
		return true;
	}
}

/// <summary>
/// Renders an <see cref="EntityReferenceInline"/> as its placeholder token.
/// </summary>
public class EntityReferenceRenderer : HtmlObjectRenderer<EntityReferenceInline>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, EntityReferenceInline obj)
	{
		// render placeholder that will be resolved async later
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

/// <summary>
/// Markdig extension wiring the "@type(id)" entity reference syntax: parser plus placeholder renderer.
/// </summary>
public class EntityReferenceExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new EntityReferenceParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<EntityReferenceRenderer>();
	}
}
