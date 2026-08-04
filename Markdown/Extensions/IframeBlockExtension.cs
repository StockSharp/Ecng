namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

/// <summary>
/// AST node for iframe embeds: ::iframe{url=https://... width=640 height=390}
/// </summary>
public class IframeBlock : LeafBlock
{
	public string Url { get; set; }
	public int? Width { get; set; }
	public int? Height { get; set; }

	public IframeBlock(BlockParser parser) : base(parser)
	{
	}
}

/// <summary>
/// Parses ::iframe{url=... width=N height=N} block syntax into <see cref="IframeBlock"/> AST nodes.
/// </summary>
public class IframeBlockParser : BlockParser
{
	private static readonly Regex _regex = new(
		@"^::iframe\{([^}]+)\}\s*$",
		RegexOptions.Compiled);

	public IframeBlockParser()
	{
		OpeningCharacters = [':'];
	}

	public override BlockState TryOpen(BlockProcessor processor)
	{
		if (processor.IsCodeIndent)
			return BlockState.None;

		var line = processor.Line;
		var text = line.ToString().TrimEnd();
		var match = _regex.Match(text);

		if (!match.Success)
			return BlockState.None;

		var block = new IframeBlock(this)
		{
			Span = new(processor.Start, processor.Line.End),
			Line = processor.LineIndex,
			Column = processor.Column,
		};

		ParseAttributes(match.Groups[1].Value, block);

		if (block.Url.IsEmpty())
			return BlockState.None;

		processor.NewBlocks.Push(block);
		return BlockState.BreakDiscard;
	}

	private static void ParseAttributes(string attrs, IframeBlock block)
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
				case "url": block.Url = value; break;
				case "width" when int.TryParse(value, out var w): block.Width = w; break;
				case "height" when int.TryParse(value, out var h): block.Height = h; break;
			}
		}
	}
}

/// <summary>
/// HTML renderer for <see cref="IframeBlock"/>.
///
/// A YouTube embed renders as a compact poster card instead of a full-width player: the thumbnail is a
/// single image, so the page stays light, and the site script swaps in the real player — autoplaying, in
/// a full-width overlay — once the reader clicks it. Any other embed keeps the plain iframe.
/// </summary>
public class IframeBlockRenderer : HtmlObjectRenderer<IframeBlock>
{
	private const string _playIcon =
		"<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M8 5v14l11-7z\"/></svg>";

	private static readonly Regex _youTubeId = new(
		@"(?:youtube\.com/(?:embed/|watch\?(?:.*&)?v=)|youtu\.be/)(?<id>[\w-]{6,})",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private static readonly Regex _vimeoId = new(
		@"vimeo\.com/(?:video/)?(?<id>\d+)",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private static readonly Regex _rutubeId = new(
		@"rutube\.ru/(?:play/embed/|video/)(?<id>[\w-]+)",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private static readonly Regex _vkVideo = new(
		@"(?:vk\.com|vkvideo\.ru)/video_ext\.php\?",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	protected override void Write(HtmlRenderer renderer, IframeBlock obj)
	{
		var player = TryGetPlayer(obj.Url);

		if (player is null)
		{
			renderer.Write($"<iframe src=\"{obj.Url}\"");

			if (obj.Width is not null)
				renderer.Write($" width=\"{obj.Width}\"");
			if (obj.Height is not null)
				renderer.Write($" height=\"{obj.Height}\"");

			renderer.Write(" frameborder=\"0\" allowfullscreen></iframe>");
			return;
		}

		var (embedUrl, posterUrl) = player.Value;

		renderer.Write("<div class=\"ss-md-video");

		// Only YouTube exposes a thumbnail at a predictable address; the other hosts would need an API call,
		// so their card shows a blank poster instead of an image.
		if (posterUrl is null)
			renderer.Write(" ss-md-video--noposter");

		renderer.Write("\" data-video=\"");
		renderer.WriteEscapeUrl(embedUrl);
		renderer.Write("\"");

		if (obj.Width is not null)
			renderer.Write($" style=\"max-width:{obj.Width}px\"");

		renderer.Write("><button type=\"button\" class=\"ss-md-video__btn\">");

		if (posterUrl is null)
			renderer.Write("<span class=\"ss-md-video__poster\"></span>");
		else
		{
			renderer.Write("<img class=\"ss-md-video__poster\" loading=\"lazy\" alt=\"\" src=\"");
			renderer.WriteEscapeUrl(posterUrl);
			renderer.Write("\" />");
		}

		renderer.Write($"<span class=\"ss-md-video__play\">{_playIcon}</span>");
		renderer.Write("</button></div>");
	}

	/// <summary>
	/// Recognises a video host and returns the player url plus a poster, or null when the embed is not a
	/// video at all — an embedded map or page is content to be seen in place, not hidden behind a play
	/// button, so it keeps the plain iframe.
	/// </summary>
	private static (string EmbedUrl, string PosterUrl)? TryGetPlayer(string url)
	{
		if (url.IsEmpty())
			return null;

		var youTube = _youTubeId.Match(url);

		if (youTube.Success)
		{
			var id = youTube.Groups["id"].Value;

			return ($"https://www.youtube.com/embed/{id}", $"https://img.youtube.com/vi/{id}/hqdefault.jpg");
		}

		var vimeo = _vimeoId.Match(url);

		if (vimeo.Success)
			return ($"https://player.vimeo.com/video/{vimeo.Groups["id"].Value}", null);

		var rutube = _rutubeId.Match(url);

		if (rutube.Success)
			return ($"https://rutube.ru/play/embed/{rutube.Groups["id"].Value}", null);

		// A VK embed carries the video identity in its query string, so it is used as authored.
		if (_vkVideo.IsMatch(url))
			return (url, null);

		return null;
	}
}

/// <summary>
/// Markdig extension for iframe embed blocks.
/// </summary>
public class IframeBlockExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new IframeBlockParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<IframeBlockRenderer>();
	}
}
