namespace Ecng.Markdown;

public class ResolvedMarkdownData
{
	// Nested by entity type rather than keyed by a (type, id) pair: a tuple cannot be a JSON key, and this
	// travels to clients that render the text themselves.
	public Dictionary<string, Dictionary<long, MarkdownLink>> Entities { get; init; } = [];

	public Dictionary<long, MarkdownLink> Files { get; init; } = [];
	public Dictionary<long, bool> Roles { get; init; } = [];
	public Dictionary<long, ResolvedVideo> Videos { get; init; } = [];
	public Dictionary<string, string> Diagrams { get; init; } = [];

	// Already formatted for the language being rendered: the count is a number, but how it reads (grouping
	// separators) belongs to the language, which only the caller knows.
	public Dictionary<SiteCounters, string> Counters { get; init; } = [];
}
