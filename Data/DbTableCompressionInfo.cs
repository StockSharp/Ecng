namespace Ecng.Data;

/// <summary>
/// How a table found in a live database is packed, read via <see cref="ISqlDialect.ReadDbCompressionsAsync"/>.
/// </summary>
/// <param name="TableName">Name of the table.</param>
/// <param name="Compression">The packing the table currently uses.</param>
public record DbTableCompressionInfo(string TableName, DataCompressions Compression);
