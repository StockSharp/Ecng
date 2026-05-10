namespace Ecng.Serialization;

/// <summary>
/// Specifies column-level metadata for a mapped property.
/// Controls nullability and maximum length for string/binary columns.
/// </summary>
[AttributeUsage(ReflectionHelper.Members)]
public class ColumnAttribute : Attribute
{
	private bool _isNullableValue;
	private bool _isNullableSet;

	/// <summary>
	/// Gets or sets whether the column allows NULL values.
	/// When not explicitly set, nullability is inferred from the CLR type.
	/// </summary>
	public bool IsNullable
	{
		get => _isNullableValue;
		set { _isNullableValue = value; _isNullableSet = true; }
	}

	/// <summary>
	/// Gets whether <see cref="IsNullable"/> was explicitly set.
	/// </summary>
	public bool IsNullableSet => _isNullableSet;

	/// <summary>
	/// Sentinel to request the dialect-specific unbounded string/binary type
	/// (NVARCHAR(MAX) on SQL Server, TEXT on PostgreSQL/SQLite). Use when a
	/// column is intentionally large — JSON blobs, exception bodies, raw
	/// payloads — to make that intent explicit in the entity, instead of
	/// leaving <see cref="MaxLength"/> at its 0 default.
	/// </summary>
	public const int Max = int.MaxValue;

	/// <summary>
	/// Gets or sets the maximum length for string/binary columns.
	/// 0 (the default) and <see cref="Max"/> both map to the dialect-specific
	/// unbounded type (NVARCHAR(MAX) / TEXT). Set to a positive integer for
	/// a fixed-size column (NVARCHAR(N) / VARCHAR(N)). Applies only to string
	/// and byte[] columns.
	/// </summary>
	public int MaxLength { get; set; }

	/// <summary>
	/// Gets or sets the numeric precision for decimal/numeric columns.
	/// 0 means use the dialect default.
	/// </summary>
	public int Precision { get; set; }

	/// <summary>
	/// Gets or sets the numeric scale for decimal/numeric columns.
	/// 0 means use the dialect default.
	/// </summary>
	public int Scale { get; set; }
}
