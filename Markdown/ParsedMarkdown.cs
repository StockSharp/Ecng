namespace Ecng.Markdown;

using Markdig;
using Markdig.Syntax;

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
	// Public so a renderer living outside this assembly can walk the tree. Desktop clients render the
	// document into native controls instead of HTML, and they must not re-parse the text: parsing is
	// what decides which extension nodes exist at all.
	public MarkdownDocument Document { get; } = document;

	// The pipeline the document was parsed with, so rendering stays consistent with parsing
	// (notably whether raw HTML was kept or stripped).
	public MarkdownPipeline Pipeline { get; } = pipeline;

	public HashSet<(string type, long id)> EntityRefs { get; } = entityRefs;
	public HashSet<long> FileIds { get; } = fileIds;
	public HashSet<long> RoleIds { get; } = roleIds;
	public HashSet<long> VideoIds { get; } = videoIds;

	// @diagram(dg) references — the raw dg argument (numeric file id or http(s) URL).
	public HashSet<string> DiagramRefs { get; } = diagramRefs;

	// The site counters the text quotes ("@connector_count"), so the caller fetches only those.
	public HashSet<SiteCounters> CounterRefs { get; } = counterRefs;
}
