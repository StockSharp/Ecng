namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// One figure in a <see cref="MdStats"/> row.
/// </summary>
/// <param name="value">The figure itself.</param>
/// <param name="label">What the figure counts.</param>
public sealed class MdStat(string value, string label)
{
	/// <summary>
	/// The figure itself.
	/// </summary>
	public string Value { get; } = value;

	/// <summary>
	/// What the figure counts.
	/// </summary>
	public string Label { get; } = label;
}

/// <summary>
/// A row of headline figures, one per "value | label" line of a ":::stats" section.
/// </summary>
/// <param name="items">The figures, in the order the author wrote them.</param>
/// <remarks>
/// The pairs are separated here rather than in each host because the split is a reading of the source, not
/// a drawing decision: a host that had to find the bar itself would be free to disagree about what counts
/// as the value, and the two hosts would then show different things from one text.
/// </remarks>
public sealed class MdStats(IReadOnlyList<MdStat> items) : MdBlock
{
	/// <summary>
	/// The figures, in the order the author wrote them.
	/// </summary>
	public IReadOnlyList<MdStat> Items { get; } = items;
}
