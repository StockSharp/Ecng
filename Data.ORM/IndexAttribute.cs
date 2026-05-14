namespace Ecng.Serialization;

/// <summary>
/// Indicates that a member or type should be indexed in the database.
/// </summary>
[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types, AllowMultiple = true)]
public class IndexAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the database field name for the index.
	/// </summary>
	public string FieldName { get; set; }

	/// <summary>
	/// Gets or sets whether null values should be cached for this index.
	/// </summary>
	public bool CacheNull { get; set; }

	/// <summary>
	/// Optional shared name used to group columns into a composite index.
	/// Two or more properties carrying the same <see cref="Name"/> are
	/// emitted as a single multi-column <c>CREATE INDEX</c>; columns are
	/// ordered by <see cref="Order"/> within the index. When left
	/// <see langword="null"/> the column gets its own single-column index
	/// named <c>IX_{Table}_{Column}</c>.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Position of this column inside a composite index (lowest first).
	/// Ignored when <see cref="Name"/> is <see langword="null"/>.
	/// </summary>
	public int Order { get; set; }
}

/// <summary>
/// Indicates that a member or type has a unique index constraint.
/// </summary>
public class UniqueAttribute : IndexAttribute
{
}

/// <summary>
/// Indicates that a member or type serves as the identity (primary key) field.
/// </summary>
public class IdentityAttribute : UniqueAttribute
{
}