namespace Ecng.Data;

using System;

using LinqToDB;

/// <summary>
/// Provides a registry for database providers and verifies database connections.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DatabaseProviderRegistry"/> class.
/// </remarks>
/// <param name="batchInserterProvider">The batch inserter provider to use for verification.</param>
public class DatabaseProviderRegistry(IDatabaseBatchInserterProvider batchInserterProvider) : IDatabaseProviderRegistry
{
	private readonly IDatabaseBatchInserterProvider _batchInserterProvider = batchInserterProvider ?? throw new ArgumentNullException(nameof(batchInserterProvider));

	/// <summary>
	/// Initializes a new instance of the <see cref="DatabaseProviderRegistry"/> class.
	/// </summary>
	public DatabaseProviderRegistry()
		: this(new Linq2dbBatchInserterProvider())
	{
	}

	/// <summary>
	/// Gets the list of available database providers.
	/// </summary>
	public virtual string[] Providers { get; } =
	[
		ProviderName.SqlServer,
		ProviderName.SQLite,
		ProviderName.MySql,
	];
}