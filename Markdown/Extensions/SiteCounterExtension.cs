namespace Ecng.Markdown.Extensions;

using System.Collections.Concurrent;

using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

/// <summary>
/// AST node for a site counter reference such as "@user_count", or one with the word forms its number is read
/// with: "@user_count(пользователь/пользователя/пользователей)".
/// </summary>
public class SiteCounterInline : LeafInline, IPlaceholderInline
{
	/// <summary>
	/// The counter's name as written: "user" for "@user_count".
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The key the host resolves the counter by (see <see cref="SiteCounterParser.Register"/>).
	/// </summary>
	public string Key { get; set; }

	/// <summary>
	/// The word forms written after the counter, in the order its language's plural rule counts them (for
	/// Russian: one, few, many), or empty when the number stands alone.
	/// </summary>
	public IReadOnlyList<string> Forms { get; set; } = [];

	string IPlaceholderInline.Token => Forms.Count == 0
		? $"{{{{count:{Name}}}}}"
		: $"{{{{count:{Name}({string.Join('/', Forms)})}}}}";
}

/// <summary>
/// Parses the site counters the host registered ("@user_count"), each optionally followed by its word forms:
/// "@user_count(пользователь/пользователя/пользователей)".
/// </summary>
/// <remarks>
/// Which counters exist is the host's business, so none does until the host registers it, and a name it never
/// registered stays text. The forms are what keep a live number grammatical: a noun typed after it is right
/// for some counts only. The "_count" ending keeps a counter apart from the entity references "@product(1)"
/// and friends, forms or not.
/// </remarks>
public class SiteCounterParser : InlineParser
{
	// The key of every registered counter, by the name it is written with.
	private static readonly ConcurrentDictionary<string, string> _keys = new(StringComparer.OrdinalIgnoreCase);

	private static readonly Regex _name = new("^[a-z]+$", RegexOptions.Compiled);

	// Not \b: in .NET a CJK letter is a word character, so "@user_countの" would never match. A word form holds
	// none of the characters that end it, the placeholder it travels in or a table cell it sits in.
	private static readonly Regex _regex = new(@"@([a-z]+)_count(?:\(([^()/{}|\r\n]+(?:/[^()/{}|\r\n]+)*)\))?(?![A-Za-z0-9_])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	/// <summary>
	/// Makes "@<paramref name="name"/>_count" a counter the host resolves by <paramref name="key"/>. Registering
	/// the same pair again changes nothing.
	/// </summary>
	/// <param name="name">The name as written in a text, lower-case letters: "user" for "@user_count".</param>
	/// <param name="key">The key the host resolves the counter by, and the key of its value in <see cref="ResolvedMarkdownData.Counters"/>.</param>
	/// <exception cref="ArgumentException">The name is not one a text can carry, the key is empty, or the name already stands for another key.</exception>
	public static void Register(string name, string key)
	{
		if (name is null || !_name.IsMatch(name))
			throw new ArgumentException($"'{name}' is not a counter name.", nameof(name));

		if (key.IsEmptyOrWhiteSpace())
			throw new ArgumentException("A counter needs a key.", nameof(key));

		var registered = _keys.GetOrAdd(name, key);

		if (registered != key)
			throw new ArgumentException($"'@{name}_count' already stands for '{registered}'.", nameof(name));
	}

	/// <summary>The key of the counter a written name stands for, e.g. the host's key for "user".</summary>
	public static bool TryParseName(string name, out string key)
		=> _keys.TryGetValue(name ?? string.Empty, out key);

	/// <summary>The token a counter is written as, so an unresolved one can be put back the way it was typed.</summary>
	/// <param name="name">The counter's name as written.</param>
	/// <param name="forms">The word forms written after it, empty when none.</param>
	/// <returns>The token.</returns>
	public static string ToToken(string name, IReadOnlyCollection<string> forms)
	{
		ArgumentNullException.ThrowIfNull(forms);

		var token = $"@{name}_count";

		return forms.Count == 0 ? token : $"{token}({string.Join('/', forms)})";
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SiteCounterParser"/> class.
	/// </summary>
	public SiteCounterParser()
	{
		OpeningCharacters = ['@'];
	}

	/// <inheritdoc />
	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var start = slice.Start;
		var match = _regex.Match(slice.Text[start..]);

		if (!match.Success || match.Index != 0 || !TryParseName(match.Groups[1].Value, out var key))
			return false;

		var startPos = processor.GetSourcePosition(start, out var line, out var col);

		processor.Inline = new SiteCounterInline
		{
			Name = match.Groups[1].Value,
			Key = key,
			Forms = match.Groups[2].Success ? [.. match.Groups[2].Value.Split('/').Select(form => form.Trim())] : [],
			Span = new(startPos, startPos + match.Length - 1),
			Line = line,
			Column = col,
		};

		slice.Start += match.Length;
		return true;
	}
}

/// <summary>
/// Renders a <see cref="SiteCounterInline"/> as its placeholder token.
/// </summary>
public class SiteCounterRenderer : HtmlObjectRenderer<SiteCounterInline>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, SiteCounterInline obj)
	{
		// Placeholder: the number itself is only known once the data has been fetched (see Md2HtmlFormatter).
		// Escaped, because the word forms in it are the author's text.
		renderer.WriteEscape(((IPlaceholderInline)obj).Token);
	}
}

/// <summary>
/// Markdig extension wiring the site counter syntax: parser plus placeholder renderer.
/// </summary>
public class SiteCounterExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new SiteCounterParser());
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
			htmlRenderer.ObjectRenderers.AddIfNotAlready<SiteCounterRenderer>();
	}
}
