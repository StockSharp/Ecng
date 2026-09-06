namespace Ecng.Data;

/// <summary>
/// How a table's rows are packed on disk.
/// </summary>
public enum DataCompressions
{
	/// <summary>
	/// Rows are stored as written.
	/// </summary>
	None,

	/// <summary>
	/// Values are packed within each row: fixed-width types keep only the bytes they use.
	/// </summary>
	Row,

	/// <summary>
	/// Row packing plus a per-page dictionary of repeated values and common column prefixes.
	/// </summary>
	Page,
}
