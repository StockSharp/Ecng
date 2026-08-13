namespace Ecng.Markdown;

/// <summary>
/// The data resolved for the references collected in <see cref="ParsedMarkdown"/>:
/// links, access flags, video sources, diagram bodies and counter values keyed by their reference.
/// </summary>
public class ResolvedMarkdownData
{
	/// <summary>
	/// Resolved entity links, grouped by entity type and keyed by entity id.
	/// </summary>
	/// <remarks>
	/// Nested by entity type rather than keyed by a (type, id) pair: a tuple cannot be a JSON key, and this
	/// travels to clients that render the text themselves.
	/// </remarks>
	public Dictionary<string, Dictionary<long, MarkdownLink>> Entities { get; init; } = [];

	/// <summary>
	/// Resolved file links keyed by file id.
	/// </summary>
	public Dictionary<long, MarkdownLink> Files { get; init; } = [];

	/// <summary>
	/// Whether the current reader has each referenced role, keyed by role id.
	/// </summary>
	public Dictionary<long, bool> Roles { get; init; } = [];

	/// <summary>
	/// Resolved video sources keyed by video id.
	/// </summary>
	public Dictionary<long, ResolvedVideo> Videos { get; init; } = [];

	/// <summary>
	/// Diagram bodies keyed by the raw diagram reference.
	/// </summary>
	public Dictionary<string, string> Diagrams { get; init; } = [];

	/// <summary>
	/// Counter values keyed by counter.
	/// </summary>
	/// <remarks>
	/// Already formatted for the language being rendered: the count is a number, but how it reads (grouping
	/// separators) belongs to the language, which only the caller knows.
	/// </remarks>
	public Dictionary<SiteCounters, string> Counters { get; init; } = [];
}
