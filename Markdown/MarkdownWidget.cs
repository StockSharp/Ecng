namespace Ecng.Markdown;

using System.Net;

using Ecng.Markdown.Extensions;

/// <summary>
/// A control the host puts in the place where the text says <c>@widget(name key=value ...)</c>
/// (see <see cref="WidgetExtension"/>). The markdown renders it as an empty marker element; the host cuts the
/// rendered HTML at the markers with <see cref="Split"/> and draws each widget itself.
/// </summary>
public class MarkdownWidget
{
	private const string _namePattern = "[a-z0-9]+(?:-[a-z0-9]+)*";
	private const string _keyPattern = "[a-z][a-z0-9-]*";

	private static readonly Regex _name = new($"^{_namePattern}$", RegexOptions.Compiled);
	private static readonly Regex _key = new($"^{_keyPattern}$", RegexOptions.Compiled);

	private static readonly Regex _marker = new(
		$@"<div class=""ss-md-widget"" data-widget=""(?<name>{_namePattern})""(?<args>(?: data-arg-{_keyPattern}=""[^""]*"")*)></div>",
		RegexOptions.Compiled);

	private static readonly Regex _markerArgument = new($@" data-arg-(?<key>{_keyPattern})=""(?<value>[^""]*)""", RegexOptions.Compiled);

	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownWidget"/>.
	/// </summary>
	/// <param name="name">The widget name: lower-case letters and digits, words joined by single hyphens.</param>
	/// <param name="arguments">The arguments by key; a key is lower-case, starts with a letter and may hold digits and hyphens.</param>
	/// <exception cref="ArgumentNullException"><paramref name="arguments"/> or one of its values is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">The name or a key is not in the form the markdown can carry.</exception>
	public MarkdownWidget(string name, IReadOnlyDictionary<string, string> arguments)
	{
		if (name is null || !_name.IsMatch(name))
			throw new ArgumentException($"'{name}' is not a widget name.", nameof(name));

		ArgumentNullException.ThrowIfNull(arguments);

		foreach (var (key, value) in arguments)
		{
			if (key is null || !_key.IsMatch(key))
				throw new ArgumentException($"'{key}' is not a widget argument key.", nameof(arguments));

			if (value is null)
				throw new ArgumentNullException(nameof(arguments), $"Argument '{key}' has no value.");
		}

		Name = name;
		Arguments = arguments;
	}

	/// <summary>The widget name.</summary>
	public string Name { get; }

	/// <summary>The arguments written in the line, by key.</summary>
	public IReadOnlyDictionary<string, string> Arguments { get; }

	/// <summary>
	/// The marker element that stands for the widget in the rendered HTML.
	/// </summary>
	/// <returns>The marker markup.</returns>
	public string ToHtml()
	{
		var sb = new StringBuilder($"<div class=\"ss-md-widget\" data-widget=\"{Name}\"");

		foreach (var (key, value) in Arguments)
			sb.Append($" data-arg-{key}=\"{WebUtility.HtmlEncode(value)}\"");

		return sb.Append("></div>").ToString();
	}

	/// <summary>
	/// Cuts rendered HTML at its widget markers.
	/// </summary>
	/// <param name="html">The rendered HTML.</param>
	/// <returns>The markup and widget parts in document order; markup that is only white space is left out.</returns>
	public static IReadOnlyList<MarkdownWidgetPart> Split(string html)
	{
		if (html.IsEmpty())
			return [];

		var parts = new List<MarkdownWidgetPart>();
		var start = 0;

		void AddHtml(int end)
		{
			var text = html[start..end];

			if (!text.IsEmptyOrWhiteSpace())
				parts.Add(new(text, null));
		}

		foreach (Match match in _marker.Matches(html))
		{
			AddHtml(match.Index);

			var arguments = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (Match argument in _markerArgument.Matches(match.Groups["args"].Value))
				arguments[argument.Groups["key"].Value] = WebUtility.HtmlDecode(argument.Groups["value"].Value);

			parts.Add(new(null, new(match.Groups["name"].Value, arguments)));
			start = match.Index + match.Length;
		}

		AddHtml(html.Length);

		return parts;
	}
}
