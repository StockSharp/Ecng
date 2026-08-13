namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

/// <summary>
/// AST node for an "@vss(fileId)" video reference.
/// </summary>
public class VideoInline : LeafInline, IPlaceholderInline
{
	/// <summary>
	/// The referenced video file id.
	/// </summary>
	public long FileId { get; set; }

	string IPlaceholderInline.Token => $"{{{{video:{FileId}}}}}";
}

/// <summary>
/// Parses "@vss(fileId)" into a <see cref="VideoInline"/>.
/// </summary>
public class VideoParser : InlineParser
{
	private static readonly Regex _regex = new(@"@vss\((\d+)\)", RegexOptions.Compiled);

	/// <summary>
	/// Initializes a new instance of the <see cref="VideoParser"/> class.
	/// </summary>
	public VideoParser()
	{
		OpeningCharacters = ['@'];
	}

	/// <inheritdoc />
	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		if (slice.PeekCharExtra(1) != 'v' || slice.PeekCharExtra(2) != 's')
			return false;

		var start = slice.Start;
		var text = slice.Text;
		var remaining = text[start..];

		var match = _regex.Match(remaining);
		if (!match.Success || match.Index != 0)
			return false;

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		processor.Inline = new VideoInline
		{
			FileId = match.Groups[1].Value.To<long>(),
			Span = new(startPos, startPos + match.Length - 1),
			Line = line,
			Column = col,
		};
		slice.Start += match.Length;
		return true;
	}
}

/// <summary>
/// Renders a <see cref="VideoInline"/> as its placeholder token.
/// </summary>
public class VideoRenderer : HtmlObjectRenderer<VideoInline>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, VideoInline obj)
	{
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

/// <summary>
/// Markdig extension wiring the "@vss(fileId)" syntax: parser plus placeholder renderer.
/// </summary>
public class VideoExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new VideoParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<VideoRenderer>();
	}
}
