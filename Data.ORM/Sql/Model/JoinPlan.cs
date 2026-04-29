namespace Ecng.Data.Sql.Model;

/// <summary>
/// Description of a single join the translator must add to the FROM-clause
/// for a navigation traversal to be expressible in SQL.
/// </summary>
/// <param name="Kind">Inner or left outer join.</param>
/// <param name="Table">Physical table name on the right side of the join.</param>
/// <param name="Alias">Alias assigned to the joined table.</param>
/// <param name="ParentAlias">Alias on the left side that owns the FK column.</param>
/// <param name="OnParentColumn">Column on the parent side used in ON clause.</param>
/// <param name="OnChildColumn">Column on the joined table used in ON clause (typically the PK).</param>
public record JoinPlan(
	JoinKind Kind,
	string Table,
	string Alias,
	string ParentAlias,
	string OnParentColumn,
	string OnChildColumn);
