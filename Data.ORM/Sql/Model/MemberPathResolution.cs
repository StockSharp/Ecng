namespace Ecng.Data.Sql.Model;

/// <summary>
/// Result of resolving a chain of member accesses against a schema:
/// the qualified column to emit and the joins that must be registered
/// for that alias to be reachable in the final SQL.
/// </summary>
/// <param name="Column">Qualified reference to the leaf column.</param>
/// <param name="RequiredJoins">
/// Joins to register, in dependency order (parent before child). Empty when
/// the column lives directly on the root entity.
/// </param>
public record MemberPathResolution(ColumnRef Column, IReadOnlyList<JoinPlan> RequiredJoins);
