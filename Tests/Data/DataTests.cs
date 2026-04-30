namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Serialization;

[TestClass]
public class DataTests : BaseTestClass
{
	[TestMethod]
	public void AddRemove()
	{
		var cache = new DatabaseConnectionCache();
		var created = 0;
		var deleted = 0;
		cache.ConnectionCreated += p => created++;
		cache.ConnectionDeleted += p => deleted++;
		var pair = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "123");
		cache.Connections.Count().AssertEqual(1);
		created.AssertEqual(1);
		deleted.AssertEqual(0);
		cache.DeleteConnection(pair);
		cache.Connections.Count().AssertEqual(0);
		created.AssertEqual(1);
		deleted.AssertEqual(1);
	}

	[TestMethod]
	public void SaveLoad()
	{
		var cache = new DatabaseConnectionCache();
		var pair = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "123");
		var ser = new JsonSerializer<DatabaseConnectionCache>();
		var cache2 = ser.Deserialize(ser.Serialize(cache));

		cache2.Connections.Count().AssertEqual(cache.Connections.Count());
		var pair2 = cache2.Connections.First();
		pair2.Provider.AssertEqual(pair.Provider);
		pair2.ConnectionString.AssertEqual(pair.ConnectionString);
	}

	[TestMethod]
	public void GetOrAddIsIdempotent()
	{
		var cache = new DatabaseConnectionCache();
		var created = 0;
		cache.ConnectionCreated += p => created++;

		var first = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "Server=localhost");
		var second = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "Server=localhost");

		AreSame(first, second);
		cache.Connections.Count().AssertEqual(1);
		created.AssertEqual(1);
	}

	[TestMethod]
	public void GetOrAddIsCaseInsensitive()
	{
		var cache = new DatabaseConnectionCache();
		var first = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "Server=Localhost");
		var second = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer.ToLower(), "server=localhost");

		AreSame(first, second);
		cache.Connections.Count().AssertEqual(1);
	}

	[TestMethod]
	public void GetOrAddDifferentConnectionStringsSameProvider()
	{
		var cache = new DatabaseConnectionCache();
		var a = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "Server=A");
		var b = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "Server=B");

		AreNotSame(a, b);
		cache.Connections.Count().AssertEqual(2);
	}

	[TestMethod]
	public void GetOrAddDifferentProvidersSameConnectionString()
	{
		var cache = new DatabaseConnectionCache();
		var a = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "shared");
		var b = cache.GetOrAdd(DatabaseProviderRegistry.SQLite, "shared");

		AreNotSame(a, b);
		cache.Connections.Count().AssertEqual(2);
	}

	[TestMethod]
	public void GetOrAddNullProviderThrows()
	{
		var cache = new DatabaseConnectionCache();
		ThrowsExactly<ArgumentNullException>(() => cache.GetOrAdd(null, "cs"));
	}

	[TestMethod]
	public void GetOrAddEmptyProviderThrows()
	{
		var cache = new DatabaseConnectionCache();
		ThrowsExactly<ArgumentNullException>(() => cache.GetOrAdd(string.Empty, "cs"));
	}

	[TestMethod]
	public void GetOrAddNullConnectionStringThrows()
	{
		var cache = new DatabaseConnectionCache();
		ThrowsExactly<ArgumentNullException>(() => cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, null));
	}

	[TestMethod]
	public void GetOrAddEmptyConnectionStringThrows()
	{
		var cache = new DatabaseConnectionCache();
		ThrowsExactly<ArgumentNullException>(() => cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, string.Empty));
	}

	[TestMethod]
	public void DeleteConnectionNullThrows()
	{
		var cache = new DatabaseConnectionCache();
		ThrowsExactly<ArgumentNullException>(() => cache.DeleteConnection(null));
	}

	[TestMethod]
	public void DeleteConnectionTwiceFiresEventOnce()
	{
		var cache = new DatabaseConnectionCache();
		var deleted = 0;
		cache.ConnectionDeleted += p => deleted++;

		var pair = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "cs");

		var firstResult = cache.DeleteConnection(pair);
		var secondResult = cache.DeleteConnection(pair);

		firstResult.AssertTrue();
		secondResult.AssertFalse();
		deleted.AssertEqual(1);
		cache.Connections.Count().AssertEqual(0);
	}

	[TestMethod]
	public void DeleteConnectionUnknownReturnsFalse()
	{
		var cache = new DatabaseConnectionCache();
		var deleted = 0;
		cache.ConnectionDeleted += p => deleted++;

		var orphan = new DatabaseConnectionPair
		{
			Provider = DatabaseProviderRegistry.SqlServer,
			ConnectionString = "not-in-cache",
		};

		cache.DeleteConnection(orphan).AssertFalse();
		deleted.AssertEqual(0);
	}

	[TestMethod]
	public void UpdatedEventFiresOnAddAndDelete()
	{
		var cache = new DatabaseConnectionCache();
		var updated = 0;
		cache.Updated += () => updated++;

		var pair = cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "cs");
		updated.AssertEqual(1);

		cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "cs");
		updated.AssertEqual(1);

		cache.DeleteConnection(pair);
		updated.AssertEqual(2);

		cache.DeleteConnection(pair);
		updated.AssertEqual(2);
	}

	[TestMethod]
	public void ConnectionCreatedReceivesCorrectPair()
	{
		var cache = new DatabaseConnectionCache();
		DatabaseConnectionPair received = null;
		cache.ConnectionCreated += p => received = p;

		var pair = cache.GetOrAdd(DatabaseProviderRegistry.SQLite, "Data Source=test.db");

		AreSame(pair, received);
	}

	[TestMethod]
	public void ConnectionDeletedReceivesCorrectPair()
	{
		var cache = new DatabaseConnectionCache();
		DatabaseConnectionPair received = null;
		cache.ConnectionDeleted += p => received = p;

		var pair = cache.GetOrAdd(DatabaseProviderRegistry.SQLite, "Data Source=test.db");
		cache.DeleteConnection(pair);

		AreSame(pair, received);
	}

	[TestMethod]
	public void SaveLoadEmptyCache()
	{
		var cache = new DatabaseConnectionCache();
		var ser = new JsonSerializer<DatabaseConnectionCache>();

		var restored = ser.Deserialize(ser.Serialize(cache));

		IsNotNull(restored);
		restored.Connections.Count().AssertEqual(0);
	}

	[TestMethod]
	public void SaveLoadMultipleConnectionsRoundTrip()
	{
		var cache = new DatabaseConnectionCache();
		cache.GetOrAdd(DatabaseProviderRegistry.SqlServer, "Server=A");
		cache.GetOrAdd(DatabaseProviderRegistry.SQLite, "Data Source=b.db");
		cache.GetOrAdd(DatabaseProviderRegistry.PostgreSql, "Host=c");

		var ser = new JsonSerializer<DatabaseConnectionCache>();
		var restored = ser.Deserialize(ser.Serialize(cache));

		restored.Connections.Count().AssertEqual(3);

		var originalPairs = cache.Connections.OrderBy(p => p.Provider).ToArray();
		var restoredPairs = restored.Connections.OrderBy(p => p.Provider).ToArray();

		for (var i = 0; i < originalPairs.Length; i++)
		{
			restoredPairs[i].Provider.AssertEqual(originalPairs[i].Provider);
			restoredPairs[i].ConnectionString.AssertEqual(originalPairs[i].ConnectionString);
		}
	}

	[TestMethod]
	public void SaveLoadDropsEntriesWithEmptyProvider()
	{
		// Use IPersistable directly to inject an entry with an empty provider, then verify
		// that DatabaseConnectionCache.Load filters such invalid entries out.
		var pair = new DatabaseConnectionPair
		{
			Provider = string.Empty,
			ConnectionString = "cs",
		};

		var storage = new SettingsStorage();
		storage.SetValue("Connections", new[] { ((IPersistable)pair).Save() });

		var cache = new DatabaseConnectionCache();
		((IPersistable)cache).Load(storage);

		cache.Connections.Count().AssertEqual(0);
	}

	[TestMethod]
	public void PairEqualsSameValues()
	{
		var a = new DatabaseConnectionPair { Provider = "X", ConnectionString = "Y" };
		var b = new DatabaseConnectionPair { Provider = "X", ConnectionString = "Y" };

		a.Equals(b).AssertTrue();
		b.Equals(a).AssertTrue();
		a.GetHashCode().AssertEqual(b.GetHashCode());
	}

	[TestMethod]
	public void PairEqualsIgnoresCase()
	{
		var a = new DatabaseConnectionPair { Provider = "SqlServer", ConnectionString = "Server=Localhost" };
		var b = new DatabaseConnectionPair { Provider = "sqlserver", ConnectionString = "server=localhost" };

		a.Equals(b).AssertTrue();
		a.GetHashCode().AssertEqual(b.GetHashCode());
	}

	[TestMethod]
	public void PairEqualsDifferentProvider()
	{
		var a = new DatabaseConnectionPair { Provider = "X", ConnectionString = "cs" };
		var b = new DatabaseConnectionPair { Provider = "Y", ConnectionString = "cs" };

		a.Equals(b).AssertFalse();
	}

	[TestMethod]
	public void PairEqualsDifferentConnectionString()
	{
		var a = new DatabaseConnectionPair { Provider = "X", ConnectionString = "cs1" };
		var b = new DatabaseConnectionPair { Provider = "X", ConnectionString = "cs2" };

		a.Equals(b).AssertFalse();
	}

	[TestMethod]
	public void PairEqualsNullReturnsFalse()
	{
		var a = new DatabaseConnectionPair { Provider = "X", ConnectionString = "Y" };
		a.Equals(null).AssertFalse();
	}

	[TestMethod]
	public void PairEqualsSelfReferenceReturnsTrue()
	{
		var a = new DatabaseConnectionPair { Provider = "X", ConnectionString = "Y" };
		a.Equals(a).AssertTrue();
	}

	[TestMethod]
	public void PairEqualsDifferentTypeReturnsFalse()
	{
		var a = new DatabaseConnectionPair { Provider = "X", ConnectionString = "Y" };
		a.Equals("string").AssertFalse();
	}

	[TestMethod]
	public void PairEqualsBothNullStringsReturnsTrue()
	{
		var a = new DatabaseConnectionPair();
		var b = new DatabaseConnectionPair();

		a.Equals(b).AssertTrue();
		a.GetHashCode().AssertEqual(b.GetHashCode());
	}

	[TestMethod]
	public void PairTitleFormat()
	{
		var pair = new DatabaseConnectionPair { Provider = "SqlServer", ConnectionString = "Server=localhost" };
		pair.Title.AssertEqual("(SqlServer) Server=localhost");
	}

	[TestMethod]
	public void PairToStringIsTitle()
	{
		var pair = new DatabaseConnectionPair { Provider = "SQLite", ConnectionString = "Data Source=test.db" };
		pair.ToString().AssertEqual(pair.Title);
	}

	[TestMethod]
	public void PairPersistableRoundTrip()
	{
		var original = new DatabaseConnectionPair
		{
			Provider = DatabaseProviderRegistry.PostgreSql,
			ConnectionString = "Host=h;Port=5432",
		};

		var storage = new SettingsStorage();
		((IPersistable)original).Save(storage);

		var restored = new DatabaseConnectionPair();
		((IPersistable)restored).Load(storage);

		restored.Provider.AssertEqual(original.Provider);
		restored.ConnectionString.AssertEqual(original.ConnectionString);
	}

	[TestMethod]
	public void PairTitleNotifiesOnProviderChange()
	{
		var pair = new DatabaseConnectionPair { Provider = "A", ConnectionString = "cs" };
		var notifications = new List<string>();
		pair.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

		pair.Provider = "B";

		notifications.Contains(nameof(DatabaseConnectionPair.Title)).AssertTrue();
	}

	[TestMethod]
	public void PairTitleNotifiesOnConnectionStringChange()
	{
		var pair = new DatabaseConnectionPair { Provider = "A", ConnectionString = "cs1" };
		var notifications = new List<string>();
		pair.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

		pair.ConnectionString = "cs2";

		notifications.Contains(nameof(DatabaseConnectionPair.Title)).AssertTrue();
	}
}
