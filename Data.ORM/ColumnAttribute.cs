namespace Ecng.Serialization;

/// <summary>
/// Specifies column-level metadata for a mapped property.
/// Controls nullability and maximum length for string/binary columns.
/// </summary>
[AttributeUsage(ReflectionHelper.Members)]
public class ColumnAttribute : Attribute
{
	/// <summary>
	/// Gets or sets whether the column allows NULL values.
	/// When not set, nullability is inferred from the CLR type.
	/// </summary>
	public bool IsNullable { get; set; }

	/// <summary>
	/// Gets or sets the maximum length for string/binary columns.
	/// 0 means unlimited (MAX/TEXT). Applies only to string and byte[] columns.
	/// </summary>
	public int MaxLength { get; set; }
}
