#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;

using Ecng.Data;
using Ecng.Serialization;

#region Single-level InnerSchema entities

/// <summary>
/// Complex (inner) type — NOT a standalone entity, just a value object
/// whose properties are flattened into the parent table.
/// </summary>
public class TestAddress
{
	public string Street { get; set; }
	public string City { get; set; }
}

/// <summary>
/// Entity with a complex type property (InnerSchema pattern).
/// The DB table has flattened columns: ShippingAddressStreet, ShippingAddressCity.
/// </summary>
public class TestOrderWithAddress : IDbPersistable
{
	public long Id { get; set; }
	public string OrderName { get; set; }

	/// <summary>
	/// Complex inline property — NOT a relation/FK.
	/// In the DB, stored as ShippingAddressStreet, ShippingAddressCity columns.
	/// </summary>
	public TestAddress ShippingAddress { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(OrderName), OrderName)
			.Set(nameof(ShippingAddress) + nameof(TestAddress.Street), ShippingAddress?.Street)
			.Set(nameof(ShippingAddress) + nameof(TestAddress.City), ShippingAddress?.City);
	}

	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		OrderName = storage.GetValue<string>(nameof(OrderName));
		ShippingAddress = new TestAddress
		{
			Street = storage.GetValue<string>(nameof(ShippingAddress) + nameof(TestAddress.Street)),
			City = storage.GetValue<string>(nameof(ShippingAddress) + nameof(TestAddress.City)),
		};
		return default;
	}
}

#endregion

#region Multi-level InnerSchema + RelationSingle entities

/// <summary>
/// 2nd-level inner schema: geo coordinates nested inside address.
/// NOT a standalone entity — flattened into parent.
/// </summary>
public class TestGeoCoord
{
	public double Lat { get; set; }
	public double Lon { get; set; }
}

/// <summary>
/// Standalone entity for RelationSingle testing inside inner schema.
/// Registered in SchemaRegistry with its own table.
/// </summary>
public class TestCountry : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(Name), Name);
	}

	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Name = storage.GetValue<string>(nameof(Name));
		return default;
	}
}

/// <summary>
/// Extended address with nested inner schema (GeoCoord) and RelationSingle (Country).
/// Flattened columns: Street, City, GeoCoordLat, GeoCoordLon, Country (FK).
/// </summary>
public class TestAddressEx
{
	public string Street { get; set; }
	public string City { get; set; }

	/// <summary>
	/// 2nd-level inner schema — flattened as GeoCoordLat, GeoCoordLon
	/// inside the parent prefix (e.g., ShippingAddressGeoCoordLat).
	/// </summary>
	public TestGeoCoord GeoCoord { get; set; }

	/// <summary>
	/// RelationSingle inside inner schema — stored as FK column
	/// (e.g., ShippingAddressCountry).
	/// </summary>
	[RelationSingle]
	public TestCountry Country { get; set; }
}

