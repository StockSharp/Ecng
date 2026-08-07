namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Table.
/// </summary>
/// <param name="rows">Table rows, the first one being the header when <paramref name="hasHeader"/> is set.</param>
/// <param name="hasHeader">Whether the first row is a header.</param>
public sealed class MdTable(IReadOnlyList<IReadOnlyList<MdTableCell>> rows, bool hasHeader) : MdBlock
{
	/// <summary>
	/// Table rows.
	/// </summary>
	public IReadOnlyList<IReadOnlyList<MdTableCell>> Rows { get; } = rows;

	/// <summary>
	/// Whether the first row is a header.
	/// </summary>
	public bool HasHeader { get; } = hasHeader;
}
