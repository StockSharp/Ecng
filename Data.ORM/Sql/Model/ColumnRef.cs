namespace Ecng.Data.Sql.Model;

/// <summary>
/// Resolved reference to a single column qualified by a table or join alias.
/// </summary>
/// <param name="Alias">Alias of the table or join the column belongs to.</param>
/// <param name="Name">Physical column name as it should appear in SQL.</param>
public record ColumnRef(string Alias, string Name);
