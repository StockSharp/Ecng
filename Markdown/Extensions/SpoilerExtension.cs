namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

public class SpoilerBlock : ContainerBlock
{
	public string Title { get; set; }

	public SpoilerBlock(BlockParser parser) : base(parser)
	{
	}
}

public class SpoilerBlockParser : BlockParser
{
	private static readonly Regex _openRegex = new(@"^:::spoiler\s*(.*)", RegexOptions.Compiled);

	public SpoilerBlockParser()
	{
		OpeningCharacters = [':'];
	}

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

public class SpoilerRenderer : HtmlObjectRenderer<SpoilerBlock>
{
	protected override void Write(HtmlRenderer renderer, SpoilerBlock obj)
	{
		renderer.Write("<details>");
		if (!obj.Title.IsEmpty())
			renderer.Write($"<summary>{obj.Title}</summary>");

		renderer.WriteChildren(obj);
		renderer.Write("</details>");
	}
}

public class SpoilerExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new SpoilerBlockParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<SpoilerRenderer>();
	}
}
