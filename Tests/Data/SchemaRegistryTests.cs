#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Collections;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

#region Test entities for reflection tests

/// <summary>
/// Inner value object for InnerSchema reflection tests.
/// </summary>
public class ReflTestCoord
{
	public double Lat { get; set; }
	public double Lon { get; set; }
}

/// <summary>
/// Inner value object with nested inner schema and RelationSingle.
/// </summary>
public class ReflTestAddress
{
	public string Street { get; set; }
	public string City { get; set; }
	public ReflTestCoord Geo { get; set; }

	[RelationSingle]
	public TestCountry Country { get; set; }
}

/// <summary>
/// Entity with InnerSchema property — no manual schema registration.
/// CreateFromReflection should flatten ShippingAddress into columns.
/// </summary>
[Entity(Name = "Ecng_ReflOrder")]
public class ReflTestOrder : IDbPersistable
{
	public long Id { get; set; }
	public string OrderName { get; set; }
	public ReflTestAddress ShippingAddress { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Inner value object for NameOverride tests.
/// </summary>
public class ReflTestMeta
{
	public string Tag { get; set; }
	public int Score { get; set; }
}

/// <summary>
/// Entity with NameOverride on InnerSchema property.
/// </summary>
[Entity(Name = "Ecng_ReflProduct")]
public class ReflTestProduct : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }

