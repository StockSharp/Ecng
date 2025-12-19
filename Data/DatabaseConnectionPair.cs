namespace Ecng.Data;

using System;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Serialization;

/// <summary>
/// Provider and connection string pair.
/// </summary>
public class DatabaseConnectionPair : NotifiableObject, IPersistable
{
	private string _provider;

	/// <summary>
	/// Provider type.
	/// </summary>
	public string Provider
	{
		get => _provider;
		set
		{
			_provider = value;
			UpdateTitle();
		}
	}

	private string _connectionString;

	/// <summary>
	/// Connection settings.
	/// </summary>
	public string ConnectionString
	{
		get => _connectionString;
		set
		{
			_connectionString = value;
			UpdateTitle();
		}
	}

	/// <summary>
	/// Connection title.
	/// </summary>
	public string Title => $"({Provider}) {ConnectionString}";

	private void UpdateTitle()
	{
		NotifyChanged(nameof(Title));
	}

	/// <inheritdoc />
	public override string ToString() => Title;

	void IPersistable.Load(SettingsStorage storage)
	{
		Provider = storage.GetValue<string>(nameof(Provider));
		ConnectionString = storage.GetValue<string>(nameof(ConnectionString));
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Provider), Provider)
			.Set(nameof(ConnectionString), ConnectionString)
			;
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is null)
			return false;

		if (ReferenceEquals(this, obj))
			return true;

		if (obj is not DatabaseConnectionPair other)
			return false;

		return
			Provider.EqualsIgnoreCase(other.Provider) &&
			ConnectionString.EqualsIgnoreCase(other.ConnectionString);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = hash * 23 + (Provider is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Provider));
			hash = hash * 23 + (ConnectionString is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(ConnectionString));
			return hash;
		}
	}
}