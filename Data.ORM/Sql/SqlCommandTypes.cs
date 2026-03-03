namespace Ecng.Data.Sql;

/// <summary>
/// Defines the types of SQL commands.
/// </summary>
public enum SqlCommandTypes
{
	/// <summary>
	/// Insert a new record.
	/// </summary>
	Create,

	/// <summary>
	/// Read a record by key columns.
	/// </summary>
	ReadBy,

	/// <summary>
	/// Read a range of records by key values.
	/// </summary>
	ReadRange,

	/// <summary>
	/// Read all records from a table.
	/// </summary>
	ReadAll,

	/// <summary>
	/// Update a record by key columns.
	/// </summary>
	UpdateBy,

	/// <summary>
	/// Delete a record by key columns.
	/// </summary>
	DeleteBy,

	//DeleteBy,

	/// <summary>
	/// Delete all records from a table.
	/// </summary>
	DeleteAll,

	/// <summary>
	/// Count records in a table.
	/// </summary>
	Count,

	/// <summary>
	/// A custom SQL command.
	/// </summary>
	Custom,
}