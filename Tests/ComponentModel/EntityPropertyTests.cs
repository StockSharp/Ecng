namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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

	[TestMethod]
	public void Simple()
	{
		new TestEntity { Prop1 = 11 }.GetPropValue(nameof(TestEntity.Prop1)).AssertEqual(11);
	}

	[TestMethod]
	public void Complex()
	{
		new TestEntity { Prop2 = new() { Prop3 = 123 } }.GetPropValue("Prop2.Prop3").AssertEqual(123);
		new TestEntity().GetPropValue("Prop2.Prop3").AssertNull();
		new TestEntity().GetPropValue("Prop3.Prop4").AssertNull();
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
	}
}