	[NameOverride("Tag", "ProductTag")]
	[NameOverride("Score", "ProductScore")]
	public ReflTestMeta Meta { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Entity with [Unique] and [Index] attributes on properties.
/// </summary>
[Entity(Name = "Ecng_ReflIndexed")]
public class ReflTestIndexed : IDbPersistable
{
	public long Id { get; set; }

	[Unique]
	public string Code { get; set; }

	[Index]
	public int Priority { get; set; }

	public string Description { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Self-referencing type for circular dependency test.
/// </summary>
public class ReflTestCyclic
{
	public string Value { get; set; }
	public ReflTestCyclic Self { get; set; }
}

/// <summary>
/// Entity with a self-referencing InnerSchema property.
/// </summary>
[Entity(Name = "Ecng_ReflCyclic")]
public class ReflTestCyclicEntity : IDbPersistable
{
	public long Id { get; set; }
	public ReflTestCyclic Data { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Reflection entity with Guid identity.
/// </summary>
[Entity(Name = "Ecng_ReflGuidId")]
public class ReflTestGuidIdEntity : IDbPersistable
{
	[Identity]
	public Guid Id { get; set; }
	public string Title { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<Guid>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// Long-identity entity referencing a Guid-identity entity.
/// FK column must be Guid, not long.
/// </summary>
[Entity(Name = "Ecng_ReflLongRefGuid")]
public class ReflTestLongRefGuidEntity : IDbPersistable
{
	public long Id { get; set; }

	[RelationSingle]
	public ReflTestGuidIdEntity GuidRef { get; set; }

	public string Note { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// Guid-identity entity referencing a long-identity entity.
/// FK column must be long.
/// </summary>
[Entity(Name = "Ecng_ReflGuidRefLong")]
public class ReflTestGuidRefLongEntity : IDbPersistable
{
	[Identity]
	public Guid Id { get; set; }

	[RelationSingle]
	public ReflTestOrder LongRef { get; set; }

	public string Label { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<Guid>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

// ===== Reflection entities for deep nullable propagation tests =====

/// <summary>
/// Outer NOT NULL → nullable introduced at L2.NullBranch, SolidBranch stays NOT NULL.
/// </summary>
[Entity(Name = "Ecng_ReflNullMid")]
public class ReflNullMidEntity : IDbPersistable
{
	public long Id { get; set; }
	public NullPropL2 Data { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// Outer nullable, inner tries [Column(IsNullable = false)] — outer must win.
/// </summary>
[Entity(Name = "Ecng_ReflNullCantCancel")]
public class ReflNullCantCancelEntity : IDbPersistable
{
	public long Id { get; set; }

	[Column(IsNullable = true)]
	public NullPropL2Strict Data { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// Outer nullable with [ColumnOverride] forcing Secret to NOT NULL.
/// </summary>
[Entity(Name = "Ecng_ReflColOverride")]
public class ReflColumnOverrideEntity : IDbPersistable
{
	public long Id { get; set; }

	[Column(IsNullable = true)]
	[ColumnOverride(nameof(TestKeySecret.Secret), IsNullable = false)]
	public TestKeySecret Auth { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// ColumnOverride + NameOverride on same inner schema property.
/// </summary>
[Entity(Name = "Ecng_ReflColOverrideName")]
public class ReflColumnOverrideWithNameEntity : IDbPersistable
{
	public long Id { get; set; }

	[Column(IsNullable = true)]
	[NameOverride("Secret", "ApiToken")]
	[ColumnOverride(nameof(TestKeySecret.Secret), IsNullable = false)]
	public TestKeySecret Auth { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// Outer nullable at root, entire 4-level tree must be nullable.
/// </summary>
[Entity(Name = "Ecng_ReflNullRoot")]
public class ReflNullRootEntity : IDbPersistable
{
	public long Id { get; set; }

	[Column(IsNullable = true)]
	public NullPropL2 Data { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

/// <summary>
/// Dummy ViewProcessor for testing.
/// </summary>
public class ReflTestViewProcessor : IViewProcessor<ReflTestViewEntity, long>
{
	public Type TableType => typeof(TestItem);
	public Type ViewType => typeof(ReflTestViewEntity);
	public IQueryable<ReflTestViewEntity> Select => throw new NotSupportedException();
	public ValueTask<IEnumerable> ReadRange(object[] ids, CancellationToken cancellationToken) => throw new NotSupportedException();
	public ValueTask<ReflTestViewEntity[]> ReadRange(long[] ids, CancellationToken cancellationToken) => throw new NotSupportedException();
	public ValueTask<ReflTestViewEntity> Create(ReflTestViewEntity view, CancellationToken cancellationToken) => throw new NotSupportedException();
	public ValueTask<ReflTestViewEntity> ReadById(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
	public ValueTask<ReflTestViewEntity> Update(ReflTestViewEntity view, CancellationToken cancellationToken) => throw new NotSupportedException();
	public ValueTask<bool> Delete(ReflTestViewEntity view, CancellationToken cancellationToken) => throw new NotSupportedException();
}

/// <summary>
/// View entity with ViewProcessorAttribute for reflection tests.
/// </summary>
[Entity(Name = "V_ReflTestView")]
[ViewProcessor(typeof(ReflTestViewProcessor))]
public class ReflTestViewEntity : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

#endregion

/// <summary>
/// Tests for SchemaRegistry reflection-based schema creation.
/// Verifies [Entity], [Identity], [Ignore], [RelationSingle], [RelationMany],
/// InnerSchema flattening, [NameOverride], [Index], [Unique], and circular dependency handling.
/// </summary>
[TestClass]
public class SchemaRegistryTests : BaseTestClass
{
	private static readonly ISqlDialect _dialect = SqlServerDialect.Instance;
	private static readonly QueryProvider _provider = new();

	#region [Entity] attribute

	[TestMethod]
	public void Reflection_WithEntityAttribute_UsesAttributeName()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));

		schema.TableName.AssertEqual("Ecng_TestItem");
	}

	[TestMethod]
	public void Reflection_WithoutEntityAttribute_UsesClassName()
	{
		var schema = SchemaRegistry.Get(typeof(TestItemWithIgnored));

		schema.TableName.AssertEqual(nameof(TestItemWithIgnored));
	}

	[TestMethod]
	public void Reflection_EntityAttribute_NoCache()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));

		schema.NoCache.AssertFalse();
	}

	#endregion

	#region [Identity] attribute

	[TestMethod]
	public void Reflection_IdProperty_BecomesIdentity()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));

		schema.Identity.AssertNotNull();
		schema.Identity.Name.AssertEqual("Id");
		schema.Identity.IsReadOnly.AssertTrue();
	}

	[TestMethod]
	public void Reflection_Identity_NotInColumns()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));

		schema.Columns.Any(c => c.Name == "Id").AssertFalse();
	}

	[TestMethod]
	public void Reflection_Identity_GuidType()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestGuidIdEntity));

		schema.Identity.AssertNotNull();
		schema.Identity.Name.AssertEqual("Id");
		schema.Identity.ClrType.AssertEqual(typeof(Guid));
		schema.Columns.Any(c => c.Name == "Id").AssertFalse();
	}

	#endregion

	#region [Ignore] attribute

	[TestMethod]
	public void Reflection_IgnoredProperty_ExcludedFromColumns()
	{
		var schema = SchemaRegistry.Get(typeof(TestItemWithIgnored));

		schema.Columns.Any(c => c.Name == "Computed").AssertFalse();
		schema.Columns.Any(c => c.Name == "Name").AssertTrue();
	}

	#endregion

	#region [RelationSingle] attribute

	[TestMethod]
	public void Reflection_RelationSingle_StoredAsLong()
	{
		var schema = SchemaRegistry.Get(typeof(TestItemCategory));

		var itemCol = schema.Columns.First(c => c.Name == "Item");
		itemCol.ClrType.AssertEqual(typeof(long));

		var catCol = schema.Columns.First(c => c.Name == "Category");
		catCol.ClrType.AssertEqual(typeof(long));
	}

	[TestMethod]
	public void Reflection_RelationSingle_GuidRef_ClrTypeIsGuid()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestLongRefGuidEntity));
		var col = schema.Columns.First(c => c.Name == "GuidRef");

		col.ClrType.AssertEqual(typeof(Guid));
	}

	[TestMethod]
	public void Reflection_RelationSingle_LongRef_ClrTypeIsLong()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestGuidRefLongEntity));
		var col = schema.Columns.First(c => c.Name == "LongRef");

		col.ClrType.AssertEqual(typeof(long));
	}

	#endregion

	#region [RelationMany] attribute

	[TestMethod]
	public void Reflection_RelationMany_ExcludedFromColumns()
	{
		var schema = SchemaRegistry.Get(typeof(TestPerson));

		schema.Columns.Any(c => c.Name == "Tasks").AssertFalse();
	}

	#endregion

	#region [AllColumnsField] attribute

	[TestMethod]
	public void Reflection_AllColumnsField_ExcludedFromColumns()
	{
		var schema = SchemaRegistry.Get(typeof(TestPerson));

		schema.Columns.Any(c => c.Name == "AllColumns").AssertFalse();
	}

	#endregion

	#region InnerSchema flattening

	[TestMethod]
	public void Reflection_InnerSchema_FlattensColumns()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestOrder));

