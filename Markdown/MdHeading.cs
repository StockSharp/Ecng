namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Heading.
/// </summary>
/// <param name="level">Heading level, 1 through 6.</param>
/// <param name="children">Heading content.</param>
public sealed class MdHeading(int level, IReadOnlyList<MdInline> children) : MdBlock
{
	/// <summary>
	/// Heading level, 1 through 6.
	/// </summary>
	public int Level { get; } = level;

	/// <summary>
	/// Heading content.
	/// </summary>
	public IReadOnlyList<MdInline> Children { get; } = children;
}
