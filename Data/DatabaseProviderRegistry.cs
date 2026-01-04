namespace Ecng.Data;

using System;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Provides a registry of known database provider names.
/// </summary>
public static class DatabaseProviderRegistry
{
	/// <summary>
	/// SQL Server provider name.
	/// </summary>
	public const string SqlServer = "SqlServer";

	/// <summary>
	/// SQLite provider name.
	/// </summary>
	public const string SQLite = "SQLite";

	/// <summary>
	/// MySQL provider name.
	/// </summary>
	public const string MySql = "MySql";

	/// <summary>
	/// PostgreSQL provider name.
	/// </summary>
	public const string PostgreSql = "PostgreSQL";

	private static string[] _allProviders =
	[
		SqlServer,
		SQLite,
		MySql,
		PostgreSql,
	];

	/// <summary>
	/// Gets or sets the list of all available database providers.
	/// </summary>
	public static string[] AllProviders
	{
		get => _allProviders;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value.Length == 0)
				throw new ArgumentException("Providers list cannot be empty.", nameof(value));

			if (value.Any(p => p.IsEmpty()))
				throw new ArgumentException("Provider name cannot be empty.", nameof(value));

			_allProviders = value;
		}
	}
}
