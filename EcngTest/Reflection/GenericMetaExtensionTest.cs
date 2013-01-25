namespace Ecng.Test.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[MetaExtension]
	public class GenericExtension<T>
	{
		[DefaultImp]
		public virtual void Method()
		{
			throw new NotSupportedException();
		}

		#region Prop1

		[DefaultImp]
		public virtual int Prop1
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		#endregion
	}

	[MetaExtension]
	public interface IGenericExtension<T>
	{
		[DefaultImp]
		void Method();

		[DefaultImp]
		int Prop1 { get; set; }
	}

	[MetaExtension]
	public class GenericExtension2<T>
		where T : new()
	{
		[DefaultImp]
		public virtual T Method()
		{
			throw new NotSupportedException();
		}

		#region Prop1

		[DefaultImp]
		public virtual T Prop1
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		#endregion
	}

	[MetaExtension]
	public interface IGenericExtension2<T>
		where T : new()
	{
		[DefaultImp]
		T Method();

		[DefaultImp]
		T Prop1 { get; set; }
	}

	[MetaExtension]
	public class GenericExtension3
	{
		[DefaultImp]
		public virtual void Method<T>()
			where T : class
		{
			throw new NotSupportedException();
		}
	}

	[MetaExtension]
	public interface IGenericExtension3
	{
		[DefaultImp]
		void Method<T>()
			where T : class;
	}

	[MetaExtension]
	public abstract class MultipleGenericExtension<T1, T2>
		where T1 : class
		where T2 : struct
	{
		[DefaultImp]
		public abstract T1 Prop1 { get; set; }

		[DefaultImp]
		public abstract T2 Prop2 { get; set; }
	}

	[MetaExtension]
	public interface IMultipleGenericExtension
	{
		[DefaultImp]
		T2 Method<T1, T2>(T1 value)
			where T1 : class
			where T2 : struct;
	}

	/// <summary>
	/// Summary description for GenericMetaExtansionTest
	/// </summary>
	[TestClass]
	public class GenericMetaExtensionTest
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
		public void GenericClass()
		{
			nTest<GenericExtension<int>>().Method();
		}

		[TestMethod]
		public void GenericInterface()
		{
			nTest<IGenericExtension<int>>().Method();
		}

		[TestMethod]
		public void GenericClass2()
		{
			var obj = nTest<GenericExtension<int>>();
			obj.Prop1 = 10;
			Assert.AreEqual(10, obj.Prop1);
		}

		[TestMethod]
		public void GenericInterface2()
		{
			var obj = nTest<IGenericExtension<int>>();
			obj.Prop1 = 10;
			Assert.AreEqual(10, obj.Prop1);
		}

		[TestMethod]
		public void LateConstructGenericClass()
		{
			Type entityType = MetaExtension.Create(typeof(GenericExtension<>));
			var obj = entityType.Make(typeof(int)).CreateInstance<GenericExtension<int>>();
			obj.Method();
		}

		[TestMethod]
		public void LateConstructGenericInterface()
		{
			Type entityType = MetaExtension.Create(typeof(IGenericExtension<>));
			var obj = entityType.Make(typeof(int)).CreateInstance<IGenericExtension<int>>();
			obj.Method();
		}

		[TestMethod]
		public void GenericClassMethod()
		{
			Assert.AreEqual(0, nTest<GenericExtension2<int>>().Method());
		}

		[TestMethod]
		public void GenericInterfaceMethod()
		{
			Assert.AreEqual(0, nTest<IGenericExtension2<int>>().Method());
		}

		[TestMethod]
		public void GenericClassMethod2()
		{
			var obj = nTest<GenericExtension2<int>>();
			obj.Prop1 = 10;
			Assert.AreEqual(10, obj.Prop1);
		}

		[TestMethod]
		public void GenericInterfaceMethod2()
		{
			var obj = nTest<IGenericExtension2<int>>();
			obj.Prop1 = 10;
			Assert.AreEqual(10, obj.Prop1);
		}

		[TestMethod]
		public void LateConstructGenericClassMethod()
		{
			Type entityType = MetaExtension.Create(typeof(GenericExtension2<>));
			var obj = entityType.Make(typeof(int)).CreateInstance<GenericExtension2<int>>();
			Assert.AreEqual(0, obj.Method());
		}

		[TestMethod]
		public void LateConstructGenericInterfaceMethod()
		{
			Type entityType = MetaExtension.Create(typeof(IGenericExtension2<>));
			var obj = entityType.Make(typeof(int)).CreateInstance<IGenericExtension2<int>>();
			Assert.AreEqual(0, obj.Method());
		}

		[TestMethod]
		public void GenericMethod()
		{
			nTest<GenericExtension3>().Method<object>();
		}

		[TestMethod]
		public void GenericMethodInterface()
		{
			nTest<IGenericExtension3>().Method<object>();
		}

		[TestMethod]
		public void MultipleGeneric()
		{
			var obj = nTest<MultipleGenericExtension<string, int>>();
			obj.Prop1 = "Mark Twain";
			Assert.AreEqual("Mark Twain", obj.Prop1);
			obj.Prop2 = 10;
			Assert.AreEqual(10, obj.Prop2);
		}

		[TestMethod]
		public void MultipleGenericInterface()
		{
			var obj = nTest<IMultipleGenericExtension>();
			Assert.AreEqual(0, obj.Method<string, int>(null));
		}

		[TestMethod]
		public void ConstraintMethod()
		{
			var entity = nTest<GenericExtension3>();
			entity.GetType().GetMember<MethodInfo>("Method").Make(typeof(string));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ConstraintMethod2()
		{
			var entity = nTest<GenericExtension3>();
			entity.GetType().GetMember<MethodInfo>("Method").Make(typeof(int));
		}

		[TestMethod]
		public void ConstraintMethodInterface()
		{
			var entity = nTest<IGenericExtension3>();
			entity.GetType().GetMember<MethodInfo>("Method").Make(typeof(string));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ConstraintMethodInterface2()
		{
			var entity = nTest<IGenericExtension3>();
			entity.GetType().GetMember<MethodInfo>("Method").Make(typeof(int));
		}

		[TestMethod]
		public void ConstraintClass()
		{
			MetaExtension.Create(typeof(GenericExtension2<>)).Make(typeof(int));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ConstraintClass2()
		{
			MetaExtension.Create(typeof(GenericExtension2<>)).Make(typeof(string));
		}

		[TestMethod]
		public void ConstraintInterface()
		{
			MetaExtension.Create(typeof(IGenericExtension2<>)).Make(typeof(List<string>));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ConstraintInterface2()
		{
			MetaExtension.Create(typeof(IGenericExtension2<>)).Make(typeof(string));
		}

		private static T nTest<T>()
		{
			return MetaExtension.Create(typeof(T)).CreateInstance<T>();
		}
	}
}
