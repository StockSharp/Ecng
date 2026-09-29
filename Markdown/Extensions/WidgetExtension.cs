namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

/// <summary>
/// A line that is nothing but <c>@widget(name key=value key="quoted value")</c>: the place where the host puts
/// the named control.
/// </summary>
/// <param name="parser">The parser that produced the block.</param>
public class WidgetBlock(BlockParser parser) : LeafBlock(parser)
{
	/// <summary>The widget the line names.</summary>
	public MarkdownWidget Widget { get; set; }
}

/// <summary>
/// Reads a <see cref="WidgetBlock"/>. A line that does not parse -- a name or key that is not lower-case words
/// joined by hyphens, a repeated key, a quote left open -- stays ordinary text. The name is not checked against any
/// list: a well-formed name the host does not know still becomes a marker, and the host decides what to show for it.
/// </summary>
public class WidgetBlockParser : BlockParser
{
	private static readonly Regex _line = new(
		@"^@widget\((?<name>[a-z0-9]+(?:-[a-z0-9]+)*)(?:\s+(?<key>[a-z][a-z0-9-]*)=(?:""(?<value>[^""]*)""|(?<value>[^\s""()]+)))*\s*\)$",
		RegexOptions.Compiled);

	/// <summary>
	/// Initializes a new instance of the <see cref="WidgetBlockParser"/>.
	/// </summary>
	public WidgetBlockParser()
	{
		OpeningCharacters = ['@'];
	}

	/// <inheritdoc />
	public override BlockState TryOpen(BlockProcessor processor)
	{
		if (processor.IsCodeIndent)
			return BlockState.None;

		var match = _line.Match(processor.Line.ToString().Trim());

		if (!match.Success)
			return BlockState.None;

		var keys = match.Groups["key"].Captures;
		var values = match.Groups["value"].Captures;
		var arguments = new Dictionary<string, string>(StringComparer.Ordinal);

		for (var i = 0; i < keys.Count; i++)
		{
			if (!arguments.TryAdd(keys[i].Value, values[i].Value))
				return BlockState.None;
		}

		processor.NewBlocks.Push(new WidgetBlock(this)
		{
			Widget = new(match.Groups["name"].Value, arguments),
			Column = processor.Column,
			Line = processor.LineIndex,
			Span = new(processor.Start, processor.Line.End),
		});

		return BlockState.BreakDiscard;
	}
}

/// <summary>
/// Writes the marker the host replaces with the widget (see <see cref="MarkdownWidget.Split"/>); plain text gets
/// nothing.
/// </summary>
public class WidgetBlockRenderer : HtmlObjectRenderer<WidgetBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, WidgetBlock obj)
	{
		if (!renderer.EnableHtmlForBlock)
			return;

		renderer.EnsureLine();
		renderer.WriteLine(obj.Widget.ToHtml());
	}
}

/// <summary>
/// Lets the text place a host's controls with <c>@widget(...)</c> lines. Not part of the built-in set: only a host
/// that draws widgets adds it, and everywhere else such a line stays text.
/// </summary>
public class WidgetExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		ArgumentNullException.ThrowIfNull(pipeline);

		if (!pipeline.BlockParsers.Contains<WidgetBlockParser>())
			pipeline.BlockParsers.InsertBefore<ParagraphBlockParser>(new WidgetBlockParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<WidgetBlockRenderer>();
	}
}