/// <summary>
/// Entity with multi-level inner schema and RelationSingle inside inner schema.
/// DB columns: Id, OrderName, ShippingAddressStreet, ShippingAddressCity,
/// ShippingAddressGeoCoordLat, ShippingAddressGeoCoordLon, ShippingAddressCountry.
/// </summary>
public class TestOrderWithAddressEx : IDbPersistable
{
	public long Id { get; set; }
	public string OrderName { get; set; }
	public TestAddressEx ShippingAddress { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(OrderName), OrderName)
			.Set(nameof(ShippingAddress) + nameof(TestAddressEx.Street), ShippingAddress?.Street)
			.Set(nameof(ShippingAddress) + nameof(TestAddressEx.City), ShippingAddress?.City)
			.Set(nameof(ShippingAddress) + nameof(TestAddressEx.GeoCoord) + nameof(TestGeoCoord.Lat), ShippingAddress?.GeoCoord?.Lat)
			.Set(nameof(ShippingAddress) + nameof(TestAddressEx.GeoCoord) + nameof(TestGeoCoord.Lon), ShippingAddress?.GeoCoord?.Lon)
			.SetFk(nameof(ShippingAddress) + nameof(TestAddressEx.Country), ShippingAddress?.Country?.Id);
	}

	public async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		OrderName = storage.GetValue<string>(nameof(OrderName));
		ShippingAddress = new TestAddressEx
		{
			Street = storage.GetValue<string>(nameof(ShippingAddress) + nameof(TestAddressEx.Street)),
			City = storage.GetValue<string>(nameof(ShippingAddress) + nameof(TestAddressEx.City)),
			GeoCoord = new TestGeoCoord
			{
				Lat = storage.GetValue<double>(nameof(ShippingAddress) + nameof(TestAddressEx.GeoCoord) + nameof(TestGeoCoord.Lat)),
				Lon = storage.GetValue<double>(nameof(ShippingAddress) + nameof(TestAddressEx.GeoCoord) + nameof(TestGeoCoord.Lon)),
			},
			Country = await storage.LoadFkAsync<TestCountry>(nameof(ShippingAddress) + nameof(TestAddressEx.Country), db, cancellationToken),
		};
	}
}

#endregion

/// <summary>
/// Tests CRUD and LINQ operations for entities with InnerSchema (complex inline type)
/// whose properties are flattened into the parent table columns.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class InnerSchemaTests : BaseTestClass
{
	private static readonly Schema _orderSchema = new()
	{
		TableName = "Ecng_OrderWithAddress",
		EntityType = typeof(TestOrderWithAddress),
		Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
		Columns =
		[
			new SchemaColumn { Name = "OrderName", ClrType = typeof(string) },
			new SchemaColumn { Name = "ShippingAddressStreet", ClrType = typeof(string) },
			new SchemaColumn { Name = "ShippingAddressCity", ClrType = typeof(string) },
		],
		Factory = () => new TestOrderWithAddress(),
	};

	private static bool _schemaRegistered;
	private IStorage _db;

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		DbTestHelper.RegisterAll();

		if (!_schemaRegistered)
		{
			SchemaRegistry.Register(_orderSchema);
			_schemaRegistered = true;
		}
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		DbTestHelper.ClearSQLitePools();
	}

	private void Init(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		// Drop table if exists, then create
		var dialect = DbTestHelper.GetDialect(provider);
		var quoted = dialect.QuoteIdentifier(_orderSchema.TableName);
		try { DbTestHelper.ExecuteRaw(provider, $"DROP TABLE IF EXISTS {quoted}"); } catch { }
		DbTestHelper.EnsureTable(provider, _orderSchema, autoIncrement: false);

		_db = DbTestHelper.CreateDatabase(provider);
	}

	private static long _nextId;

	[TestCleanup]
	public void Cleanup()
	{
		(_db as IDisposable)?.Dispose();
	}

	private static TestOrderWithAddress NewOrder(string name, string street, string city)
		=> new()
		{
			Id = Interlocked.Increment(ref _nextId),
			OrderName = name,
			ShippingAddress = new TestAddress { Street = street, City = city },
		};

	#region CRUD

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Create_InnerSchema_SavesFlattenedColumns(string provider)
	{
		Init(provider);
		var order = NewOrder("Order1", "123 Main St", "NYC");

		var saved = await _db.AddAsync(order, CancellationToken);

		saved.Id.AssertEqual(order.Id);
		saved.OrderName.AssertEqual("Order1");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Read_InnerSchema_LoadsFlattenedColumns(string provider)
	{
		Init(provider);
		var order = NewOrder("Order2", "456 Oak Ave", "LA");

		var saved = await _db.AddAsync(order, CancellationToken);
		var loaded = await _db.GetByIdAsync<long, TestOrderWithAddress>(saved.Id, CancellationToken);

		loaded.AssertNotNull();
		loaded.OrderName.AssertEqual("Order2");
		loaded.ShippingAddress.AssertNotNull();
		loaded.ShippingAddress.Street.AssertEqual("456 Oak Ave");
		loaded.ShippingAddress.City.AssertEqual("LA");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetCount_InnerSchema_ReturnsCorrectCount(string provider)
	{
		Init(provider);
		await _db.AddAsync(NewOrder("CountOrder1", "1st St", "NYC"), CancellationToken);
		await _db.AddAsync(NewOrder("CountOrder2", "2nd St", "LA"), CancellationToken);

		var count = await _db.GetCountAsync<TestOrderWithAddress>(CancellationToken);

		count.AssertEqual(2L);
	}

	#endregion

	#region LINQ (InnerSchema)

	/// <summary>
	/// LINQ Where on a complex inline type property generates correct SQL.
	/// Should produce "e"."ShippingAddressCity", not "ShippingAddress"."City".
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_ComplexTypeProperty_ShouldUseFlattenedColumnName(string provider)
	{
		Init(provider);
		await _db.AddAsync(NewOrder("TargetOrder", "123 Main St", "NYC"), CancellationToken);

		var queryable = new DefaultQueryable<TestOrderWithAddress>(
			new DefaultQueryProvider<TestOrderWithAddress>(_db), null);

		var city = "NYC";
		var filtered = Queryable.Where(queryable, o => o.ShippingAddress.City == city);

		var results = await filtered.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].ShippingAddress.City.AssertEqual("NYC");
	}

	#endregion

	#region Schema

	[TestMethod]
	public void SchemaRegistry_ManualRegistration_HasFlattenedColumns()
	{
		var schema = SchemaRegistry.Get(typeof(TestOrderWithAddress));
		var colNames = schema.Columns.Select(c => c.Name).ToArray();

		colNames.Contains("ShippingAddressStreet").AssertTrue(
			$"Should have flattened 'ShippingAddressStreet'. Columns: {string.Join(", ", colNames)}");
		colNames.Contains("ShippingAddressCity").AssertTrue(
			$"Should have flattened 'ShippingAddressCity'. Columns: {string.Join(", ", colNames)}");
		colNames.Contains("ShippingAddress").AssertFalse(
			$"Should NOT have single 'ShippingAddress' column. Columns: {string.Join(", ", colNames)}");
	}

	#endregion
}

