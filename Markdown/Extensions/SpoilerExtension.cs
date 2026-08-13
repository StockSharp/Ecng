namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

/// <summary>
/// AST node for a ":::spoiler title" collapsible block.
/// </summary>
public class SpoilerBlock : ContainerBlock
{
	/// <summary>
	/// The text shown on the collapsed block.
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SpoilerBlock"/> class.
	/// </summary>
	/// <param name="parser">The parser that created this block.</param>
	public SpoilerBlock(BlockParser parser) : base(parser)
	{
	}
}

/// <summary>
/// Parses ":::spoiler title" blocks (closed by ":::") into <see cref="SpoilerBlock"/> AST nodes.
/// </summary>
public class SpoilerBlockParser : BlockParser
{
	private static readonly Regex _openRegex = new(@"^:::spoiler\s*(.*)", RegexOptions.Compiled);

	/// <summary>
	/// Initializes a new instance of the <see cref="SpoilerBlockParser"/> class.
	/// </summary>
	public SpoilerBlockParser()
	{
		OpeningCharacters = [':'];
	}

	/// <inheritdoc />
	public override BlockState TryOpen(BlockProcessor processor)
	{
		if (processor.IsCodeIndent)
			return BlockState.None;

		var line = processor.Line;
		var match = _openRegex.Match(line.ToString());

		if (!match.Success)
			return BlockState.None;

		var block = new SpoilerBlock(this)
		{
			Title = match.Groups[1].Value.Trim(),
			Span = new(processor.Start, processor.Line.End),
			Line = processor.LineIndex,
			Column = processor.Column,
		};

		processor.NewBlocks.Push(block);
		return BlockState.ContinueDiscard;
	}

	/// <inheritdoc />
	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		if (processor.Line.ToString().TrimStart().StartsWith(":::") &&
			!processor.Line.ToString().TrimStart().StartsWith(":::spoiler"))
		{
			block.UpdateSpanEnd(processor.Line.End);
			return BlockState.BreakDiscard;
		}

		return BlockState.Continue;
	}
}

/// <summary>
/// Renders a <see cref="SpoilerBlock"/> as a &lt;details&gt;/&lt;summary&gt; element.
/// </summary>
public class SpoilerRenderer : HtmlObjectRenderer<SpoilerBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, SpoilerBlock obj)
	{
		renderer.Write("<details>");
		if (!obj.Title.IsEmpty())
			renderer.Write($"<summary>{obj.Title}</summary>");

		renderer.WriteChildren(obj);
		renderer.Write("</details>");
	}
}

/// <summary>
/// Markdig extension wiring the ":::spoiler" syntax: parser plus renderer.
/// </summary>
public class SpoilerExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new SpoilerBlockParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<SpoilerRenderer>();
	}
}
