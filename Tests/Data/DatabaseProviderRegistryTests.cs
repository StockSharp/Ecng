namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;

using Microsoft.Data.Sqlite;

[TestClass]
public class DatabaseProviderRegistryTests : BaseTestClass
{
	/// <summary>
	/// Generates a unique provider name per test to avoid cross-test interference
	/// in the static <see cref="DatabaseProviderRegistry"/>.
	/// </summary>
	private string UniqueProvider([System.Runtime.CompilerServices.CallerMemberName] string caller = null)
		=> $"test_{caller}_{Guid.NewGuid():N}";

	/// <summary>
	/// Safely cleans up a provider registration if it exists.
	/// </summary>
	private static void TryUnregister(string providerName)
	{
		DatabaseProviderRegistry.Unregister(providerName);
	}

	#region Register / GetFactory

	[TestMethod]
	public void Register_GetFactory_ReturnsSameInstance()
	{
		var name = UniqueProvider();
		try
		{
			var factory = SqliteFactory.Instance;

			DatabaseProviderRegistry.Register(name, factory);
			var result = DatabaseProviderRegistry.GetFactory(name);

			AreSame(factory, result);
		}
		finally
		{
			TryUnregister(name);
		}
	}

	#endregion

	#region Register Duplicate

	[TestMethod]
	public void Register_Duplicate_UpdatesFactory()
	{
		var name = UniqueProvider();
		try
		{
			var factory1 = SqliteFactory.Instance;
			DatabaseProviderRegistry.Register(name, factory1);

			// Register again with the same name - should overwrite, not throw.
			var factory2 = SqliteFactory.Instance;
			DatabaseProviderRegistry.Register(name, factory2);

			var result = DatabaseProviderRegistry.GetFactory(name);
			AreSame(factory2, result);
		}
		finally
		{
			TryUnregister(name);
		}
	}

