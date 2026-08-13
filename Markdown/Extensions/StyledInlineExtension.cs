namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

/// <summary>
/// AST node for styled inline text: :[content]{color=red size=24pt font=Arial float=left}
/// </summary>
public class StyledInline : LeafInline
{
	/// <summary>
	/// The styled content, kept as raw markdown.
	/// </summary>
	public string Content { get; set; }

	/// <summary>
	/// CSS color value, or empty when not set.
	/// </summary>
	public string Color { get; set; }

	/// <summary>
	/// CSS font-size value, or empty when not set.
	/// </summary>
	public string FontSize { get; set; }

	/// <summary>
	/// CSS font-family value, or empty when not set.
	/// </summary>
	public string FontFamily { get; set; }

	/// <summary>
	/// CSS float value, or empty when not set.
	/// </summary>
	public string Float { get; set; }
}

/// <summary>
/// Parses :[content]{key=value ...} syntax into <see cref="StyledInline"/> AST nodes.
/// </summary>
public class StyledInlineParser : InlineParser
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StyledInlineParser"/> class.
	/// </summary>
	public StyledInlineParser()
	{
		OpeningCharacters = [':'];
	}

	/// <inheritdoc />
	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		// Must be :[
		if (slice.PeekCharExtra(1) != '[')
			return false;

		var start = slice.Start;
		var text = slice.Text;

		// Find matching ] with bracket balancing (content may contain [links](url))
		var contentStart = start + 2; // after :[
		var bracketDepth = 1;
		var pos = contentStart;

		while (pos < text.Length && bracketDepth > 0)
		{
			var ch = text[pos];
			if (ch == '[') bracketDepth++;
			else if (ch == ']') bracketDepth--;
			pos++;
		}

		if (bracketDepth != 0)
			return false;

		var contentEnd = pos - 1; // position of ]

		// Must be followed by {
		if (pos >= text.Length || text[pos] != '{')
			return false;

		var attrStart = pos + 1; // after {

		// Find matching }
		var attrEnd = text.IndexOf('}', attrStart);
		if (attrEnd < 0)
			return false;

		var content = text[contentStart..contentEnd];
		var attrs = text[attrStart..attrEnd];

		// Parse attributes
		var styled = new StyledInline { Content = content };
		ParseAttributes(attrs, styled);

		if (styled.Color.IsEmpty() && styled.FontSize.IsEmpty() && styled.FontFamily.IsEmpty() && styled.Float.IsEmpty())
			return false; // no recognized attributes

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		styled.Span = new(startPos, startPos + (attrEnd - start));
		styled.Line = line;
		styled.Column = col;

		processor.Inline = styled;
		slice.Start = attrEnd + 1;
		return true;
	}

	private static void ParseAttributes(string attrs, StyledInline styled)
	{
		foreach (var pair in attrs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			var eq = pair.IndexOf('=');
			if (eq <= 0)
				continue;

			var key = pair[..eq].Trim();
			var value = pair[(eq + 1)..].Trim();

			switch (key)
			{
				case "color": styled.Color = value; break;
				case "size": styled.FontSize = value; break;
				case "font": styled.FontFamily = value; break;
				case "float": styled.Float = value; break;
			}
		}
	}
}

/// <summary>
/// HTML renderer for <see cref="StyledInline"/>.
/// Renders inner content through Markdig to support nested markdown.
/// </summary>
public class StyledInlineHtmlRenderer : HtmlObjectRenderer<StyledInline>
{
	private readonly MarkdownPipeline _pipeline;

	/// <summary>
	/// Initializes a new instance of the <see cref="StyledInlineHtmlRenderer"/> class.
	/// </summary>
	/// <param name="pipeline">The pipeline the inner content is rendered with.</param>
	public StyledInlineHtmlRenderer(MarkdownPipeline pipeline)
		=> _pipeline = pipeline;

	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, StyledInline obj)
	{
		var styles = new List<string>();

		if (!obj.Color.IsEmpty())
			styles.Add($"color:{obj.Color}");
		if (!obj.FontSize.IsEmpty())
			styles.Add($"font-size:{obj.FontSize}");
		if (!obj.FontFamily.IsEmpty())
			styles.Add($"font-family:{obj.FontFamily}");
		if (!obj.Float.IsEmpty())
			styles.Add($"float:{obj.Float}");

		renderer.Write($"<span style=\"{styles.Join("; ")}\">");

		// Render inner content as markdown (supports **bold**, *italic*, @entity refs, etc.)
		var innerHtml = Markdig.Markdown.ToHtml(obj.Content, _pipeline).Trim();

		// Strip wrapping <p>...</p> so content stays inline
		if (innerHtml.StartsWith("<p>") && innerHtml.EndsWith("</p>"))
			innerHtml = innerHtml[3..^4];

		renderer.Write(innerHtml);
		renderer.Write("</span>");
	}
}

/// <summary>
/// Markdig extension for styled inline text.
/// </summary>
public class StyledInlineExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new StyledInlineParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready(new StyledInlineHtmlRenderer(pipeline));
	}
}
