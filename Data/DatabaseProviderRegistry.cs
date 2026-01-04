namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Provides a registry of known database provider names and their factories.
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

	private static readonly Dictionary<string, DbProviderFactory> _factories = [];
	private static readonly object _sync = new();

	/// <summary>
	/// Gets the list of all registered database providers.
	/// </summary>
	public static string[] AllProviders
	{
		get
		{
			lock (_sync)
				return [.. _factories.Keys];
		}
	}

	/// <summary>
	/// Registers a database provider factory.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <param name="factory">Provider factory instance.</param>
	public static void Register(string providerName, DbProviderFactory factory)
	{
		if (providerName.IsEmpty())
			throw new ArgumentNullException(nameof(providerName));

		if (factory is null)
			throw new ArgumentNullException(nameof(factory));

		lock (_sync)
			_factories[providerName] = factory;
	}

	/// <summary>
	/// Unregisters a database provider factory.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <returns><c>true</c> if the provider was removed; otherwise, <c>false</c>.</returns>
	public static bool Unregister(string providerName)
	{
		if (providerName.IsEmpty())
			throw new ArgumentNullException(nameof(providerName));

		lock (_sync)
			return _factories.Remove(providerName);
	}

	/// <summary>
	/// Gets the registered provider factory by name.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <returns>Provider factory instance.</returns>
	/// <exception cref="InvalidOperationException">Provider is not registered.</exception>
	public static DbProviderFactory GetFactory(string providerName)
	{
		if (providerName.IsEmpty())
			throw new ArgumentNullException(nameof(providerName));

		lock (_sync)
		{
			if (_factories.TryGetValue(providerName, out var factory))
				return factory;
		}

		throw new InvalidOperationException($"Provider '{providerName}' is not registered.");
	}

	/// <summary>
	/// Tries to get the registered provider factory by name.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <param name="factory">Provider factory instance if found.</param>
	/// <returns><c>true</c> if the provider was found; otherwise, <c>false</c>.</returns>
	public static bool TryGetFactory(string providerName, out DbProviderFactory factory)
	{
		if (providerName.IsEmpty())
		{
			factory = null;
			return false;
		}

		lock (_sync)
			return _factories.TryGetValue(providerName, out factory);
	}

	/// <summary>
	/// Checks if a provider is registered.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <returns><c>true</c> if the provider is registered; otherwise, <c>false</c>.</returns>
	public static bool IsRegistered(string providerName)
	{
		if (providerName.IsEmpty())
			return false;

		lock (_sync)
			return _factories.ContainsKey(providerName);
	}
}
