namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

public class DiagramInline : LeafInline, IPlaceholderInline
{
	// The raw @diagram argument: either a numeric file id or an http(s) URL.
	public string Ref { get; set; }

	string IPlaceholderInline.Token => $"{{{{diagram:{Ref}}}}}";
}

public class DiagramParser : InlineParser
{
	// dg is a numeric file id OR an http(s) URL, so capture the whole argument up to the closing paren.
	private static readonly Regex _regex = new(@"@diagram\(([^)]+)\)", RegexOptions.Compiled);

	public DiagramParser()
	{
		OpeningCharacters = ['@'];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		if (slice.PeekCharExtra(1) != 'd' || slice.PeekCharExtra(2) != 'i')
			return false;

		var start = slice.Start;
		var text = slice.Text;
		var remaining = text[start..];

		var match = _regex.Match(remaining);
		if (!match.Success || match.Index != 0)
			return false;

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		processor.Inline = new DiagramInline
		{
			Ref = match.Groups[1].Value,
			Span = new(startPos, startPos + match.Length - 1),
			Line = line,
			Column = col,
		};
		slice.Start += match.Length;
		return true;
	}
}

public class DiagramRenderer : HtmlObjectRenderer<DiagramInline>
{
	protected override void Write(HtmlRenderer renderer, DiagramInline obj)
	{
		// Emit a placeholder resolved later (Md2HtmlFormatter.ResolveDiagrams) once the ref is turned into
		// a diagram host element by the async fetch phase.
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

public class DiagramExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new DiagramParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<DiagramRenderer>();
	}
}

// Renders a ```diagram fenced code block, whose body is a Designer schema JSON pasted straight into the
// message, as a diagram host carrying that JSON. Every other code block delegates to the renderer this one
// wraps, so mermaid, syntax-highlighted and indented blocks keep rendering exactly as before.
public class DiagramCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
	private readonly IMarkdownObjectRenderer _fallback;

	public DiagramCodeBlockRenderer(IMarkdownObjectRenderer fallback)
	{
		_fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
	}

	protected override void Write(HtmlRenderer renderer, CodeBlock obj)
	{
		if (obj is FencedCodeBlock fenced && string.Equals(fenced.Info?.Trim(), DiagramCodeBlockExtension.InfoKeyword, StringComparison.OrdinalIgnoreCase))
		{
			// Embed the schema JSON verbatim in a data script so the client can parse and draw it without a
			// round-trip. Every '<' is rewritten to the JSON unicode escape below so the payload can never
			// form a '</script>' end tag or a '<!--' comment state and break out of the script tag; the
			// client's JSON.parse decodes it back to '<', so the schema itself is unchanged.
			var json = GetRawText(fenced).Replace("<", "\\u003c");
			renderer.EnsureLine();
			renderer.Write("<div class=\"ss-diagram-host\"><script type=\"application/json\">");
			renderer.Write(json);
			renderer.Write("</script></div>");
			renderer.EnsureLine();
			return;
		}

		_fallback.Write(renderer, obj);
	}

	private static string GetRawText(LeafBlock block)
	{
		var lines = block.Lines;
		var sb = new StringBuilder();

		for (var i = 0; i < lines.Count; i++)
		{
			if (i > 0)
				sb.Append('\n');

			sb.Append(lines.Lines[i].Slice.ToString());
		}

		return sb.ToString();
	}
}

// Content-manager-only markup: a ```diagram fenced block renders its Designer schema JSON as a diagram.
// Registered only on the trusted (raw-HTML-allowed) pipeline, so for untrusted authors the block stays an
// ordinary code block.
public class DiagramCodeBlockExtension : IMarkdownExtension
{
	public const string InfoKeyword = "diagram";

	public void Setup(MarkdownPipelineBuilder pipeline)
	{
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is not HtmlRenderer htmlRenderer)
			return;

		// By this point another renderer already owns CodeBlock (UseAdvancedExtensions installs Markdig's
		// mermaid/diagrams renderer over the plain CodeBlockRenderer). Wrap whichever is current so
		// ```diagram becomes a host while every other code block keeps its existing rendering.
		var current = htmlRenderer.ObjectRenderers.OfType<HtmlObjectRenderer<CodeBlock>>().FirstOrDefault();

		if (current is null or DiagramCodeBlockRenderer)
			return;

		htmlRenderer.ObjectRenderers.Remove(current);
		htmlRenderer.ObjectRenderers.AddIfNotAlready(new DiagramCodeBlockRenderer(current));
	}
}