/// <summary>
/// Tests multi-level InnerSchema nesting and RelationSingle inside inner schemas.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class InnerSchemaNestedTests : BaseTestClass
{
	private static readonly Schema _countrySchema = new()
	{
		TableName = "Ecng_Country",
		EntityType = typeof(TestCountry),
		Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
		Columns =
		[
			new SchemaColumn { Name = "Name", ClrType = typeof(string) },
		],
		Factory = () => new TestCountry(),
	};

	private static readonly Schema _orderExSchema = new()
	{
		TableName = "Ecng_OrderWithAddressEx",
		EntityType = typeof(TestOrderWithAddressEx),
		Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
		Columns =
		[
			new SchemaColumn { Name = "OrderName", ClrType = typeof(string) },
			new SchemaColumn { Name = "ShippingAddressStreet", ClrType = typeof(string) },
			new SchemaColumn { Name = "ShippingAddressCity", ClrType = typeof(string) },
			new SchemaColumn { Name = "ShippingAddressGeoCoordLat", ClrType = typeof(double) },
			new SchemaColumn { Name = "ShippingAddressGeoCoordLon", ClrType = typeof(double) },
			new SchemaColumn { Name = "ShippingAddressCountry", ClrType = typeof(long) },
		],
		Factory = () => new TestOrderWithAddressEx(),
	};

	private static bool _schemaRegistered;
	private IStorage _db;

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		DbTestHelper.RegisterAll();

		if (!_schemaRegistered)
		{
			SchemaRegistry.Register(_countrySchema);
			SchemaRegistry.Register(_orderExSchema);
			_schemaRegistered = true;
		}
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		DbTestHelper.ClearSQLitePools();
	}

	private void Init(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		var dialect = DbTestHelper.GetDialect(provider);

		// Drop tables (order matters for FK constraints)
		foreach (var tbl in new[] { _orderExSchema.TableName, _countrySchema.TableName })
		{
			var quoted = dialect.QuoteIdentifier(tbl);
			try { DbTestHelper.ExecuteRaw(provider, $"DROP TABLE IF EXISTS {quoted}"); } catch { }
		}

		// Create tables
		DbTestHelper.EnsureTable(provider, _countrySchema, autoIncrement: false);
		DbTestHelper.EnsureTable(provider, _orderExSchema, autoIncrement: false);

		_db = DbTestHelper.CreateDatabase(provider);
	}

	private static long _nextId = 10000;
	private static long _nextCountryId = 20000;

	[TestCleanup]
	public void Cleanup()
	{
		(_db as IDisposable)?.Dispose();
	}

	private static TestCountry NewCountry(string name)
		=> new()
		{
			Id = Interlocked.Increment(ref _nextCountryId),
			Name = name,
		};

	private static TestOrderWithAddressEx NewOrderEx(string name, string street, string city,
		double lat, double lon, TestCountry country)
		=> new()
		{
			Id = Interlocked.Increment(ref _nextId),
			OrderName = name,
			ShippingAddress = new TestAddressEx
			{
				Street = street,
				City = city,
				GeoCoord = new TestGeoCoord { Lat = lat, Lon = lon },
				Country = country,
			},
		};

	#region CRUD

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Create_NestedInnerSchema_SavesFlattenedColumns(string provider)
	{
		Init(provider);
		var country = await _db.AddAsync(NewCountry("US"), CancellationToken);
		var order = NewOrderEx("OrderNested1", "123 Main St", "NYC", 40.7128, -74.006, country);

		var saved = await _db.AddAsync(order, CancellationToken);

		saved.Id.AssertEqual(order.Id);
		saved.OrderName.AssertEqual("OrderNested1");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Read_NestedInnerSchema_LoadsFlattenedColumns(string provider)
	{
		Init(provider);
		var country = await _db.AddAsync(NewCountry("US"), CancellationToken);
		var order = NewOrderEx("OrderNested2", "456 Oak Ave", "LA", 34.0522, -118.2437, country);

		var saved = await _db.AddAsync(order, CancellationToken);
		var loaded = await _db.GetByIdAsync<long, TestOrderWithAddressEx>(saved.Id, CancellationToken);

		loaded.AssertNotNull();
		loaded.OrderName.AssertEqual("OrderNested2");
		loaded.ShippingAddress.AssertNotNull();
		loaded.ShippingAddress.Street.AssertEqual("456 Oak Ave");
		loaded.ShippingAddress.City.AssertEqual("LA");
		loaded.ShippingAddress.GeoCoord.AssertNotNull();
		loaded.ShippingAddress.GeoCoord.Lat.AssertEqual(34.0522);
		loaded.ShippingAddress.GeoCoord.Lon.AssertEqual(-118.2437);
		loaded.ShippingAddress.Country.AssertNotNull();
		loaded.ShippingAddress.Country.Name.AssertEqual("US");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetCount_NestedInnerSchema_ReturnsCorrectCount(string provider)
	{
		Init(provider);
		var country = await _db.AddAsync(NewCountry("US"), CancellationToken);
		await _db.AddAsync(NewOrderEx("N1", "1st St", "NYC", 40.0, -74.0, country), CancellationToken);
		await _db.AddAsync(NewOrderEx("N2", "2nd St", "LA", 34.0, -118.0, country), CancellationToken);

		var count = await _db.GetCountAsync<TestOrderWithAddressEx>(CancellationToken);

		count.AssertEqual(2L);
	}

	#endregion

	#region LINQ — multi-level InnerSchema

	/// <summary>
	/// 2-level deep: o.ShippingAddress.GeoCoord.Lat == value
	/// Should generate "e"."ShippingAddressGeoCoordLat", not "GeoCoord"."Lat".
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_NestedInnerSchema_2Levels_ShouldUseFlattenedColumnName(string provider)
	{
		Init(provider);
		var country = await _db.AddAsync(NewCountry("US"), CancellationToken);
		await _db.AddAsync(NewOrderEx("GeoOrder", "Main St", "NYC", 40.7128, -74.006, country), CancellationToken);

		var queryable = new DefaultQueryable<TestOrderWithAddressEx>(
			new DefaultQueryProvider<TestOrderWithAddressEx>(_db), null);

		var lat = 40.7128;
		var filtered = Queryable.Where(queryable, o => o.ShippingAddress.GeoCoord.Lat == lat);

		var results = await filtered.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].ShippingAddress.GeoCoord.Lat.AssertEqual(40.7128);
	}

	/// <summary>
	/// 1-level deep on extended entity: o.ShippingAddress.City == value
	/// Should still work with the extended entity.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_NestedInnerSchema_1Level_StillWorks(string provider)
	{
		Init(provider);
		var country = await _db.AddAsync(NewCountry("US"), CancellationToken);
		await _db.AddAsync(NewOrderEx("CityOrder", "Main St", "NYC", 40.0, -74.0, country), CancellationToken);

		var queryable = new DefaultQueryable<TestOrderWithAddressEx>(
			new DefaultQueryProvider<TestOrderWithAddressEx>(_db), null);

		var city = "NYC";
		var filtered = Queryable.Where(queryable, o => o.ShippingAddress.City == city);

		var results = await filtered.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].ShippingAddress.City.AssertEqual("NYC");
	}

	#endregion

	#region LINQ — RelationSingle inside InnerSchema

	/// <summary>
	/// RelationSingle FK inside inner schema: o.ShippingAddress.Country.Id == value
	/// Should generate "e"."ShippingAddressCountry", not "Country"."Id".
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_RelationSingleInsideInnerSchema_ShouldUseFlattenedFkColumn(string provider)
	{
		Init(provider);
		var country = await _db.AddAsync(NewCountry("US"), CancellationToken);
		await _db.AddAsync(NewOrderEx("FkOrder", "Main St", "NYC", 40.0, -74.0, country), CancellationToken);

		var queryable = new DefaultQueryable<TestOrderWithAddressEx>(
			new DefaultQueryProvider<TestOrderWithAddressEx>(_db), null);

		var countryId = country.Id;
		var filtered = Queryable.Where(queryable, o => o.ShippingAddress.Country.Id == countryId);

		var results = await filtered.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].ShippingAddress.Country.AssertNotNull();
		results[0].ShippingAddress.Country.Id.AssertEqual(country.Id);
	}

	#endregion

	#region Schema

	[TestMethod]
	public void SchemaRegistry_NestedInnerSchema_HasAllFlattenedColumns()
	{
		var schema = SchemaRegistry.Get(typeof(TestOrderWithAddressEx));
		var colNames = schema.Columns.Select(c => c.Name).ToArray();

		colNames.Contains("ShippingAddressStreet").AssertTrue(
			$"Should have 'ShippingAddressStreet'. Columns: {string.Join(", ", colNames)}");
		colNames.Contains("ShippingAddressCity").AssertTrue(
			$"Should have 'ShippingAddressCity'. Columns: {string.Join(", ", colNames)}");
		colNames.Contains("ShippingAddressGeoCoordLat").AssertTrue(
			$"Should have 'ShippingAddressGeoCoordLat'. Columns: {string.Join(", ", colNames)}");
		colNames.Contains("ShippingAddressGeoCoordLon").AssertTrue(
			$"Should have 'ShippingAddressGeoCoordLon'. Columns: {string.Join(", ", colNames)}");
		colNames.Contains("ShippingAddressCountry").AssertTrue(
			$"Should have 'ShippingAddressCountry' (FK). Columns: {string.Join(", ", colNames)}");
	}

	#endregion
}

#endif