	[TestMethod]
	public void Register_NullProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.Register(null, SqliteFactory.Instance));
	}

	[TestMethod]
	public void Register_EmptyProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.Register(string.Empty, SqliteFactory.Instance));
	}

	[TestMethod]
	public void Register_NullFactory_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.Register("some_provider", null));
	}

	#endregion

	#region Unregister

	[TestMethod]
	public void Unregister_RegisteredProvider_ReturnsTrue()
	{
		var name = UniqueProvider();
		DatabaseProviderRegistry.Register(name, SqliteFactory.Instance);

		var removed = DatabaseProviderRegistry.Unregister(name);
		IsTrue(removed);
	}

	[TestMethod]
	public void Unregister_ThenGetFactory_Throws()
	{
		var name = UniqueProvider();
		DatabaseProviderRegistry.Register(name, SqliteFactory.Instance);
		DatabaseProviderRegistry.Unregister(name);

		Throws<InvalidOperationException>(() => DatabaseProviderRegistry.GetFactory(name));
	}

	[TestMethod]
	public void Unregister_NonExistent_ReturnsFalse()
	{
		var name = UniqueProvider();
		var removed = DatabaseProviderRegistry.Unregister(name);
		IsFalse(removed);
	}

	[TestMethod]
	public void Unregister_NullProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.Unregister(null));
	}

	#endregion

	#region GetFactory Unknown

	[TestMethod]
	public void GetFactory_UnknownProvider_ThrowsInvalidOperation()
	{
		var name = UniqueProvider();
		var ex = Throws<InvalidOperationException>(() => DatabaseProviderRegistry.GetFactory(name));
		Contains(name, ex.Message);
	}

	[TestMethod]
	public void GetFactory_NullProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.GetFactory(null));
	}

	[TestMethod]
	public void GetFactory_EmptyProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.GetFactory(string.Empty));
	}

	#endregion

	#region TryGetFactory

	[TestMethod]
	public void TryGetFactory_Registered_ReturnsTrueAndFactory()
	{
		var name = UniqueProvider();
		try
		{
			DatabaseProviderRegistry.Register(name, SqliteFactory.Instance);

			var found = DatabaseProviderRegistry.TryGetFactory(name, out var factory);

			IsTrue(found);
			AreSame(SqliteFactory.Instance, factory);
		}
		finally
		{
			TryUnregister(name);
		}
	}

	[TestMethod]
	public void TryGetFactory_NotRegistered_ReturnsFalse()
	{
		var name = UniqueProvider();

		var found = DatabaseProviderRegistry.TryGetFactory(name, out var factory);

		IsFalse(found);
		IsNull(factory);
	}

	[TestMethod]
	public void TryGetFactory_NullProviderName_ReturnsFalse()
	{
		var found = DatabaseProviderRegistry.TryGetFactory(null, out var factory);

		IsFalse(found);
		IsNull(factory);
	}

	[TestMethod]
	public void TryGetFactory_EmptyProviderName_ReturnsFalse()
	{
		var found = DatabaseProviderRegistry.TryGetFactory(string.Empty, out var factory);

		IsFalse(found);
		IsNull(factory);
	}

	#endregion

	#region IsRegistered

	[TestMethod]
	public void IsRegistered_RegisteredProvider_ReturnsTrue()
	{
		var name = UniqueProvider();
		try
		{
			DatabaseProviderRegistry.Register(name, SqliteFactory.Instance);

			IsTrue(DatabaseProviderRegistry.IsRegistered(name));
		}
		finally
		{
			TryUnregister(name);
		}
	}

	[TestMethod]
	public void IsRegistered_NotRegistered_ReturnsFalse()
	{
		var name = UniqueProvider();
		IsFalse(DatabaseProviderRegistry.IsRegistered(name));
	}

	[TestMethod]
	public void IsRegistered_NullProviderName_ReturnsFalse()
	{
		IsFalse(DatabaseProviderRegistry.IsRegistered(null));
	}

	[TestMethod]
	public void IsRegistered_EmptyProviderName_ReturnsFalse()
	{
		IsFalse(DatabaseProviderRegistry.IsRegistered(string.Empty));
	}

	[TestMethod]
	public void IsRegistered_AfterUnregister_ReturnsFalse()
	{
		var name = UniqueProvider();
		DatabaseProviderRegistry.Register(name, SqliteFactory.Instance);
		DatabaseProviderRegistry.Unregister(name);

		IsFalse(DatabaseProviderRegistry.IsRegistered(name));
	}

	#endregion

	#region AllProviders

	[TestMethod]
	public void AllProviders_ContainsRegisteredProviders()
	{
		var name1 = UniqueProvider() + "_a";
		var name2 = UniqueProvider() + "_b";
		try
		{
			DatabaseProviderRegistry.Register(name1, SqliteFactory.Instance);
			DatabaseProviderRegistry.Register(name2, SqliteFactory.Instance);

			var all = DatabaseProviderRegistry.AllProviders;

			Contains(all, name1);
			Contains(all, name2);
		}
		finally
		{
			TryUnregister(name1);
			TryUnregister(name2);
		}
	}

	[TestMethod]
	public void AllProviders_DoesNotContainUnregistered()
	{
		var name = UniqueProvider();
		DatabaseProviderRegistry.Register(name, SqliteFactory.Instance);
		DatabaseProviderRegistry.Unregister(name);

		var all = DatabaseProviderRegistry.AllProviders;

		DoesNotContain(all, name);
	}

	#endregion

	#region RegisterDialect / GetDialect

	[TestMethod]
	public void RegisterDialect_GetDialect_ReturnsSameInstance()
	{
		var name = UniqueProvider();
		try
		{
			var dialect = SQLiteDialect.Instance;

			DatabaseProviderRegistry.RegisterDialect(name, dialect);
			var result = DatabaseProviderRegistry.GetDialect(name);

			AreSame(dialect, result);
		}
		finally
		{
			// Note: no UnregisterDialect API, but unique names prevent interference.
		}
	}

	[TestMethod]
	public void RegisterDialect_NullProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.RegisterDialect(null, SQLiteDialect.Instance));
	}

	[TestMethod]
	public void RegisterDialect_NullDialect_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.RegisterDialect("some_provider", null));
	}

	[TestMethod]
	public void RegisterDialect_Duplicate_UpdatesDialect()
	{
		var name = UniqueProvider();
		var dialect1 = SQLiteDialect.Instance;
		var dialect2 = SqlServerDialect.Instance;

		DatabaseProviderRegistry.RegisterDialect(name, dialect1);
		DatabaseProviderRegistry.RegisterDialect(name, dialect2);

		var result = DatabaseProviderRegistry.GetDialect(name);
		AreSame(dialect2, result);
	}

	#endregion

	#region TryGetDialect

	[TestMethod]
	public void TryGetDialect_Registered_ReturnsTrueAndDialect()
	{
		var name = UniqueProvider();
		DatabaseProviderRegistry.RegisterDialect(name, SQLiteDialect.Instance);

		var found = DatabaseProviderRegistry.TryGetDialect(name, out var dialect);

		IsTrue(found);
		AreSame(SQLiteDialect.Instance, dialect);
	}

	[TestMethod]
	public void TryGetDialect_NotRegistered_ReturnsFalse()
	{
		var name = UniqueProvider();

		var found = DatabaseProviderRegistry.TryGetDialect(name, out var dialect);

		IsFalse(found);
		IsNull(dialect);
	}

	[TestMethod]
	public void TryGetDialect_NullProviderName_ReturnsFalse()
	{
		var found = DatabaseProviderRegistry.TryGetDialect(null, out var dialect);

		IsFalse(found);
		IsNull(dialect);
	}

	[TestMethod]
	public void TryGetDialect_EmptyProviderName_ReturnsFalse()
	{
		var found = DatabaseProviderRegistry.TryGetDialect(string.Empty, out var dialect);

		IsFalse(found);
		IsNull(dialect);
	}

	#endregion

	#region GetDialect Unknown

	[TestMethod]
	public void GetDialect_UnknownProvider_ThrowsInvalidOperation()
	{
		var name = UniqueProvider();
		var ex = Throws<InvalidOperationException>(() => DatabaseProviderRegistry.GetDialect(name));
		Contains(name, ex.Message);
	}

	[TestMethod]
	public void GetDialect_NullProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.GetDialect(null));
	}

	[TestMethod]
	public void GetDialect_EmptyProviderName_Throws()
	{
		Throws<ArgumentNullException>(() => DatabaseProviderRegistry.GetDialect(string.Empty));
	}

	#endregion
}
