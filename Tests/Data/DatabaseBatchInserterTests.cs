#if NET10_0_OR_GREATER
namespace Ecng.Tests.Data;

using Ecng.Data;

using Microsoft.Data.SqlClient;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class DatabaseBatchInserterTests : BaseTestClass
{
	private const string _testTableName = "ecng_batch_test";
	private const string _dynamicTestTableName = "ecng_batch_dynamic_test";

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		// Register SQL Server provider for ADO.NET tests
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SqlServer, SqlClientFactory.Instance);
	}

	private static DatabaseConnectionPair GetSqlServerConnectionPair()
	{
		return new()
		{
			Provider = DatabaseProviderRegistry.AllProviders.First(),
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		};
	}

	private static IDatabaseBatchInserterProvider CreateProvider(string providerName)
	{
		return providerName switch
		{
			nameof(Linq2dbBatchInserterProvider) => new Linq2dbBatchInserterProvider(),
			nameof(AdoBatchInserterProvider) => new AdoBatchInserterProvider(),
			_ => throw new InvalidOperationException($"Unknown provider: {providerName}"),
		};
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task InsertAsync_SingleItem_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _testTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			_testTableName,
			b => ConfigureMapping(b, _testTableName));

		var entity = new TestEntity
		{
			Id = 1,
			Name = "Test Item",
			Value = 123.45m,
			CreatedAt = DateTime.UtcNow,
		};

		await inserter.InsertAsync(entity, CancellationToken);
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task BulkCopyAsync_MultipleItems_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _testTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			_testTableName,
			b => ConfigureMapping(b, _testTableName));

		var entities = Enumerable.Range(1, 100)
			.Select(i => new TestEntity
			{
				Id = i,
				Name = $"Item {i}",
				Value = i * 1.5m,
				CreatedAt = DateTime.UtcNow.AddMinutes(-i),
			})
			.ToList();

		await inserter.BulkCopyAsync(entities, CancellationToken);
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task BulkCopyAsync_LargeDataset_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _testTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			_testTableName,
			b => ConfigureMapping(b, _testTableName));

		var entities = Enumerable.Range(1, 1000)
			.Select(i => new TestEntity
			{
				Id = i,
				Name = $"Bulk Item {i}",
				Value = i * 0.01m,
				CreatedAt = DateTime.UtcNow,
			})
			.ToList();

		await inserter.BulkCopyAsync(entities, CancellationToken);
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task Create_WithDynamicProperties_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _dynamicTestTableName);

		using var inserter = provider.Create<DynamicTestEntity>(
			connection,
			_dynamicTestTableName,
			b => ConfigureDynamicMapping(b, _dynamicTestTableName));

		var entity = new DynamicTestEntity
		{
			Id = 1,
			Name = "Dynamic Test",
		};
		entity.SetDynamic("CustomField", "CustomValue");

		await inserter.InsertAsync(entity, CancellationToken);
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public void Create_NullConnection_ThrowsArgumentNullException(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task Create_EmptyTableName_ThrowsArgumentNullException(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "", _ => { }));
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task Create_NullConfigureMapping_ThrowsArgumentNullException(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "table", null));
	}

	private static void ConfigureMapping(IDatabaseMappingBuilder<TestEntity> builder, string tableName)
	{
		builder
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(e => e.Id).HasColumnName("id").HasDataType(DatabaseDataType.Int)
			.Property(e => e.Name).HasColumnName("name").HasLength(100).HasDataType(DatabaseDataType.NVarChar)
			.Property(e => e.Value).HasColumnName("value").HasScale(2).HasDataType(DatabaseDataType.Decimal)
			.Property(e => e.CreatedAt).HasColumnName("created_at").HasDataType(DatabaseDataType.DateTime);
	}

	private static void ConfigureDynamicMapping(IDatabaseMappingBuilder<DynamicTestEntity> builder, string tableName)
	{
		builder
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(e => e.Id).HasColumnName("id").HasDataType(DatabaseDataType.Int)
			.Property(e => e.Name).HasColumnName("name").HasLength(100).HasDataType(DatabaseDataType.NVarChar)
			.DynamicProperty("CustomField").HasColumnName("custom_field").HasLength(200).HasDataType(DatabaseDataType.NVarChar)
			.DynamicPropertyAccessors(
				(e, name, def) => e.GetDynamic(name) ?? def,
				(e, name, value) => e.SetDynamic(name, value));
	}

	public class TestEntity
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Value { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class DynamicTestEntity
	{
		private readonly Dictionary<string, object> _dynamicProperties = [];

		public int Id { get; set; }
		public string Name { get; set; }

		public object GetDynamic(string name)
			=> _dynamicProperties.TryGetValue(name, out var value) ? value : null;

		public void SetDynamic(string name, object value)
			=> _dynamicProperties[name] = value;
	}

	public enum TestStatus
	{
		Unknown,
		Active,
		Inactive,
		Deleted
	}

	public enum TestFlags : long
	{
		None = 0,
		Flag1 = 1L << 32,
		Flag2 = 1L << 33,
		All = Flag1 | Flag2
	}

	public class ComplexTestEntity
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public TestStatus Status { get; set; }
		public TestFlags Flags { get; set; }
		public TimeZoneInfo TimeZone { get; set; }
		public TimeSpan Duration { get; set; }
		public Guid UniqueId { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public bool IsActive { get; set; }
		public int? NullableInt { get; set; }
		public decimal? NullableDecimal { get; set; }
		public DateTime? NullableDate { get; set; }
		public double Price { get; set; }
		public float Rate { get; set; }
	}

	private const string _complexTestTableName = "ecng_batch_complex_test";

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task InsertAsync_ComplexTypes_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _complexTestTableName);

		using var inserter = provider.Create<ComplexTestEntity>(
			connection,
			_complexTestTableName,
			ConfigureComplexMapping);

		var entity = new ComplexTestEntity
		{
			Id = 1,
			Name = "Complex Test",
			Status = TestStatus.Active,
			Flags = TestFlags.Flag1 | TestFlags.Flag2,
			TimeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC"),
			Duration = TimeSpan.FromHours(2.5),
			UniqueId = Guid.NewGuid(),
			CreatedAt = DateTimeOffset.UtcNow,
			IsActive = true,
			NullableInt = 42,
			NullableDecimal = 123.45m,
			NullableDate = DateTime.UtcNow,
			Price = 99.99,
			Rate = 0.15f,
		};

		await inserter.InsertAsync(entity, CancellationToken);
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task InsertAsync_ComplexTypesWithNulls_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _complexTestTableName);

		using var inserter = provider.Create<ComplexTestEntity>(
			connection,
			_complexTestTableName,
			ConfigureComplexMapping);

		var entity = new ComplexTestEntity
		{
			Id = 2,
			Name = "Nullable Test",
			Status = TestStatus.Unknown,
			Flags = TestFlags.None,
			TimeZone = TimeZoneInfo.Utc,
			Duration = TimeSpan.Zero,
			UniqueId = Guid.Empty,
			CreatedAt = DateTimeOffset.MinValue,
			IsActive = false,
			NullableInt = null,
			NullableDecimal = null,
			NullableDate = null,
			Price = 0,
			Rate = 0,
		};

		await inserter.InsertAsync(entity, CancellationToken);
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task BulkCopyAsync_ComplexTypes_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _complexTestTableName);

		using var inserter = provider.Create<ComplexTestEntity>(
			connection,
			_complexTestTableName,
			ConfigureComplexMapping);

		var entities = Enumerable.Range(1, 100)
			.Select(i => new ComplexTestEntity
			{
				Id = i,
				Name = $"Bulk Complex {i}",
				Status = (TestStatus)(i % 4),
				Flags = i % 2 == 0 ? TestFlags.Flag1 : TestFlags.Flag2,
				TimeZone = TimeZoneInfo.Utc,
				Duration = TimeSpan.FromMinutes(i),
				UniqueId = Guid.NewGuid(),
				CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
				IsActive = i % 2 == 0,
				NullableInt = i % 3 == 0 ? null : i,
				NullableDecimal = i % 5 == 0 ? null : i * 1.1m,
				NullableDate = i % 7 == 0 ? null : DateTime.UtcNow.AddDays(-i),
				Price = i * 10.5,
				Rate = i * 0.01f,
			})
			.ToList();

		await inserter.BulkCopyAsync(entities, CancellationToken);
	}

	private static void ConfigureComplexMapping(IDatabaseMappingBuilder<ComplexTestEntity> builder)
	{
		builder
			.HasTableName(_complexTestTableName)
			.SetConverter<TimeZoneInfo, string>(tz => tz.Id)
			.SetConverter<TimeSpan, long>(ts => ts.Ticks)
			.SetConverter<Guid, string>(g => g.ToString())
			.Property(e => e.Id)
			.Property(e => e.Name).HasLength(100)
			.Property(e => e.Status)
			.Property(e => e.Flags)
			.Property(e => e.TimeZone).HasDataType(DatabaseDataType.NVarChar).HasLength(100)
			.Property(e => e.Duration).HasDataType(DatabaseDataType.BigInt)
			.Property(e => e.UniqueId).HasDataType(DatabaseDataType.NVarChar).HasLength(36)
			.Property(e => e.CreatedAt)
			.Property(e => e.IsActive)
			.Property(e => e.NullableInt)
			.Property(e => e.NullableDecimal).HasScale(2)
			.Property(e => e.NullableDate)
			.Property(e => e.Price).HasScale(2)
			.Property(e => e.Rate).HasScale(4);
	}
}
#endif