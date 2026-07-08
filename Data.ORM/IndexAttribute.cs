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

	/// <summary>
	/// Optional predicate turning this into a partial (filtered) index — emitted as a trailing
	/// <c>WHERE</c> on the <c>CREATE INDEX</c>. Column names must be wrapped in braces so they are
	/// quoted per dialect, e.g. <c>Condition = "{IdempotencyKey} &lt;&gt; ''"</c> becomes
	/// <c>WHERE [IdempotencyKey] &lt;&gt; ''</c> (SQL Server) / <c>WHERE "IdempotencyKey" &lt;&gt; ''</c>
	/// (PostgreSQL / SQLite). The chief use is a unique index that must ignore rows carrying the
	/// "unset" value of a key (empty string, sentinel), which a plain unique index would wrongly
	/// collapse. For a composite index set it on any one participating member. <see langword="null"/>
	/// emits an ordinary, non-filtered index.
	/// </summary>
	public string Condition { get; set; }
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
/// A partial unique index that ignores rows whose LAST key column holds the empty string — the
/// common "unique per scope, but only for rows that actually carry a key" case (e.g. an idempotency
/// key defaulting to empty). Equivalent to <c>[Unique(fields…)]</c> with
/// <see cref="IndexAttribute.Condition"/> = <c>"{lastField} &lt;&gt; ''"</c>, but typed and named so
/// the raw predicate lives here once instead of as a string literal on every entity. For any other
/// filter use the general <see cref="IndexAttribute.Condition"/> directly.
/// </summary>
public sealed class NonEmptyUniqueAttribute : UniqueAttribute
{
	/// <summary>
	/// Declares the unique index over <paramref name="fieldName"/>, filtered to rows whose
	/// column is non-empty.
	/// </summary>
	public NonEmptyUniqueAttribute(string fieldName)
		: this(fieldName is null ? null : new[] { fieldName })
	{
	}

	/// <summary>
	/// Declares the unique index over <paramref name="fieldNames"/>, filtered to rows whose last
	/// column is non-empty. Scope columns come first, the value-carrying key last.
	/// </summary>
	public NonEmptyUniqueAttribute(params string[] fieldNames)
		: base(fieldNames)
	{
		if (fieldNames is not { Length: > 0 })
			throw new ArgumentException("NonEmptyUnique requires at least one field.", nameof(fieldNames));

		// Braces are quoted per dialect by the migrator; only the last column is filtered.
		Condition = $"{{{fieldNames[^1]}}} <> ''";
	}
}

/// <summary>
/// Indicates that a member or type serves as the identity (primary key) field.
/// </summary>
public class IdentityAttribute : UniqueAttribute
{
}
