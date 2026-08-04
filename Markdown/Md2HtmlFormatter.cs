namespace Ecng.Markdown;

using System.Net;

using Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

public class Md2HtmlFormatter
{
	private readonly MarkdownPipeline _pipeline;
	private readonly MarkdownPipeline _safePipeline;

	private static readonly Regex _entityPlaceholder = new(@"\{\{entity:(\w+):(\d+)\}\}", RegexOptions.Compiled);
	private static readonly Regex _rolePlaceholder = new(@"\{\{role:(\d+):([^}]*)\}\}", RegexOptions.Compiled);
	private static readonly Regex _videoPlaceholder = new(@"\{\{video:(\d+)\}\}", RegexOptions.Compiled);
	private static readonly Regex _diagramPlaceholder = new(@"\{\{diagram:([^}]+)\}\}", RegexOptions.Compiled);
	private static readonly Regex _imgSrcPattern = new(@"<img\s([^>]*?)src=""(\d+)""([^>]*?)(/?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex _spoilerPattern = new(@"<div\s+class=""spoiler"">\s*<p>([^<]*)</p>([\s\S]*?)</div>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	// Only the opener is matched by regex; the matching close brace is found by counting depth so the
	// gated content may itself contain balanced { } pairs (e.g. a {#anchor} heading or {color=red}
	// directive). A simple [^}]* would stop at the first "}" and leak everything after it.
	private static readonly Regex _rolePrefix = new(@"@role\((\d+)\)\{", RegexOptions.Compiled);
	private static readonly Regex _imgPattern = new(@"!\[[^\]]*\]\((?:(\d+)|[^)]*\/file\/(\d+)[^)]*)\)", RegexOptions.Compiled);
	private static readonly Regex _rawHtmlImgPattern = new(@"<img\s[^>]*?src=""(\d+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex _anchorHrefPattern = new(@"<a\s([^>]*?)href=""(\d+)""([^>]*?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex _htmlTagPattern = new(@"<[^>]+>", RegexOptions.Compiled);
	private static readonly Regex _htmlEntityPattern = new(@"&(?:nbsp|quot|amp|lt|gt|#\d+|#x[\da-fA-F]+);", RegexOptions.Compiled);

	// Pre-processing patterns for Clean() — strip directive syntax before ToPlainText
	private static readonly Regex _styledInlinePattern = new(@":\[([^\]]*(?:\[[^\]]*\][^\]]*)*)\]\{[^}]+\}", RegexOptions.Compiled);
	private static readonly Regex _alignBlockPattern = new(@"^:::(center|left|right|feature-left|feature-right|cards|stats|cta|steps|quote)\b[^\n]*$", RegexOptions.Compiled | RegexOptions.Multiline);
	private static readonly Regex _alignBlockClosePattern = new(@"^:::\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
	private static readonly Regex _iframeBlockPattern = new(@"^::iframe\{[^}]+\}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
	// A ```diagram fenced block carries a raw schema JSON; drop the whole block so the JSON never leaks into
	// the plain-text excerpt (ToPlainText has no diagram renderer and would otherwise emit the JSON as code).
	private static readonly Regex _diagramFencePattern = new(@"```[ \t]*diagram\b[^\n]*\n.*?\n[ \t]*```", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

	// Raw text scanning for entity refs (catches refs inside styled content)
	private static readonly Regex _entityRefPattern = new(@"@(user|product_name|product|topic|message|page|file)\((\d+)\)", RegexOptions.Compiled);

	// Raw text scan for @diagram(dg) refs (also catches diagrams nested in styled content, like the entity scan).
	private static readonly Regex _diagramRefPattern = new(@"@diagram\(([^)]+)\)", RegexOptions.Compiled);

	// Raw text scan for the site counters ("@connector_count"), for the same reason as the entity scan.
	private static readonly Regex _counterRefPattern = new(@"@([a-z]+)_count\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private static readonly Regex _counterPlaceholder = new(@"\{\{count:(\w+)\}\}", RegexOptions.Compiled);

	/// <summary>The entity type that renders as a bare localized name instead of a link.</summary>
	public const string ProductNameEntity = "product_name";

	// A pipe-table delimiter row, e.g. "| --- | --- |", "|---|---|", ":---:|:---:".
	private static readonly Regex _tableDelim = new(@"^\s{0,3}\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$", RegexOptions.Compiled);

	// Bare-email autolinking (post-processing). Markdig's UseAutoLinks linkifies URL schemes and www but not
	// bare emails, so "info@stocksharp.com" gets wrapped in a mailto link like plain URLs are. The rendered
	// HTML is walked tag-by-tag (see LinkifyEmails) so addresses inside <a>/<code>/<pre> or a tag attribute
	// (e.g. an existing mailto href) are left untouched.
	private static readonly Regex _htmlTokenizer = new(@"<[^>]+>|[^<]+", RegexOptions.Compiled | RegexOptions.Singleline);
	private static readonly Regex _htmlTagName = new(@"^<(/?)([a-zA-Z][a-zA-Z0-9]*)", RegexOptions.Compiled);
	private static readonly Regex _bareEmail = new(@"(?<![\w.%+/@-])[A-Za-z0-9][A-Za-z0-9._%+-]*@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}(?![A-Za-z0-9@.-])", RegexOptions.Compiled);

	public Md2HtmlFormatter()
	{
		static MarkdownPipelineBuilder NewBuilder()
			=> new MarkdownPipelineBuilder()
				.UseAdvancedExtensions()
				.Use<EntityReferenceExtension>()
				.Use<SiteCounterExtension>()
				.Use<RoleBlockExtension>()
				.Use<VideoExtension>()
				.Use<DiagramExtension>()
				.Use<StyledInlineExtension>()
				.Use<SectionBlockExtension>()
				.Use<IframeBlockExtension>()
				.Use<OrderedMarkerListExtension>()
				// The inline diagram block (```diagram with a Designer schema JSON) is for every author, not
				// only content managers: a schema is drawn, never executed, and the renderer escapes the
				// payload so it cannot break out of the script tag it travels in.
				.Use<DiagramCodeBlockExtension>();

		// Raw HTML is allowed: trusted authors (content managers, admin pages) may embed
		// markup such as <img src="fileId">, <div class="spoiler"> etc.
		_pipeline = NewBuilder().Build();

		// Raw HTML is stripped: untrusted content (regular forum/blog authors) is rendered
		// with the HTML block/inline parsers removed, so any embedded markup — including
		// <script>, <iframe>, <img onerror=...> — is emitted as escaped text rather than a
		// live element. The directive extensions above still render, since DisableHtml only
		// removes the raw HTML parsers, not our custom syntax.
		_safePipeline = NewBuilder().DisableHtml().Build();
	}

	private MarkdownPipeline GetPipeline(bool allowHtml)
		=> allowHtml ? _pipeline : _safePipeline;

	public ParsedMarkdown Parse(string text, bool allowHtml)
	{
		var pipeline = GetPipeline(allowHtml);

		// Normalize line endings up front so the output is deterministic regardless of the source's
		// CRLF/LF. Markdig normalizes the document itself, but raw passthroughs (e.g. the verbatim
		// @role block content) would otherwise leak the original \r into the rendered HTML.
		text = text.Replace("\r\n", "\n").Replace("\r", "\n");

		// A GitHub-flavoured pipe table only renders as its own block. Content migrated from old HTML
		// tables tends to glue the table to the surrounding paragraphs (and wrap cell content in block
		// tags like <div>), so Markdig folds it into a paragraph / HTML block and emits the "|" and
		// "---" as literal text (e.g. the payment-systems table on /payways). Isolate every pipe table
		// with a blank line before and after so it parses regardless of the surrounding content.
		text = EnsureTableSpacing(text);

		var doc = Markdig.Markdown.Parse(text, pipeline);

		var entities = new HashSet<(string type, long id)>();
		var fileIds = new HashSet<long>();
		var roleIds = new HashSet<long>();
		var videoIds = new HashSet<long>();
		var diagramRefs = new HashSet<string>();

		CollectFromBlock(doc, entities, fileIds, roleIds, videoIds, pipeline);

		// Scan raw text for <img src="NUMERIC"> (raw HTML embedded in markdown)
		foreach (Match match in _rawHtmlImgPattern.Matches(text))
		{
			if (long.TryParse(match.Groups[1].Value, out var fid))
				fileIds.Add(fid);
		}

		// Scan raw text for entity refs that may be inside styled content (:[...]{...})
		// where they aren't parsed as AST nodes. HashSet deduplicates.
		foreach (Match match in _entityRefPattern.Matches(text))
			entities.Add((match.Groups[1].Value, match.Groups[2].Value.To<long>()));

		// Scan raw text for @diagram(dg) refs (dg is a file id or an http(s) URL).
		foreach (Match match in _diagramRefPattern.Matches(text))
			diagramRefs.Add(match.Groups[1].Value);

		// Same raw scan for the site counters, so one inside styled content is fetched too.
		var counterRefs = new HashSet<SiteCounters>();

		foreach (Match match in _counterRefPattern.Matches(text))
		{
			if (SiteCounterParser.TryParseName(match.Groups[1].Value, out var counter))
				counterRefs.Add(counter);
		}

		return new(doc, entities, fileIds, roleIds, videoIds, diagramRefs, counterRefs, pipeline);
	}

	// Ensures every pipe table is separated from the surrounding content by a blank line (before the
	// header row and after the last body row), so Markdig always recognises it as a standalone table
	// block. Skips fenced code; a "---" line is only treated as a table delimiter when the line above
	// it carries a "|" (so thematic breaks and setext headings are left alone).
	internal static string EnsureTableSpacing(string text)
	{
		if (text.IsEmpty() || text.IndexOf('|') < 0)
			return text;

		var lines = text.Split('\n');
		var n = lines.Length;
		var blankBefore = new bool[n];
		var blankAfter = new bool[n];
		var fenced = false;

		for (var i = 0; i < n; i++)
		{
			var t = lines[i].TrimStart();
			if (t.StartsWith("```") || t.StartsWith("~~~"))
			{
				fenced = !fenced;
				continue;
			}
			if (fenced)
				continue;

			if (i > 0 && lines[i].IndexOf('-') >= 0 && _tableDelim.IsMatch(lines[i])
				&& lines[i - 1].IndexOf('|') >= 0 && lines[i - 1].Trim().Length > 0)
			{
				var header = i - 1;
				if (header > 0 && lines[header - 1].Trim().Length > 0)
					blankBefore[header] = true;

				var last = i;
				while (last + 1 < n && lines[last + 1].IndexOf('|') >= 0 && lines[last + 1].Trim().Length > 0)
					last++;

				if (last + 1 < n && lines[last + 1].Trim().Length > 0)
					blankAfter[last] = true;
			}
		}

		var result = new List<string>(n + 8);
		for (var i = 0; i < n; i++)
		{
			if (blankBefore[i])
				result.Add(string.Empty);
			result.Add(lines[i]);
			if (blankAfter[i])
				result.Add(string.Empty);
		}

		return result.JoinN();
	}

	public string Render(ParsedMarkdown parsed, ResolvedMarkdownData data)
	{
		var html = Markdig.Markdown.ToHtml(parsed.Document, parsed.Pipeline);
		html = ResolveImageFiles(html, data.Files);
		html = ResolveLinkFiles(html, data.Files);
		html = ConvertSpoilers(html);
		html = ResolveEntities(html, data.Entities, data.Files);
		html = ResolveCounters(html, data.Counters);
		html = ResolveRoles(html, data.Roles);
		html = ResolveVideos(html, data.Videos);
		html = ResolveDiagrams(html, data.Diagrams);
		html = LinkifyEmails(html);
		return html;
	}

	// Auto-link bare email addresses to mailto: links (Markdig's UseAutoLinks only handles URL schemes and www).
	// Walk the rendered HTML tag-by-tag and linkify only text nodes outside <a>/<code>/<pre>, so an address that
	// is already inside a link, in code, or in a tag attribute (e.g. a mailto href) is left untouched.
	private static string LinkifyEmails(string html)
	{
		if (html.IsEmpty() || !html.Contains('@'))
			return html;

		var a = 0;
		var code = 0;
		var pre = 0;
		var sb = new StringBuilder(html.Length + 32);

		foreach (Match token in _htmlTokenizer.Matches(html))
		{
			var s = token.Value;

			if (s[0] == '<')
			{
				var tag = _htmlTagName.Match(s);
				if (tag.Success)
				{
					var delta = tag.Groups[1].Value.Length == 0 ? 1 : -1;

					switch (tag.Groups[2].Value.ToLowerInvariant())
					{
						case "a": a = Math.Max(0, a + delta); break;
						case "code": code = Math.Max(0, code + delta); break;
						case "pre": pre = Math.Max(0, pre + delta); break;
					}
				}

				sb.Append(s);
			}
			else if (a == 0 && code == 0 && pre == 0)
				sb.Append(_bareEmail.Replace(s, m => $"<a href=\"mailto:{m.Value}\">{m.Value}</a>"));
			else
				sb.Append(s);
		}

		return sb.ToString();
	}

	public string Clean(string text)
	{
		if (text.IsEmptyOrWhiteSpace())
			return text;

		// Strip directive syntax before ToPlainText (which doesn't know about our extensions)
		// :[content]{attrs} → content
		text = _styledInlinePattern.Replace(text, "$1");
		// :::center / :::left / :::right → remove markers, keep content
		text = _alignBlockPattern.Replace(text, string.Empty);
		text = _alignBlockClosePattern.Replace(text, string.Empty);
		// ::iframe{...} → remove entirely
		text = _iframeBlockPattern.Replace(text, string.Empty);
		// ```diagram ... ``` → remove entirely (don't leak the schema JSON into the excerpt)
		text = _diagramFencePattern.Replace(text, string.Empty);

		var plain = Markdig.Markdown.ToPlainText(text, _pipeline);

		// Markdig.ToPlainText does not strip inline HTML (e.g. <span style="...">, <div>, <iframe>)
		// which may still be present in legacy content. Strip remaining HTML tags and entities.
		plain = _htmlTagPattern.Replace(plain, string.Empty);
		plain = WebUtility.HtmlDecode(plain);

		return plain.RemoveMultipleWhitespace();
	}

	public long? FindPicture(string text)
	{
		if (text.IsEmptyOrWhiteSpace())
			return null;

		var match = _imgPattern.Match(text);
		if (!match.Success)
			return null;

		if (match.Groups[1].Success && long.TryParse(match.Groups[1].Value, out var fileId1))
			return fileId1;

		if (match.Groups[2].Success && long.TryParse(match.Groups[2].Value, out var fileId2))
			return fileId2;

		return null;
	}

	// Enumerates every "@role(<id>){ ... }" span. The closing brace is located by depth-counting so the
	// content may contain its own balanced { } pairs (a {#anchor} heading, {color=red} directive, etc.).
	// An opener without a matching close is left as literal text and scanning continues after it.
	private static IEnumerable<(int index, int length, long roleId, string content)> EnumerateRoleBlocks(string text)
	{
		var pos = 0;

		while (pos < text.Length)
		{
			var m = _rolePrefix.Match(text, pos);
			if (!m.Success)
				yield break;

			var contentStart = m.Index + m.Length; // right after the opening '{'
			var depth = 1;
			var i = contentStart;

			for (; i < text.Length && depth > 0; i++)
			{
				if (text[i] == '{')
					depth++;
				else if (text[i] == '}')
					depth--;
			}

			if (depth != 0)
			{
				// No matching close brace: leave this opener literal and keep scanning past it.
				pos = contentStart;
				continue;
			}

			var closeIndex = i - 1; // index of the matching '}'
			yield return (m.Index, closeIndex - m.Index + 1, m.Groups[1].Value.To<long>(),
				text.Substring(contentStart, closeIndex - contentStart));
			pos = closeIndex + 1;
		}
	}

	public HashSet<long> CollectInlineRoleIds(string text)
	{
		var roleIds = new HashSet<long>();

		if (text.IsEmpty())
			return roleIds;

		foreach (var (_, _, roleId, _) in EnumerateRoleBlocks(text))
			roleIds.Add(roleId);

		return roleIds;
	}

	public string ActivateRule(string text, Dictionary<long, bool> roles)
	{
		if (text.IsEmptyOrWhiteSpace())
			return text;

		var blocks = new List<(int index, int length, long roleId, string content)>(EnumerateRoleBlocks(text));
		if (blocks.Count == 0)
			return text;

		var sb = new StringBuilder(text);

		// Replace from the end so earlier indices stay valid.
		for (var i = blocks.Count - 1; i >= 0; i--)
		{
			var (index, length, roleId, content) = blocks[i];
			var hasRole = roles.TryGetValue(roleId, out var v) && v;
			sb.Remove(index, length);
			sb.Insert(index, hasRole ? content : string.Empty);
		}

		return sb.ToString();
	}

	#region AST collection

	private static void CollectFromBlock(Block block,
		HashSet<(string type, long id)> entities, HashSet<long> fileIds,
		HashSet<long> roleIds, HashSet<long> videoIds, MarkdownPipeline pipeline)
	{
		if (block is ContainerBlock container)
		{
			foreach (var child in container)
				CollectFromBlock(child, entities, fileIds, roleIds, videoIds, pipeline);
		}

		if (block is LeafBlock leaf && leaf.Inline is not null)
			CollectFromInline(leaf.Inline, entities, fileIds, roleIds, videoIds, pipeline);
	}

	private static void CollectFromInline(Inline inline,
		HashSet<(string type, long id)> entities, HashSet<long> fileIds,
		HashSet<long> roleIds, HashSet<long> videoIds, MarkdownPipeline pipeline)
	{
		switch (inline)
		{
			case EntityReferenceInline entityRef:
				if (entityRef.EntityType == "file")
					fileIds.Add(entityRef.EntityId);
				else
					entities.Add((entityRef.EntityType, entityRef.EntityId));
				break;

			case RoleBlockInline roleBlock:
				roleIds.Add(roleBlock.RoleId);
				break;

			case VideoInline video:
				videoIds.Add(video.FileId);
				break;

			case LinkInline link when long.TryParse(link.Url, out var fid):
				fileIds.Add(fid);
				break;

			case StyledInline styled:
				// StyledInline stores content as raw string, not as child AST nodes.
				// Re-parse to collect nested references (e.g. ![img](fileId), @entity refs).
				var styledDoc = Markdig.Markdown.Parse(styled.Content, pipeline);
				CollectFromBlock(styledDoc, entities, fileIds, roleIds, videoIds, pipeline);
				break;
		}

		if (inline is ContainerInline container)
		{
			var child = container.FirstChild;
			while (child is not null)
			{
				CollectFromInline(child, entities, fileIds, roleIds, videoIds, pipeline);
				child = child.NextSibling;
			}
		}
	}

	#endregion

	#region Resolve from pre-fetched data

	private static string ResolveImageFiles(string html, Dictionary<long, (string url, string name, string description)> files)
	{
		if (files.Count == 0)
			return html;

		return _imgSrcPattern.Replace(html, match =>
		{
			var fileId = match.Groups[2].Value.To<long>();

			if (!files.TryGetValue(fileId, out var data) || data.url.IsEmpty())
				return match.Value;

			var url = ResolveVirtualPath(data.url);
			var before = match.Groups[1].Value;
			var after = match.Groups[3].Value;
			var selfClose = match.Groups[4].Value;
			return $"<img {before}src=\"{url}\"{after}{selfClose}>";
		});
	}

	private static string ResolveLinkFiles(string html, Dictionary<long, (string url, string name, string description)> files)
	{
		if (files.Count == 0)
			return html;

		return _anchorHrefPattern.Replace(html, match =>
		{
			var fileId = match.Groups[2].Value.To<long>();

			if (!files.TryGetValue(fileId, out var data) || data.url.IsEmpty())
				return match.Value;

			var url = ResolveVirtualPath(data.url);
			var before = match.Groups[1].Value;
			var after = match.Groups[3].Value;
			return $"<a {before}href=\"{url}\"{after}>";
		});
	}

	private static string ConvertSpoilers(string html)
	{
		return _spoilerPattern.Replace(html, m =>
		{
			var title = m.Groups[1].Value.Trim();
			var content = m.Groups[2].Value;
			return $"<details><summary>{title}</summary>{content}</details>";
		});
	}

	private static string ResolveEntities(string html,
		Dictionary<(string type, long id), (string url, string name, string description)> entities,
		Dictionary<long, (string url, string name, string description)> files)
	{
		return _entityPlaceholder.Replace(html, match =>
		{
			var entityType = match.Groups[1].Value;
			var entityId = match.Groups[2].Value.To<long>();

			(string url, string name, string description) data;

			if (entityType == "file")
			{
				if (!files.TryGetValue(entityId, out data))
					return match.Value;
			}
			else
			{
				if (!entities.TryGetValue((entityType, entityId), out data))
					return match.Value;
			}

			// "@product_name(id)" asks for the name a product goes by in this language, to be read inside a
			// sentence -- so it is written as text, never as a link, and escaped like any other content.
			if (entityType == ProductNameEntity)
				return WebUtility.HtmlEncode(data.name ?? string.Empty);

			var url = ResolveVirtualPath(data.url);
			return url.IsEmpty()
				? data.name ?? entityType
				: $"<a href=\"{url}\" title=\"{data.description}\">{data.name}</a>";
		});
	}

	/// <summary>
	/// Puts the site counters in place of their placeholders. A counter that was not resolved is written back
	/// as the source token: a number nobody could obtain must not be invented, and a zero in a sentence reads
	/// as a fact, while the token is visible to whoever edits the page.
	/// </summary>
	private static string ResolveCounters(string html, Dictionary<SiteCounters, string> counters)
	{
		if (!html.Contains("{{count:"))
			return html;

		return _counterPlaceholder.Replace(html, match =>
		{
			if (!Enum.TryParse<SiteCounters>(match.Groups[1].Value, out var counter))
				return match.Value;

			return counters.TryGetValue(counter, out var value) && !value.IsEmpty()
				? WebUtility.HtmlEncode(value)
				: SiteCounterParser.ToToken(counter);
		});
	}

	private static string ResolveRoles(string html, Dictionary<long, bool> roles)
	{
		if (roles.Count == 0)
			return html;

		return _rolePlaceholder.Replace(html, match =>
		{
			var roleId = match.Groups[1].Value.To<long>();
			var content = match.Groups[2].Value;

			return roles.TryGetValue(roleId, out var hasRole) && hasRole
				? content
				: string.Empty;
		});
	}

	private static string ResolveVideos(string html, Dictionary<long, ResolvedVideo> videos)
	{
		if (videos.Count == 0)
			return html;

		return _videoPlaceholder.Replace(html, match =>
		{
			var fileId = match.Groups[1].Value.To<long>();
			return videos.TryGetValue(fileId, out var video)
				? BuildVideo(video)
				: string.Empty;
		});
	}

	/// <summary>
	/// Renders a resolved video as either a player or the reason it cannot be played.
	/// </summary>
	/// <param name="video">Resolved video.</param>
	/// <returns>Video markup, or an empty string when there is nothing to say.</returns>
	/// <remarks>
	/// The caller resolves the address and the reason, not the markup, so a renderer which is not producing
	/// HTML uses the same data to show a player of its own.
	/// </remarks>
	public static string BuildVideo(ResolvedVideo video)
	{
		if (video.IsPlayable)
			return $"<video width=\"640\" height=\"390\" controls controlsList=\"nodownload\"><source src=\"{WebUtility.HtmlEncode(video.Url)}\" type=\"video/mp4\"></video>";

		return video.UnavailableText.IsEmpty()
			? string.Empty
			: $"<span style=\"color:red\"><em>{WebUtility.HtmlEncode(video.UnavailableText)}</em></span>";
	}

	private static string ResolveDiagrams(string html, Dictionary<string, string> diagrams)
	{
		if (!html.Contains("{{diagram:"))
			return html;

		return _diagramPlaceholder.Replace(html, match =>
		{
			var reference = match.Groups[1].Value;

			// A reference nothing could resolve leaves the author's token on the page, the way an
			// unavailable counter does. The alternatives are both silent: rendering nothing leaves a hole
			// with no author to blame it on, and the internal placeholder would read as gibberish.
			return diagrams.TryGetValue(reference, out var src) && !src.IsEmpty()
				? BuildDiagramHost(src)
				: $"@diagram({WebUtility.HtmlEncode(reference)})";
		});
	}

	/// <summary>
	/// Wraps a schema address into the host element the browser-side renderer fills in.
	/// </summary>
	/// <param name="src">Address the schema JSON is fetched from.</param>
	/// <returns>Host element markup.</returns>
	/// <remarks>
	/// The caller resolves a reference to an address and stops there, so that a renderer which is not
	/// producing HTML - a desktop client drawing the schema itself - reads the same address and does its
	/// own thing with it.
	/// </remarks>
	public static string BuildDiagramHost(string src)
		=> $"<div class=\"ss-diagram-host\" data-diagram-src=\"{WebUtility.HtmlEncode(src)}\"></div>";

	#endregion

	private static string ResolveVirtualPath(string url)
		=> UrlHelper.ResolveVirtualPath(url);
}
