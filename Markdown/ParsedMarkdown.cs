namespace Ecng.Markdown;

using Markdig;
using Markdig.Syntax;

/// <summary>
/// The result of parsing a markdown text: the syntax tree plus every external reference
/// (entities, files, roles, videos, diagrams, site counters) collected during the parse.
/// </summary>
/// <param name="document">The parsed markdown syntax tree.</param>
/// <param name="entityRefs">Entity references found in the text as (type, id) pairs.</param>
/// <param name="fileIds">Identifiers of the files the text references.</param>
/// <param name="roleIds">Identifiers of the roles the text references.</param>
/// <param name="videoIds">Identifiers of the videos the text references.</param>
/// <param name="diagramRefs">Diagram references (numeric file id or http(s) URL).</param>
/// <param name="counterRefs">Site counters the text quotes.</param>
/// <param name="pipeline">The pipeline the document was parsed with.</param>
public class ParsedMarkdown(
	MarkdownDocument document,
	HashSet<(string type, long id)> entityRefs,
	HashSet<long> fileIds,
	HashSet<long> roleIds,
	HashSet<long> videoIds,
	HashSet<string> diagramRefs,
	HashSet<SiteCounters> counterRefs,
	MarkdownPipeline pipeline)
{
	/// <summary>
	/// The parsed markdown syntax tree.
	/// </summary>
	/// <remarks>
	/// Public so a renderer living outside this assembly can walk the tree. Desktop clients render the
	/// document into native controls instead of HTML, and they must not re-parse the text: parsing is
	/// what decides which extension nodes exist at all.
	/// </remarks>
	public MarkdownDocument Document { get; } = document;

	/// <summary>
	/// The pipeline the document was parsed with, so rendering stays consistent with parsing
	/// (notably whether raw HTML was kept or stripped).
	/// </summary>
	public MarkdownPipeline Pipeline { get; } = pipeline;

	/// <summary>
	/// Entity references found in the text as (type, id) pairs.
	/// </summary>
	public HashSet<(string type, long id)> EntityRefs { get; } = entityRefs;

	/// <summary>
	/// Identifiers of the files the text references.
	/// </summary>
	public HashSet<long> FileIds { get; } = fileIds;

	/// <summary>
	/// Identifiers of the roles the text references.
	/// </summary>
	public HashSet<long> RoleIds { get; } = roleIds;

	/// <summary>
	/// Identifiers of the videos the text references.
	/// </summary>
	public HashSet<long> VideoIds { get; } = videoIds;

	/// <summary>
	/// @diagram(dg) references — the raw dg argument (numeric file id or http(s) URL).
	/// </summary>
	public HashSet<string> DiagramRefs { get; } = diagramRefs;

	/// <summary>
	/// The site counters the text quotes ("@connector_count"), so the caller fetches only those.
	/// </summary>
	public HashSet<SiteCounters> CounterRefs { get; } = counterRefs;
}
