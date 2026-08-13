namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

/// <summary>
/// AST node for an "@role(id){content}" span whose content only readers with the role see.
/// </summary>
public class RoleBlockInline : ContainerInline, IPlaceholderInline
{
	/// <summary>
	/// The role id that gates the content.
	/// </summary>
	public long RoleId { get; set; }

	/// <summary>
	/// The gated content, kept as raw text.
	/// </summary>
	public string Content { get; set; }

	string IPlaceholderInline.Token => $"{{{{role:{RoleId}:{Content}}}}}";
}

/// <summary>
/// Parses "@role(id){content}" into a <see cref="RoleBlockInline"/>.
/// </summary>
public class RoleBlockParser : InlineParser
{
	private static readonly Regex _regex = new(@"@role\((\d+)\)\{([^}]*)\}", RegexOptions.Compiled | RegexOptions.Singleline);

	/// <summary>
	/// Initializes a new instance of the <see cref="RoleBlockParser"/> class.
	/// </summary>
	public RoleBlockParser()
	{
		OpeningCharacters = ['@'];
	}

	/// <inheritdoc />
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

/// <summary>
/// Renders a <see cref="RoleBlockInline"/> as its placeholder token.
/// </summary>
public class RoleBlockRenderer : HtmlObjectRenderer<RoleBlockInline>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, RoleBlockInline obj)
	{
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

/// <summary>
/// Markdig extension wiring the "@role(id){content}" syntax: parser plus placeholder renderer.
/// </summary>
public class RoleBlockExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new RoleBlockParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<RoleBlockRenderer>();
	}
}
