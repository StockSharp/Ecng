namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// One item of a list.
/// </summary>
/// <param name="children">Item content.</param>
public sealed class MdListItem(IReadOnlyList<MdBlock> children) : MdNode
{
	/// <summary>
	/// Item content.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;
}
