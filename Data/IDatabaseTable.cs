namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a database table with DDL and DML operations.
/// </summary>
public interface IDatabaseTable
{
	/// <summary>
	/// Gets the table name.
	/// </summary>
	string Name { get; }

	#region DDL

	/// <summary>
	/// Creates the table if it doesn't exist.
	/// </summary>
	/// <param name="columns">Column definitions (name -> C# type).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task CreateAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken);

	/// <summary>
	/// Modifies the table structure by adding columns.
	/// </summary>
	/// <param name="columns">Column definitions to add (name -> C# type).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task ModifyAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken);

	/// <summary>
	/// Drops the table if it exists.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task DropAsync(CancellationToken cancellationToken);

	#endregion

	#region DML

	/// <summary>
	/// Inserts a single row.
	/// </summary>
	/// <param name="values">Column values (name -> value).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task InsertAsync(IDictionary<string, object> values, CancellationToken cancellationToken);

	/// <summary>
	/// Inserts multiple rows.
	/// </summary>
	/// <param name="rows">Collection of rows, each row is column values (name -> value).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task BulkInsertAsync(IEnumerable<IDictionary<string, object>> rows, CancellationToken cancellationToken);

	/// <summary>
	/// Selects rows from the table.
	/// </summary>
	/// <param name="filters">Filter conditions.</param>
	/// <param name="orderBy">Order by conditions.</param>
	/// <param name="skip">Number of rows to skip.</param>
	/// <param name="take">Number of rows to take.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Selected rows as dictionaries (column name -> value).</returns>
	Task<IEnumerable<IDictionary<string, object>>> SelectAsync(IEnumerable<FilterCondition> filters, IEnumerable<OrderByCondition> orderBy, long? skip, long? take, CancellationToken cancellationToken);

	/// <summary>
	/// Updates rows in the table.
	/// </summary>
	/// <param name="values">Column values to set (name -> value).</param>
	/// <param name="filters">Filter conditions to identify rows to update.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task UpdateAsync(IDictionary<string, object> values, IEnumerable<FilterCondition> filters, CancellationToken cancellationToken);

	/// <summary>
	/// Deletes rows from the table.
	/// </summary>
	/// <param name="filters">Filter conditions to identify rows to delete.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Number of deleted rows.</returns>
	Task<int> DeleteAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken);

	/// <summary>
	/// Inserts a row if it doesn't exist, or updates it if it does (MERGE/UPSERT).
	/// </summary>
	/// <param name="values">Column values (name -> value).</param>
	/// <param name="keyColumns">Key column names used to determine if the row exists.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task UpsertAsync(IDictionary<string, object> values, IEnumerable<string> keyColumns, CancellationToken cancellationToken);

	#endregion
}
