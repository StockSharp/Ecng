namespace Ecng.Test.Reflection
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[MetaExtension]
	public class ValidatorEntity
	{
		[Interceptor(typeof(ValidatorInterceptor))]
		public ValidatorEntity([String(5, int.MaxValue)]string s)
		{
			Assert.IsNotNull(s);
			Assert.IsTrue(s.Length >= 5);
		}

		[Interceptor(typeof(ValidatorInterceptor))]
		public ValidatorEntity([NotNull]ref string s, DateTime i)
		{
			Assert.IsNotNull(s);
			s = null;
		}

		[Interceptor(typeof(ValidatorInterceptor))]
		public ValidatorEntity(string s, [Range(MinValue = 10, MaxValue = 1000)]out int value)
		{
			value = 1;
		}

		#region Prop1

		private string _prop1;

		[String(10, int.MaxValue)]
		public virtual string Prop1
		{
			[Interceptor(typeof(ValidatorInterceptor))]
			get { return _prop1; }
			[Interceptor(typeof(ValidatorInterceptor))]
			set { _prop1 = value; }
		}

		#endregion

		[NotNull]
		public virtual object this[[Range(MaxValue = 3)]int index]
		{
			[Interceptor(typeof(ValidatorInterceptor))]
			get { return null; }
			[Interceptor(typeof(ValidatorInterceptor))]
			set { /* set the specified index to value here */ }
		}

		private EventHandler _event1;

		[NotNull]
		public virtual event EventHandler Event1
		{
			[Interceptor(typeof(ValidatorInterceptor))]
			add { _event1 = value; }
			remove { _event1 = value; }
		}

		private EventHandler _event2;

		[NotNull]
		public virtual event EventHandler Event2
		{
			add { _event2 = value; }
			[Interceptor(typeof(ValidatorInterceptor))]
			remove { _event2 = value; }
		}
	}

	public class InterceptorScope : Interceptor
	{
		public static List<string> Names = new List<string>();

		protected override void BeforeCall(InterceptContext context)
		{
			Names.Add(context.MethodName);
			base.BeforeCall(context);
		}
	}

	[MetaExtension]
	public class InterceptorScopeEntity
	{
		[Interceptor(typeof(InterceptorScope))]
		public virtual void Method()
		{
			Method2();
		}

		[Interceptor(typeof(InterceptorScope))]
		public virtual void Method2()
		{
		}
	}

	/// <summary>
	/// Summary description for InterceptorValidatorTest
	/// </summary>
	[TestClass]
	public class InterceptorValidatorTest
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
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Ctor()
		{
			Type entityType = MetaExtension.Create(typeof(ValidatorEntity));
			entityType.CreateInstance(string.Empty);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CtorRefArg()
		{
			Type entityType = MetaExtension.Create(typeof(ValidatorEntity));
			entityType.CreateInstance(new object[] { "Mark Twain", DateTime.Now });
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CtorOutArg()
		{
			Type entityType = MetaExtension.Create(typeof(ValidatorEntity));
			entityType.CreateInstance(new object[] { "Mark Twain", 0 });
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void SetProp()
		{
			nCreate().Prop1 = "123456789";
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetProp()
		{
			string s = nCreate().Prop1;
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void SetIndexer()
		{
			nCreate()[10] = new object();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetIndexer()
		{
			object o = nCreate()[0];
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddEvent()
		{
			nCreate().Event1 += null;
		}

		[TestMethod]
		public void RemoveEvent()
		{
			nCreate().Event1 -= null;
		}

		[TestMethod]
		public void AddEvent2()
		{
			nCreate().Event2 += null;
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEvent2()
		{
			nCreate().Event2 -= null;
		}

		[TestMethod]
		public void Scope()
		{
			var entity = MetaExtension.Create(typeof(InterceptorScopeEntity)).CreateInstance<InterceptorScopeEntity>();
			entity.Method();
			Assert.AreEqual(2, InterceptorScope.Names.Count);
			Assert.AreEqual("Method", InterceptorScope.Names[0]);
			Assert.AreEqual("Method2", InterceptorScope.Names[1]);
		}

		private static ValidatorEntity nCreate()
		{
			Type entityType = MetaExtension.Create(typeof(ValidatorEntity));
			return entityType.CreateInstance<ValidatorEntity>("Mark Twain");
		}
	}
}
