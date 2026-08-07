namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Block quote.
/// </summary>
/// <param name="children">Quoted content.</param>
public sealed class MdQuote(IReadOnlyList<MdBlock> children) : MdBlock
{
	/// <summary>
	/// Quoted content.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;
}
