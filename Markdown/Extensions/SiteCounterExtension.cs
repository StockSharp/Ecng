namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

public class SiteCounterInline : LeafInline, IPlaceholderInline
{
	public SiteCounters Counter { get; set; }

	string IPlaceholderInline.Token => $"{{{{count:{Counter}}}}}";
}

/// <summary>
/// Parses "@indicator_count", "@connector_count", "@user_count", "@strategy_count" and "@app_count".
/// </summary>
/// <remarks>
/// A counter takes no argument, which is what keeps it apart from the entity references "@product(1)" and
/// friends: those require "(" straight after the name.
/// </remarks>
public class SiteCounterParser : InlineParser
{
	private static readonly Dictionary<string, SiteCounters> _names = new(StringComparer.OrdinalIgnoreCase)
	{
		["indicator"] = SiteCounters.Indicators,
		["connector"] = SiteCounters.Connectors,
		["user"] = SiteCounters.Users,
		["strategy"] = SiteCounters.Strategies,
		["app"] = SiteCounters.Apps,
	};

	private static readonly Regex _regex = new(@"@([a-z]+)_count\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	/// <summary>The counter a written name stands for, e.g. "connector" for <see cref="SiteCounters.Connectors"/>.</summary>
	public static bool TryParseName(string name, out SiteCounters counter)
		=> _names.TryGetValue(name ?? string.Empty, out counter);

	/// <summary>The token a counter is written as, so an unresolved one can be put back the way it was typed.</summary>
	public static string ToToken(SiteCounters counter)
		=> $"@{_names.First(p => p.Value == counter).Key}_count";

	public SiteCounterParser()
	{
		OpeningCharacters = ['@'];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var start = slice.Start;
		var match = _regex.Match(slice.Text[start..]);

		if (!match.Success || match.Index != 0 || !_names.TryGetValue(match.Groups[1].Value, out var counter))
			return false;

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		processor.Inline = new SiteCounterInline
		{
			Counter = counter,
			Span = new(startPos, startPos + match.Length - 1),
			Line = line,
			Column = col,
		};

		slice.Start += match.Length;
		return true;
	}
}

public class SiteCounterRenderer : HtmlObjectRenderer<SiteCounterInline>
{
	protected override void Write(HtmlRenderer renderer, SiteCounterInline obj)
	{
		// Placeholder: the number itself is only known once the data has been fetched (see Md2HtmlFormatter).
		renderer.Write(((IPlaceholderInline)obj).Token);
	}
}

public class SiteCounterExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new SiteCounterParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<SiteCounterRenderer>();
	}
}
