namespace Ecng.Markdown;

public class ResolvedMarkdownData
{
	public Dictionary<(string type, long id), (string url, string name, string description)> Entities { get; init; } = [];
	public Dictionary<long, (string url, string name, string description)> Files { get; init; } = [];
	public Dictionary<long, bool> Roles { get; init; } = [];
	public Dictionary<long, ResolvedVideo> Videos { get; init; } = [];
	public Dictionary<string, string> Diagrams { get; init; } = [];

	// Already formatted for the language being rendered: the count is a number, but how it reads (grouping
	// separators) belongs to the language, which only the caller knows.
	public Dictionary<SiteCounters, string> Counters { get; init; } = [];
}
