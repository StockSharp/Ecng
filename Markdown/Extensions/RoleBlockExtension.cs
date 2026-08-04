namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

public class RoleBlockInline : ContainerInline, IPlaceholderInline
{
	public long RoleId { get; set; }
	public string Content { get; set; }

	string IPlaceholderInline.Token => $"{{{{role:{RoleId}:{Content}}}}}";
}

public class RoleBlockParser : InlineParser
{
	private static readonly Regex _regex = new(@"@role\((\d+)\)\{([^}]*)\}", RegexOptions.Compiled | RegexOptions.Singleline);

	public RoleBlockParser()
	{
		OpeningCharacters = ['@'];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		if (slice.PeekCharExtra(1) != 'r' || slice.PeekCharExtra(2) != 'o')
			return false;

		var start = slice.Start;
		var text = slice.Text;
		var remaining = text[start..];

		var match = _regex.Match(remaining);
		if (!match.Success || match.Index != 0)
			return false;

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		processor.Inline = new RoleBlockInline
		{
			RoleId = match.Groups[1].Value.To<long>(),
			Content = match.Groups[2].Value,
			Span = new(startPos, startPos + match.Length - 1),
			Line = line,
			Column = col,
		};
		slice.Start += match.Length;
		return true;
	}
}

public class RoleBlockRenderer : HtmlObjectRenderer<RoleBlockInline>
{
	protected override void Write(HtmlRenderer renderer, RoleBlockInline obj)
	{
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

public class RoleBlockExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new RoleBlockParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<RoleBlockRenderer>();
	}
}
