namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// One table cell.
/// </summary>
/// <param name="children">Cell content.</param>
public sealed class MdTableCell(IReadOnlyList<MdBlock> children) : MdNode
{
	/// <summary>
	/// Cell content.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;
}
