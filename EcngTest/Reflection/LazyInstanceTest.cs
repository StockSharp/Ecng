namespace Ecng.Test.Reflection
{
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	public class LazyInstanceValue
	{
		#region LazyInstanceValue.ctor()

		public LazyInstanceValue()
		{
		}

		public LazyInstanceValue(string field)
		{
			Field = field;
		}

		#endregion

		public string Field;
	}

	public struct LazyInstanceValue2
	{
		#region LazyInstanceValue2.ctor()

		public LazyInstanceValue2(int field)
		{
			Field = field;
			Value = null;
		}

		public LazyInstanceValue2(ComplexValue value)
		{
			Field = 0;
			Value = value;
		}

		#endregion

		public int Field;
		public ComplexValue Value;
	}

	public class ComplexValue
	{
		public int Field = 100;
	}

	public class ComplexLazyInstanceAttribute : LazyInstanceAttribute
	{
		#region ComplexLazyInstanceAttribute.ctor()

		public ComplexLazyInstanceAttribute()
			: base(new ComplexValue())
		{
		}

		#endregion
	}

	[MetaExtension]
	public abstract class RefEmptyEntity
	{
		#region EmptyCtor

		[LazyInstance]
		public abstract LazyInstanceValue EmptyCtor { get; }

		#endregion
	}

	[MetaExtension]
	public abstract class RefValuesEntity
	{
		#region ValuesCtor

		[LazyInstance("Mark Twain")]
		public abstract LazyInstanceValue ValuesCtor { get; }

		#endregion
	}

	[MetaExtension]
	public abstract class StructEmptyEntity
	{
		[LazyInstance]
		public abstract LazyInstanceValue2 EmptyStructCtor { get; }
	}

	[MetaExtension]
	public abstract class StructValuesEntity
	{
		[LazyInstance(10)]
		public abstract LazyInstanceValue2 ValuesStructCtor { get; }
	}

	[MetaExtension]
	public abstract class StructComplexEntity
	{
		[ComplexLazyInstance]
		public abstract LazyInstanceValue2 ComplexStructCtor { get; }
	}

	/// <summary>
	/// Summary description for LazyInstanceTest
	/// </summary>
	[TestClass]
	public class LazyInstanceTest
	{
		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		[TestMethod]
		public void EmptyCtor()
		{
			var entity = Create<RefEmptyEntity>();
			Assert.IsNotNull(entity.EmptyCtor);
		}

		[TestMethod]
		public void ValuesCtor()
		{
			var entity = Create<RefValuesEntity>();
			Assert.IsNotNull(entity.ValuesCtor);
			Assert.AreEqual("Mark Twain", entity.ValuesCtor.Field);
		}

		[TestMethod]
		public void EmptyStructCtor()
		{
			var entity = Create<StructEmptyEntity>();
			Assert.IsNotNull(entity.EmptyStructCtor);
		}

		[TestMethod]
		public void ValuesStructCtor()
		{
			var entity = Create<StructValuesEntity>();
			Assert.IsNotNull(entity.ValuesStructCtor);
			Assert.AreEqual(10, entity.ValuesStructCtor.Field);
		}

		[TestMethod]
		public void ComplexStructCtor()
		{
			var entity = Create<StructComplexEntity>();
			Assert.IsNotNull(entity.ComplexStructCtor);
			Assert.IsNotNull(entity.ComplexStructCtor.Value);
			Assert.AreEqual(100, entity.ComplexStructCtor.Value.Field);
		}

		private static T Create<T>()
		{
			var entity = MetaExtension.Create(typeof(T)).CreateInstance<T>();

			foreach (FieldInfo field in typeof(T).GetMembers<FieldInfo>())
				Assert.IsNull(field.GetValue(entity));

			return entity;
		}
	}
}