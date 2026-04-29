#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data.Sql.Model;
using Ecng.Serialization;

/// <summary>
/// Tests the pure resolver that maps a <see cref="MemberExpression"/> chain
/// to a <see cref="MemberPathResolution"/>. The resolver is the foundation
/// for unifying the five inline navigation-resolution sites in
/// ExpressionQueryTranslator.
/// </summary>
[TestClass]
public class MemberPathResolverTests : BaseTestClass
{
	private const string RootAlias = "e";

	[ClassInitialize]
	public static void ClassInit(TestContext _)
	{
		// Register schemas so the resolver can find target tables/identities
		// for [RelationSingle] hops it traverses.
		SchemaRegistry.Get(typeof(TestPerson));
		SchemaRegistry.Get(typeof(TestTask));
		SchemaRegistry.Get(typeof(TestSubTask));

		// TestCountry has no [Entity] attribute (it is registered manually
		// in InnerSchemaTests). Register it explicitly so the inner-schema
		// + RelationSingle scenarios below resolve their target table.
		SchemaRegistry.Register(new Schema
		{
			TableName = "Ecng_Country",
			EntityType = typeof(TestCountry),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
			Columns = [new SchemaColumn { Name = "Name", ClrType = typeof(string) }],
			Factory = () => new TestCountry(),
		});
	}

	private static MemberExpression Body<T, TR>(Expression<Func<T, TR>> e)
	{
		var body = e.Body;

		// Expression<Func<T, object>> over a value-type member emits Convert(member, object).
		if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
			body = u.Operand;

		return (MemberExpression)body;
	}

	[TestMethod]
	public void Resolve_SimpleColumn_NoJoins()
	{
		var res = MemberPathResolver.Resolve(Body((TestPerson p) => p.Name), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "Name"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Resolve_NavigationLeafIsId_UsesFkShortcut()
	{
		// e.Person.Id → reads the FK column directly off the parent table.
		// Must not introduce a JOIN.
		var res = MemberPathResolver.Resolve(Body((TestTask t) => t.Person.Id), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "Person"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Resolve_NavigationLeafIsNonId_AddsInnerJoinAndQualifiesColumn()
	{
		// e.Person.Name → INNER JOIN Person, qualify Name with the join alias.
		var res = MemberPathResolver.Resolve(Body((TestTask t) => t.Person.Name), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef("Person", "Name"));
		res.RequiredJoins.Count.AssertEqual(1);

		var join = res.RequiredJoins[0];
		join.Kind.AssertEqual(JoinKind.Inner);
		join.Alias.AssertEqual("Person");
		join.ParentAlias.AssertEqual(RootAlias);
		join.OnParentColumn.AssertEqual("Person");
		join.OnChildColumn.AssertEqual("Id");
	}

	[TestMethod]
	public void Resolve_TwoLevelNavigationLeafIsId_OneJoinPlusFkShortcut()
	{
		// s.Task.Person.Id → 1 JOIN to Task, then FK shortcut to Person column on Task.
		var res = MemberPathResolver.Resolve(Body((TestSubTask s) => s.Task.Person.Id), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef("Task", "Person"));
		res.RequiredJoins.Count.AssertEqual(1);

		var taskJoin = res.RequiredJoins[0];
		taskJoin.Alias.AssertEqual("Task");
		taskJoin.ParentAlias.AssertEqual(RootAlias);
		taskJoin.OnParentColumn.AssertEqual("Task");
		taskJoin.OnChildColumn.AssertEqual("Id");
	}

	[TestMethod]
	public void Resolve_TwoLevelNavigationLeafIsNonId_TwoJoinsInOrder()
	{
		// s.Task.Person.Name → JOIN Task, JOIN Person, qualify Name with Person alias.
		var res = MemberPathResolver.Resolve(Body((TestSubTask s) => s.Task.Person.Name), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef("Person", "Name"));
		res.RequiredJoins.Count.AssertEqual(2);

		// Joins must appear parent-first so the dependent FROM-clause builds correctly.
		var first = res.RequiredJoins[0];
		first.Alias.AssertEqual("Task");
		first.ParentAlias.AssertEqual(RootAlias);

		var second = res.RequiredJoins[1];
		second.Alias.AssertEqual("Person");
		second.ParentAlias.AssertEqual("Task");
		second.OnParentColumn.AssertEqual("Person");
		second.OnChildColumn.AssertEqual("Id");
	}

	[TestMethod]
	public void Resolve_InnerSchema_FlattensIntoSingleColumn()
	{
		// e.ShippingAddress.Street → no JOIN; column name accumulates the
		// inner-schema prefix into a single physical column on the root table.
		var res = MemberPathResolver.Resolve(Body((TestOrderWithAddress o) => o.ShippingAddress.Street), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "ShippingAddressStreet"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Resolve_NestedInnerSchema_FlattensWithMultiplePrefixes()
	{
		// e.ShippingAddress.GeoCoord.Lat → two inner-schema hops collapse
		// into a single flattened column ShippingAddressGeoCoordLat.
		var res = MemberPathResolver.Resolve(Body((TestOrderWithAddressEx o) => o.ShippingAddress.GeoCoord.Lat), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "ShippingAddressGeoCoordLat"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Resolve_RelationSingleInsideInnerSchema_LeafIsId_FkShortcutWithPrefix()
	{
		// e.ShippingAddress.Country.Id → no JOIN; FK column on the parent
		// table carries the prefix accumulated from the inner-schema hop:
		// ShippingAddress + Country = ShippingAddressCountry.
		var res = MemberPathResolver.Resolve(Body((TestOrderWithAddressEx o) => o.ShippingAddress.Country.Id), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "ShippingAddressCountry"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Resolve_RelationSingleInsideInnerSchema_LeafIsNonId_AddsJoinOnPrefixedFk()
	{
		// e.ShippingAddress.Country.Name → INNER JOIN Country ON the
		// inner-schema-prefixed FK column on the parent (ShippingAddressCountry)
		// matches the joined table's identity.
		var res = MemberPathResolver.Resolve(Body((TestOrderWithAddressEx o) => o.ShippingAddress.Country.Name), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef("Country", "Name"));
		res.RequiredJoins.Count.AssertEqual(1);

		var join = res.RequiredJoins[0];
		join.Kind.AssertEqual(JoinKind.Inner);
		join.Alias.AssertEqual("Country");
		join.ParentAlias.AssertEqual(RootAlias);
		join.OnParentColumn.AssertEqual("ShippingAddressCountry");
		join.OnChildColumn.AssertEqual("Id");
	}

	[TestMethod]
	public void Resolve_ChainNotRootedAtParameter_ReturnsNull()
	{
		// `someValue.X.Y` — closure capture chain. Resolver must refuse it
		// so the caller can route to the closure-materialiser path.
		var holder = new { Inner = new TestPerson { Name = "x" } };
		Expression<Func<TestTask, string>> e = _ => holder.Inner.Name;
		var body = (MemberExpression)e.Body;

		var res = MemberPathResolver.Resolve(body, RootAlias);

		res.AssertNull();
	}

}

#endif
