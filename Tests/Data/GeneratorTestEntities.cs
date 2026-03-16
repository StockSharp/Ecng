#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;

using Ecng.Serialization;

public abstract partial class GenTestBaseEntity : IDbPersistable
{
	[Identity]
	[ReadOnly(true)]
	public long Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public virtual void Save(SettingsStorage storage) { }
	public virtual ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
	public virtual void InitLists(IStorage db) { }
}

[Entity(Name = "Ecng_Orders", NoCache = true)]
public partial class GenTestOrderEntity : GenTestBaseEntity
{
	public string Symbol { get; set; }
	public decimal Price { get; set; }
}

[Entity(Name = "Ecng_Products")]
public partial class GenTestProductEntity : GenTestBaseEntity
{
	public string Title { get; set; }
}

public partial class GenTestPlainEntity : GenTestBaseEntity
{
	public string Value { get; set; }
}

[Entity(Name = "Ecng_ColGen")]
public partial class GenTestColumnAttrEntity : GenTestBaseEntity
{
	[Column(MaxLength = 128)]
	public string Name { get; set; }

	[Column(IsNullable = true)]
	public string Description { get; set; }

	[Column(IsNullable = true, MaxLength = 64)]
	public string Tag { get; set; }

	public string Plain { get; set; }

	public int? NullableInt { get; set; }

	public int RequiredInt { get; set; }
}

/// <summary>
/// Base entity with int identity instead of long.
/// </summary>
public abstract partial class GenTestIntIdBase : IDbPersistable
{
	[Identity]
	[ReadOnly(true)]
	public int Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<int>();
	public virtual void Save(SettingsStorage storage) { }
	public virtual ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
	public virtual void InitLists(IStorage db) { }
}

[Entity(Name = "Ecng_IntId")]
public partial class GenTestIntIdEntity : GenTestIntIdBase
{
	public string Name { get; set; }
}

/// <summary>
/// Base entity with Guid identity.
/// </summary>
public abstract partial class GenTestGuidIdBase : IDbPersistable
{
	[Identity]
	[ReadOnly(true)]
	public Guid Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<Guid>();
	public virtual void Save(SettingsStorage storage) { }
	public virtual ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
	public virtual void InitLists(IStorage db) { }
}

[Entity(Name = "Ecng_GuidId")]
public partial class GenTestGuidIdEntity : GenTestGuidIdBase
{
	public string Title { get; set; }

	/// <summary>
	/// Reference to a long-id entity — FK column type must be long.
	/// </summary>
	[RelationSingle]
	public GenTestOrderEntity Order { get; set; }
}

/// <summary>
/// Entity with long identity referencing a Guid-identity entity.
/// FK column type must be Guid, not long.
/// </summary>
[Entity(Name = "Ecng_LongRefGuid")]
public partial class GenTestLongRefGuidEntity : GenTestBaseEntity
{
	[RelationSingle]
	public GenTestGuidIdEntity GuidRef { get; set; }

	public string Note { get; set; }
}

/// <summary>
/// Simple inner schema type for flattening tests.
/// </summary>
public class GenTestAddress
{
	public string Street { get; set; }
	public string City { get; set; }
}

/// <summary>
/// Entity with nullable inner schema property.
/// </summary>
[Entity(Name = "Ecng_NullInner")]
public partial class GenTestNullableInnerEntity : GenTestBaseEntity
{
	[Column(IsNullable = true)]
	public GenTestAddress Address { get; set; }

	public string Code { get; set; }
}

// ===== Enum tests =====

public enum GenTestStatus
{
	Active,
	Inactive,
	Deleted,
}

[Entity(Name = "Ecng_EnumTest")]
public partial class GenTestEnumEntity : GenTestBaseEntity
{
	public GenTestStatus Status { get; set; }
	public GenTestStatus? NullableStatus { get; set; }
	public string Label { get; set; }
}

// ===== Shared inner schema types for deep nullable propagation tests =====

/// <summary>Level 4 (deepest leaf).</summary>
public class NullPropL4
{
	public string Deep { get; set; }
	public int Count { get; set; }
}

/// <summary>Level 3.</summary>
public class NullPropL3
{
	public string Mid { get; set; }
	public NullPropL4 L4 { get; set; }
}

/// <summary>
/// Level 2 with two branches:
/// NullBranch is [Column(IsNullable = true)] — nullable introduced mid-tree.
/// SolidBranch has no nullable marker.
/// </summary>
public class NullPropL2
{
	public string Top { get; set; }

	[Column(IsNullable = true)]
	public NullPropL3 NullBranch { get; set; }

	public NullPropL3 SolidBranch { get; set; }
}

/// <summary>
/// Level 2 where inner tries to cancel outer nullable via [Column(IsNullable = false)].
/// </summary>
public class NullPropL2Strict
{
	[Column(IsNullable = false)]
	public string Forced { get; set; }

	public string Normal { get; set; }
}

// ===== Generator test entities using shared inner types =====

/// <summary>
/// Outer NOT NULL → nullable introduced at mid-level via NullBranch.
/// SolidBranch stays NOT NULL. Tests 4-level propagation with branching.
/// </summary>
[Entity(Name = "Ecng_GenNullMid")]
public partial class GenTestNullMidEntity : GenTestBaseEntity
{
	public NullPropL2 Data { get; set; }
}

/// <summary>
/// Outer nullable, inner tries [Column(IsNullable = false)] — outer must win.
/// </summary>
[Entity(Name = "Ecng_GenNullCantCancel")]
public partial class GenTestNullCantCancelEntity : GenTestBaseEntity
{
	[Column(IsNullable = true)]
	public NullPropL2Strict Data { get; set; }
}

/// <summary>
/// Outer nullable at root, entire 4-level tree must be nullable.
/// </summary>
[Entity(Name = "Ecng_GenNullRoot")]
public partial class GenTestNullRootEntity : GenTestBaseEntity
{
	[Column(IsNullable = true)]
	public NullPropL2 Data { get; set; }
}

/// <summary>
/// Inner value object for ColumnOverride tests.
/// Key and Secret are both non-nullable string properties.
/// </summary>
public class TestKeySecret
{
	public string Key { get; set; }
	public string Secret { get; set; }
}

/// <summary>
/// Outer nullable, but [ColumnOverride] forces Secret to NOT NULL.
/// Key remains nullable (inherits outer).
/// </summary>
[Entity(Name = "Ecng_GenColOverride")]
public partial class GenTestColumnOverrideEntity : GenTestBaseEntity
{
	[Column(IsNullable = true)]
	[ColumnOverride(nameof(TestKeySecret.Secret), IsNullable = false)]
	public TestKeySecret Auth { get; set; }
}

#endif
