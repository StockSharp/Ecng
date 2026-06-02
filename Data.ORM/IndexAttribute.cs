namespace Ecng.Serialization;

/// <summary>
/// Indicates that a member or type should be indexed in the database.
/// </summary>
[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types, AllowMultiple = true)]
public class IndexAttribute : Attribute
{
	/// <summary>
	/// Initializes a new <see cref="IndexAttribute"/>.
	/// </summary>
	public IndexAttribute()
	{
	}

	/// <summary>
	/// Type-level constructor declaring one whole index (single- or multi-column) over the
	/// named columns, in argument order. Use <c>nameof</c> for compile-safe column names —
	/// e.g. <c>[Index(nameof(Client), nameof(Deleted), nameof(Topic))]</c>. The index name is
	/// auto-generated (<c>IX_{Table}_{col1}_{col2}…</c>) unless <see cref="Name"/> is set, and
	/// no per-column <see cref="Order"/> is needed — order follows the argument order. This is
	/// the preferred way to declare composite indexes and indexes over inherited base columns.
	/// </summary>
	public IndexAttribute(params string[] fieldNames)
	{
		FieldNames = fieldNames;
	}

	/// <summary>
	/// Gets or sets the database field name for the index.
	/// </summary>
	public string FieldName { get; set; }

	/// <summary>
	/// The ordered set of columns this type-level index spans (set via the
	/// <see cref="IndexAttribute(string[])"/> constructor). When it holds more than one column
	/// the columns form a single composite index in this order; with one column it is a
	/// single-column index. Takes precedence over <see cref="FieldName"/> when both are set.
	/// </summary>
	public string[] FieldNames { get; set; }

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
	/// <summary>
	/// Initializes a new <see cref="UniqueAttribute"/>.
	/// </summary>
	public UniqueAttribute()
	{
	}

	/// <summary>
	/// Type-level constructor declaring a unique index over the named columns, in argument
	/// order — e.g. <c>[Unique(nameof(Topic), nameof(Role), nameof(Write))]</c>.
	/// </summary>
	public UniqueAttribute(params string[] fieldNames)
		: base(fieldNames)
	{
	}
}

/// <summary>
/// Indicates that a member or type serves as the identity (primary key) field.
/// </summary>
public class IdentityAttribute : UniqueAttribute
{
}