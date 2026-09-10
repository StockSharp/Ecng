namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class EntityPropertyTests
{
	private class TestEntity
	{
		public int Prop1 { get; set; }
		public TestEntity2 Prop2 { get; set; }
	}

	private class TestEntity2
	{
		public int Prop3 { get; set; }
	}

	private interface IExplicit
	{
		int Prop4 { get; }
		TestEntity2 Prop5 { get; }
	}

	private interface IOther
	{
		int Prop4 { get; }
	}

	private class ExplicitEntity : IExplicit
	{
		public int Renamed { get; set; }
		public TestEntity2 Nested { get; set; }

		int IExplicit.Prop4 => Renamed;
		TestEntity2 IExplicit.Prop5 => Nested;
	}

	private class AmbiguousEntity : IExplicit, IOther
	{
		public int Mine { get; set; }
		public int Theirs { get; set; }
		public TestEntity2 Nested { get; set; }

		int IExplicit.Prop4 => Mine;
		TestEntity2 IExplicit.Prop5 => Nested;
		int IOther.Prop4 => Theirs;
	}

	[TestMethod]
	public void Simple()
	{
		new TestEntity { Prop1 = 11 }.GetPropValue(nameof(TestEntity.Prop1)).AssertEqual(11);

		typeof(TestEntity).GetPropType(nameof(TestEntity.Prop1)).AssertEqual(typeof(int));
	}

	[TestMethod]
	public void Complex()
	{
		new TestEntity { Prop2 = new() { Prop3 = 123 } }.GetPropValue("Prop2.Prop3").AssertEqual(123);
		new TestEntity().GetPropValue("Prop2.Prop3").AssertNull();
		new TestEntity().GetPropValue("Prop3.Prop4").AssertNull();

		typeof(TestEntity).GetPropType("Prop2.Prop3").AssertEqual(typeof(int));
		typeof(TestEntity).GetPropType("Prop3.Prop4").AssertNull();
	}

	[TestMethod]
	public void ExplicitInterface()
	{
		// The type is asked of the interface and the value of the instance behind it. An explicitly
		// implemented member is absent from the instance type, so without the interfaces being searched
		// the two answers disagree: a caller is told the property exists and then reads null from it.
		var entity = new ExplicitEntity { Renamed = 11, Nested = new() { Prop3 = 123 } };

		typeof(IExplicit).GetPropType(nameof(IExplicit.Prop4)).AssertEqual(typeof(int));
		((object)entity).GetPropValue(nameof(IExplicit.Prop4)).AssertEqual(11);

		typeof(IExplicit).GetPropType("Prop5.Prop3").AssertEqual(typeof(int));
		((object)entity).GetPropValue("Prop5.Prop3").AssertEqual(123);

		((object)entity).GetPropValue("Prop6").AssertNull();
	}

	[TestMethod]
	public void ExplicitInterfaceAmbiguous()
	{
		// Two interfaces give the same name two meanings. Read against the object alone there is nothing
		// to choose by, so nothing is read; named against the interface the caller asked the type of, the
		// answer is the one that interface declares.
		var entity = new AmbiguousEntity { Mine = 11, Theirs = 22, Nested = new() { Prop3 = 123 } };

		((object)entity).GetPropValue(nameof(IExplicit.Prop4)).AssertNull();

		((object)entity).GetPropValue(typeof(IExplicit), nameof(IExplicit.Prop4)).AssertEqual(11);
		((object)entity).GetPropValue(typeof(IOther), nameof(IOther.Prop4)).AssertEqual(22);

		((object)entity).GetPropValue(typeof(IExplicit), "Prop5.Prop3").AssertEqual(123);
	}

	[TestMethod]
	public void Virtual()
	{
		new TestEntity { Prop2 = new() { Prop3 = 123 } }.GetPropValue("Prop2.VirtProp.Prop1", (o, n) =>
		{
			if (o is TestEntity2 && n == "VirtProp")
				return new TestEntity { Prop1 = 456 };

			return null;
		}).AssertEqual(456);

		new TestEntity { Prop2 = new() { Prop3 = 123 } }.GetPropValue("Prop2.VirtProp2.Prop1", (o, n) =>
		{
			if (o is TestEntity2 && n == "VirtProp")
				return new TestEntity { Prop1 = 456 };

			return null;
		}).AssertNull();

		new TestEntity().GetPropValue("VirtProp.Prop1", (o, n) =>
		{
			if (o is TestEntity && n == "VirtProp")
				return new TestEntity { Prop1 = 456 };

			return null;
		}).AssertEqual(456);

		typeof(TestEntity).GetPropType("Prop2.VirtProp.Prop1", (t, n) =>
		{
			if (t == typeof(TestEntity2) && n == "VirtProp")
				return typeof(TestEntity);

			return null;
		}).AssertEqual(typeof(int));

		typeof(TestEntity).GetPropType("Prop2.VirtProp2.Prop1", (t, n) =>
		{
			if (t == typeof(TestEntity2) && n == "VirtProp")
				return typeof(TestEntity);

			return null;
		}).AssertNull();

		typeof(TestEntity).GetPropType("VirtProp.Prop1", (t, n) =>
		{
			if (t == typeof(TestEntity) && n == "VirtProp")
				return typeof(TestEntity);

			return null;
		}).AssertEqual(typeof(int));
	}

	private class TestIndexerEntity
	{
		public int[] Arr { get; set; }
		public TestEntity[] Arr2 { get; set; }
		public IDictionary<string, TestEntity> Dict { get; set; }
		public IDictionary<DayOfWeek, TestEntity2> Dict2 { get; set; }
	}

	[TestMethod]
	public void Indexer()
	{
		var entity = new TestIndexerEntity
		{
			Arr = [1, 4, 7],
			Arr2 =
			[
				new TestEntity { Prop1 = 123 },
				new TestEntity { Prop1 = 456 },
				new TestEntity { Prop1 = 789 },
			],
			Dict = new Dictionary<string, TestEntity>
			{
				{ "123", new TestEntity { Prop1 = 123 } },
				{ "456", new TestEntity { Prop1 = 456 } },
				{ "789", new TestEntity { Prop1 = 789 } },
			},
			Dict2 = new Dictionary<DayOfWeek, TestEntity2>
			{
				{ DayOfWeek.Monday, new TestEntity2 { Prop3 = 123 } },
				{ DayOfWeek.Sunday, new TestEntity2 { Prop3 = 456 } },
				{ DayOfWeek.Wednesday, new TestEntity2 { Prop3 = 789 } },
			}
		};

		entity.GetPropValue("Arr[1]").AssertEqual(4);
		entity.GetPropValue("Arr[5]").AssertNull();

		entity.GetPropValue("Arr2[1].Prop1").AssertEqual(456);
		entity.GetPropValue("Arr2[5].Prop1").AssertNull();

		entity.GetPropValue("Dict[123].Prop1").AssertEqual(123);
		entity.GetPropValue("Dict[000].Prop1").AssertNull();

		entity.GetPropValue("Dict2[Monday].Prop3").AssertEqual(123);
		entity.GetPropValue("Dict2[Saturday].Prop3").AssertNull();

		entity.GetPropValue("Dict2[Monday].VirtProp.Prop1", (o, n) =>
		{
			if (o is TestEntity2 && n == "VirtProp")
				return new TestEntity { Prop1 = 456 };

			return null;
		}).AssertEqual(456);
		entity.GetPropValue("Dict2[Monday].VirtProp[1]", (o, n) =>
		{
			if (o is TestEntity2 && n == "VirtProp")
				return new[] { 1, 4, 7 };

			return null;
		}).AssertEqual(4);
		entity.GetPropValue("Dict2[Monday].VirtProp[5]", (o, n) =>
		{
			if (o is TestEntity2 && n == "VirtProp")
				return new[] { 1, 4, 7 };

			return null;
		}).AssertNull();

		typeof(TestIndexerEntity).GetPropType("Arr[1]").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Arr[5]").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Arr3[5]").AssertNull();
		typeof(TestIndexerEntity).GetPropType("Arr2[1].Prop1").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Arr2[5].Prop1").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict[123].Prop1").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict[123].Prop1").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict[123].Prop3").AssertNull();

		typeof(TestIndexerEntity).GetPropType("Dict2[Monday].Prop3").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict2[Saturday].Prop3").AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict2[Saturday].Prop4").AssertNull();

		typeof(TestIndexerEntity).GetPropType("Dict2[Monday].VirtProp.Prop1", (o, n) =>
		{
			if (o == typeof(TestEntity2) && n == "VirtProp")
				return typeof(TestEntity);

			return null;
		}).AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict2[Monday].VirtProp[1]", (o, n) =>
		{
			if (o == typeof(TestEntity2) && n == "VirtProp")
				return typeof(int[]);

			return null;
		}).AssertEqual(typeof(int));
		typeof(TestIndexerEntity).GetPropType("Dict2[Monday].VirtProp[5]", (o, n) =>
		{
			if (o == typeof(TestEntity2) && n == "VirtProp")
				return typeof(string[]);

			return null;
		}).AssertEqual(typeof(string));

	}

	[TestMethod]
	public void Vars()
	{
		var vars = typeof(TestIndexerEntity).GetVars("Arr2[a].VirtProp.Dict[x].Prop1", (o, n) =>
		{
			if (o == typeof(TestEntity) && n == "VirtProp")
				return typeof(TestIndexerEntity);
			
			return null;
		}).ToArray();

		vars.Length.AssertEqual(1);
		vars[0].AssertEqual("a");

		var entity = new TestIndexerEntity
		{
			Arr2 = [new TestEntity(), new TestEntity()]
		};

		var obj = entity.GetPropValue("Arr2[a].VirtProp.Dict[x].Prop1", (o, n) =>
		{
			if (o is TestEntity && n == "VirtProp")
				return new TestIndexerEntity { Dict = new Dictionary<string, TestEntity> { { "x", new TestEntity { Prop1 = 123 } } } };

			return null;
		}, vars: new Dictionary<string, object> { { "a", 0 } });

		obj.AssertEqual(123);

		//obj = entity.GetPropValue("Arr2[a].VirtProp.Dict[x].Prop1", (o, n) =>
		//{
		//	if (o is TestEntity && n == "VirtProp")
		//		return new TestIndexerEntity { Dict = new Dictionary<string, TestEntity> { { "x", new TestEntity { Prop1 = 123 } } } };

		//	return null;
		//}, vars: new Dictionary<string, object> { { "b", 0 } });

		//obj.AssertNull();

		obj = entity.GetPropValue("Arr2[a].VirtProp.Dict[x].Prop1", (o, n) =>
		{
			if (o is TestEntity && n == "VirtProp")
				return new TestIndexerEntity { Dict = new Dictionary<string, TestEntity> { { "x", new TestEntity { Prop1 = 123 } } } };

			return null;
		}, vars: new Dictionary<string, object> { { "a", 4 } });

		obj.AssertNull();
	}
}
