namespace Ecng.Data.Sql.Model;

/// <summary>
/// Kind of SQL join produced by the resolver when traversing relations.
/// </summary>
public enum JoinKind
{
	/// <summary>
	/// INNER JOIN — only rows with a matching counterpart on both sides.
	/// </summary>
	Inner,

	/// <summary>
	/// LEFT OUTER JOIN — keeps rows from the parent side even with no match.
	/// </summary>
	LeftOuter,
}
