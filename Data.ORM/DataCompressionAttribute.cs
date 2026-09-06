namespace Ecng.Serialization;

using Ecng.Data;

/// <summary>
/// Declares how a table's rows are packed on disk.
/// </summary>
/// <remarks>
/// A table of narrow, fixed-width columns whose values repeat - a log keyed by identifiers, a visit
/// counter - stores several times the bytes it needs, and packing costs only processor time on read.
/// Content held out of row (large text and binary) is unaffected, so declaring it there gains nothing.
/// Not every database has the concept; where it does not, the declaration is ignored.
/// </remarks>
/// <param name="compression">The packing this table should use.</param>
[AttributeUsage(ReflectionHelper.Types)]
public class DataCompressionAttribute(DataCompressions compression) : Attribute
{
	/// <summary>
	/// The packing this table should use.
	/// </summary>
	public DataCompressions Compression { get; } = compression;
}
