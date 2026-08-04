namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

public class EntityReferenceInline : LeafInline, IPlaceholderInline
{
	public string EntityType { get; set; }
	public long EntityId { get; set; }

	string IPlaceholderInline.Token => $"{{{{entity:{EntityType}:{EntityId}}}}}";
}

public class EntityReferenceParser : InlineParser
{
	// "product_name" comes before "product": the alternation is ordered, and the shorter name would otherwise
	// match first and then fail on the "_" where it expects "(".
	private static readonly Regex _regex = new(@"@(user|product_name|product|topic|message|page|file)\((\d+)\)", RegexOptions.Compiled);

	public EntityReferenceParser()
	{
		OpeningCharacters = ['@'];
	}

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

public class EntityReferenceRenderer : HtmlObjectRenderer<EntityReferenceInline>
{
	protected override void Write(HtmlRenderer renderer, EntityReferenceInline obj)
	{
		// render placeholder that will be resolved async later
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

public class EntityReferenceExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new EntityReferenceParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<EntityReferenceRenderer>();
	}
}