		schema.Columns.Any(c => c.Name == "OrderName").AssertTrue();
		schema.Columns.Any(c => c.Name == "ShippingAddressStreet").AssertTrue();
		schema.Columns.Any(c => c.Name == "ShippingAddressCity").AssertTrue();

		// no raw "ShippingAddress" column
		schema.Columns.Any(c => c.Name == "ShippingAddress").AssertFalse();
	}

	[TestMethod]
	public void Reflection_InnerSchema_NestedFlattening()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestOrder));

		// 2nd level: ReflTestAddress.Geo → ShippingAddressGeoLat, ShippingAddressGeoLon
		schema.Columns.Any(c => c.Name == "ShippingAddressGeoLat").AssertTrue();
		schema.Columns.Any(c => c.Name == "ShippingAddressGeoLon").AssertTrue();
	}

	[TestMethod]
	public void Reflection_InnerSchema_RelationSingleInsideInner()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestOrder));

		// ReflTestAddress.Country is [RelationSingle] → ShippingAddressCountry as long
		var col = schema.Columns.First(c => c.Name == "ShippingAddressCountry");
		col.ClrType.AssertEqual(typeof(long));
	}

	[TestMethod]
	public void Reflection_InnerSchema_ColumnTypes()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestOrder));

		schema.Columns.First(c => c.Name == "ShippingAddressStreet").ClrType.AssertEqual(typeof(string));
		schema.Columns.First(c => c.Name == "ShippingAddressCity").ClrType.AssertEqual(typeof(string));
		schema.Columns.First(c => c.Name == "ShippingAddressGeoLat").ClrType.AssertEqual(typeof(double));
		schema.Columns.First(c => c.Name == "ShippingAddressGeoLon").ClrType.AssertEqual(typeof(double));
	}

	[TestMethod]
	public void Reflection_InnerSchema_Sql_ContainsFlattenedColumns()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestOrder));
		var query = _provider.Create(schema, SqlCommandTypes.UpdateBy, [schema.Identity], schema.AllColumns);
		var sql = query.Render(_dialect);

		sql.Contains("[ShippingAddressStreet]").AssertTrue($"Expected flattened column in SQL, got: {sql}");
		sql.Contains("[ShippingAddressGeoLat]").AssertTrue($"Expected nested flattened column in SQL, got: {sql}");
		sql.Contains("[Ecng_ReflOrder]").AssertTrue($"Expected table name in SQL, got: {sql}");
	}

	#endregion

	#region [NameOverride] attribute

	[TestMethod]
	public void Reflection_NameOverride_UsesOverriddenColumnNames()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestProduct));

		// Meta.Tag → ProductTag (overridden), Meta.Score → ProductScore (overridden)
		schema.Columns.Any(c => c.Name == "ProductTag").AssertTrue();
		schema.Columns.Any(c => c.Name == "ProductScore").AssertTrue();

		// default names should NOT exist
		schema.Columns.Any(c => c.Name == "MetaTag").AssertFalse();
		schema.Columns.Any(c => c.Name == "MetaScore").AssertFalse();
	}

	#endregion

	#region [Index] / [Unique] attributes

	[TestMethod]
	public void Reflection_UniqueAttribute_SetsIsUnique()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestIndexed));

		var codeCol = schema.Columns.First(c => c.Name == "Code");
		codeCol.IsUnique.AssertTrue();
		codeCol.IsIndex.AssertTrue(); // Unique implies Index
	}

	[TestMethod]
	public void Reflection_IndexAttribute_SetsIsIndex()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestIndexed));

		var priorityCol = schema.Columns.First(c => c.Name == "Priority");
		priorityCol.IsIndex.AssertTrue();
		priorityCol.IsUnique.AssertFalse();
	}

	[TestMethod]
	public void Reflection_NoIndexAttribute_DefaultsFalse()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestIndexed));

		var descCol = schema.Columns.First(c => c.Name == "Description");
		descCol.IsIndex.AssertFalse();
		descCol.IsUnique.AssertFalse();
	}

	#endregion

	#region Circular dependency protection

	[TestMethod]
	public void Reflection_CircularReference_DoesNotStackOverflow()
	{
		// ReflTestCyclic has Self property of same type — should not recurse infinitely
		var schema = SchemaRegistry.Get(typeof(ReflTestCyclicEntity));

		schema.AssertNotNull();
		schema.TableName.AssertEqual("Ecng_ReflCyclic");
	}

	#endregion

	#region IDbPersistable.Schema default implementation

	[TestMethod]
	public void Schema_ViaInterface_ReturnsRegisteredSchema()
	{
		IDbPersistable entity = new ReflTestOrder();
		var schema = entity.Schema;

		schema.AssertNotNull();
		schema.AssertSame(SchemaRegistry.Get(typeof(ReflTestOrder)));
		schema.TableName.AssertEqual("Ecng_ReflOrder");
	}

	[TestMethod]
	public void Schema_ViaInterface_HasCorrectColumns()
	{
		IDbPersistable entity = new ReflTestOrder();
		var schema = entity.Schema;

		schema.Identity.AssertNotNull();
		schema.Identity.Name.AssertEqual("Id");
		schema.Columns.Any(c => c.Name == "OrderName").AssertTrue();
		schema.Columns.Any(c => c.Name == "ShippingAddressStreet").AssertTrue();
	}

	[TestMethod]
	public void Schema_ViaInterface_DifferentEntities_DifferentSchemas()
	{
		IDbPersistable order = new ReflTestOrder();
		IDbPersistable indexed = new ReflTestIndexed();

		order.Schema.AssertNotSame(indexed.Schema);
		order.Schema.TableName.AssertEqual("Ecng_ReflOrder");
		indexed.Schema.TableName.AssertEqual("Ecng_ReflIndexed");
	}

	#endregion

	#region SQL generation uses correct table name

	[TestMethod]
	public void Sql_ReadBy_UsesEntityAttributeTableName()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));
		var query = _provider.Create(schema, SqlCommandTypes.ReadBy, [schema.Identity], schema.AllColumns);
		var sql = query.Render(_dialect);

		sql.Contains("[Ecng_TestItem]").AssertTrue(
			$"Expected '[Ecng_TestItem]' in SQL, got: {sql}");
	}

	[TestMethod]
	public void Sql_ReadBy_WithoutEntityAttribute_UsesClassName()
	{
		var schema = SchemaRegistry.Get(typeof(TestItemWithIgnored));
		var query = _provider.Create(schema, SqlCommandTypes.ReadBy, [schema.Identity], schema.AllColumns);
		var sql = query.Render(_dialect);

		sql.Contains($"[{nameof(TestItemWithIgnored)}]").AssertTrue(
			$"Expected '[{nameof(TestItemWithIgnored)}]' in SQL, got: {sql}");
	}

	[TestMethod]
	public void Sql_Count_UsesEntityAttributeTableName()
	{
		var schema = SchemaRegistry.Get(typeof(TestCategory));
		var query = _provider.Create(schema, SqlCommandTypes.Count, [schema.Identity], schema.AllColumns);
		var sql = query.Render(_dialect);

		sql.Contains("[Ecng_TestCategory]").AssertTrue(
			$"Expected '[Ecng_TestCategory]' in SQL, got: {sql}");
	}

	[TestMethod]
	public void Sql_ReadAll_UsesEntityAttributeTableName()
	{
		var schema = SchemaRegistry.Get(typeof(TestPerson));
		var query = _provider.Create(schema, SqlCommandTypes.ReadAll, [], schema.AllColumns);
		var sql = query.Render(_dialect);

		sql.Contains("[Ecng_TestPerson]").AssertTrue(
			$"Expected '[Ecng_TestPerson]' in SQL, got: {sql}");
	}

	#endregion

	#region Deep nullable propagation (reflection)

	[TestMethod]
	public void Reflection_NullMid_NullBranchPropagates4Levels()
	{
		var schema = SchemaRegistry.Get(typeof(ReflNullMidEntity));

		// L2.Top — outer is NOT NULL → NOT NULL
		schema.Columns.First(c => c.Name == "DataTop").IsNullable.AssertFalse();

		// NullBranch subtree — [Column(IsNullable = true)] at L2.NullBranch → all NULL
		schema.Columns.First(c => c.Name == "DataNullBranchMid").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Deep").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Count").IsNullable.AssertTrue();

		// SolidBranch subtree — no nullable marker → all NOT NULL
		schema.Columns.First(c => c.Name == "DataSolidBranchMid").IsNullable.AssertFalse();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Deep").IsNullable.AssertFalse();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Count").IsNullable.AssertFalse();
	}

	[TestMethod]
	public void Reflection_NullCantCancel_OuterWins()
	{
		var schema = SchemaRegistry.Get(typeof(ReflNullCantCancelEntity));

		// Outer [Column(IsNullable = true)], inner [Column(IsNullable = false)] — outer wins
		schema.Columns.First(c => c.Name == "DataForced").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNormal").IsNullable.AssertTrue();
	}

	[TestMethod]
	public void Reflection_NullRoot_Entire4LevelTreeNullable()
	{
		var schema = SchemaRegistry.Get(typeof(ReflNullRootEntity));

		// Root is nullable → everything is nullable, including SolidBranch
		schema.Columns.First(c => c.Name == "DataTop").IsNullable.AssertTrue();

		schema.Columns.First(c => c.Name == "DataNullBranchMid").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Deep").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Count").IsNullable.AssertTrue();

		schema.Columns.First(c => c.Name == "DataSolidBranchMid").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Deep").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Count").IsNullable.AssertTrue();
	}

	#endregion

	#region [ColumnOverride] attribute

	[TestMethod]
	public void Reflection_ColumnOverride_OverridesNullability()
	{
		var schema = SchemaRegistry.Get(typeof(ReflColumnOverrideEntity));

		// Auth.Key — outer nullable, no override → NULL
		schema.Columns.First(c => c.Name == "AuthKey").IsNullable.AssertTrue();

		// Auth.Secret — outer nullable, but [ColumnOverride] forces NOT NULL
		schema.Columns.First(c => c.Name == "AuthSecret").IsNullable.AssertFalse();
	}

	[TestMethod]
	public void Reflection_ColumnOverride_WithNameOverride()
	{
		var schema = SchemaRegistry.Get(typeof(ReflColumnOverrideWithNameEntity));

		// Key — no NameOverride → default name "AuthKey", outer nullable → NULL
		schema.Columns.First(c => c.Name == "AuthKey").IsNullable.AssertTrue();

		// Secret → renamed to "ApiToken" via NameOverride, forced NOT NULL via ColumnOverride
		schema.Columns.Any(c => c.Name == "AuthSecret").AssertFalse(
			"Secret should be renamed to ApiToken");

		var apiToken = schema.Columns.First(c => c.Name == "ApiToken");
		apiToken.IsNullable.AssertFalse();
	}

	#endregion

	#region SchemaMigrator.Compare — skipComputed

	[TestMethod]
	public void Compare_SkipComputed_ExcludesComputedColumns()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ReflTestOrder),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
		};

		var dbColumns = new List<DbColumnInfo>
		{
			new("TestTable", "Id", "BIGINT", false, null, null, null),
			new("TestTable", "Name", "NVARCHAR", false, null, null, null),
			new("TestTable", "Computed", "NVARCHAR", true, null, null, null, IsComputed: true),
		};

		// Without skipComputed — Computed shows as ExtraColumn
		var diffs = SchemaMigrator.Compare([schema], dbColumns, _dialect, false);
		diffs.Any(d => d.ColumnName == "Computed" && d.Kind == SchemaDiffKind.ExtraColumn).AssertTrue();

		// With skipComputed — Computed is excluded
		var diffsSkipped = SchemaMigrator.Compare([schema], dbColumns, _dialect, skipComputed: true);
		diffsSkipped.Any(d => d.ColumnName == "Computed").AssertFalse();
	}

	#endregion

	#region Foreign key metadata (reflection)

	[TestMethod]
	public void Reflection_RelationSingle_LongRef_CapturesForeignKey()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestGuidRefLongEntity));
		var col = schema.Columns.First(c => c.Name == "LongRef");

		col.ReferencedEntityType.AssertEqual(typeof(ReflTestOrder));
	}

	[TestMethod]
	public void Reflection_RelationSingle_GuidRef_CapturesForeignKey()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestLongRefGuidEntity));
		var col = schema.Columns.First(c => c.Name == "GuidRef");

		col.ReferencedEntityType.AssertEqual(typeof(ReflTestGuidIdEntity));
	}

	[TestMethod]
	public void Reflection_SimpleColumn_NoForeignKey()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestIndexed));
		var col = schema.Columns.First(c => c.Name == "Description");

		col.ReferencedEntityType.AssertNull();
	}

	[TestMethod]
	public void Reflection_InnerSchema_RelationSingle_CapturesForeignKey()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestOrder));
		var col = schema.Columns.First(c => c.Name == "ShippingAddressCountry");

		col.ReferencedEntityType.AssertEqual(typeof(TestCountry));
	}

	#endregion

	#region SchemaMigrator — foreign key DDL

	[TestMethod]
	public void Migrator_MissingTable_WithForeignKey_GeneratesConstraint()
	{
		var schema = new Schema
		{
			TableName = "Orders",
			EntityType = typeof(ReflTestOrder),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
			Columns =
			[
				new SchemaColumn
				{
					Name = "CustomerId",
					ClrType = typeof(long),
					ReferencedEntityType = typeof(TestItem),
				},
			],
		};

		var diffs = new List<SchemaDiff>
		{
			new("Orders", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(_dialect, diffs, [schema]);

		sql.Contains("FOREIGN KEY").AssertTrue($"Expected FOREIGN KEY in SQL, got: {sql}");
		sql.Contains("([CustomerId])").AssertTrue($"Expected ([CustomerId]) in SQL, got: {sql}");
		sql.Contains("REFERENCES [Ecng_TestItem]").AssertTrue($"Expected REFERENCES [Ecng_TestItem], got: {sql}");
		sql.Contains("CONSTRAINT").AssertTrue($"Expected CONSTRAINT clause, got: {sql}");
	}

	[TestMethod]
	public void Migrator_MissingTable_NoForeignKey_DoesNotEmitConstraint()
	{
		var schema = new Schema
		{
			TableName = "Items",
			EntityType = typeof(TestItem),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
		};

		var diffs = new List<SchemaDiff>
		{
			new("Items", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(_dialect, diffs, [schema]);

		sql.Contains("FOREIGN KEY").AssertFalse($"Should not emit FOREIGN KEY for non-FK columns, got: {sql}");
		sql.Contains("CONSTRAINT").AssertFalse($"Should not emit CONSTRAINT for non-FK columns, got: {sql}");
	}

	[TestMethod]
	public void Migrator_MissingColumn_WithForeignKey_EmitsAddConstraint()
	{
		var schema = new Schema
		{
			TableName = "Orders",
			EntityType = typeof(ReflTestOrder),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
			Columns =
			[
				new SchemaColumn
				{
					Name = "CustomerId",
					ClrType = typeof(long),
					IsNullable = true,
					ReferencedEntityType = typeof(TestItem),
				},
			],
		};

		var diffs = new List<SchemaDiff>
		{
			new("Orders", "CustomerId", SchemaDiffKind.MissingColumn, "expected", string.Empty),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(_dialect, diffs, [schema]);

		sql.Contains("ADD [CustomerId]").AssertTrue($"Expected ADD [CustomerId], got: {sql}");
		sql.Contains("FOREIGN KEY").AssertTrue($"Expected FOREIGN KEY, got: {sql}");
		sql.Contains("REFERENCES [Ecng_TestItem]").AssertTrue($"Expected REFERENCES [Ecng_TestItem], got: {sql}");
	}

	#endregion

	#region Finding #3: ViewProcessorAttribute → IsView

	[TestMethod]
	public void Reflection_ViewProcessorAttribute_SetsIsView()
	{
		var schema = SchemaRegistry.Get(typeof(ReflTestViewEntity));

		schema.IsView.AssertTrue(
			"Entity with [ViewProcessor] attribute should have IsView = true");
	}

	#endregion
}

#endif
