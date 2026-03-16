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
	/// Gets or sets the maximum length for string/binary columns.
	/// 0 means unlimited (MAX/TEXT). Applies only to string and byte[] columns.
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
