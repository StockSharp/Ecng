namespace Ecng.Test.Reflection
{
	using System.Collections.Generic;

	using Ecng.Reflection;
	using Ecng.Reflection.Path;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	class Value
	{
		string GetValue()
		{
			return "Billy Bob";
		}
	}

	class Root
	{
		private Child1 Child = new Child1();

		Value GetValue(decimal value)
		{
			return new Value();
		}
	}

	class Child1
	{
		Child2Collection Childs = new Child2Collection();
	}

	class Child2
	{
		Child3 GetChild3(string name, string name2)
		{
			return new Child3();
		}
	}

	class Child2Collection : List<Child2>
	{
		#region Child2Collection.ctor()

		public Child2Collection()
		{
			Add(new Child2());
		}

		#endregion
	}

	class Child3
	{
		Root GetRoot(int index, string name)
		{
			return new Root();
		}
	}

	/// <summary>
	/// Summary description for ProxyInvokeTest
	/// </summary>
	[TestClass]
	public class ProxyInvokeTest
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
		public void WithParams()
		{
			MemberProxy proxy = typeof(Root).GetMember<MemberProxy>("Child.Childs[@inDex as int].GetChild3(@name as string, @name2).GetRoot(@IndeX, @name3).GetValue(@value).GetValue()");
			Dictionary<string, object> args = new Dictionary<string, object>();
			args.Add("index", 0);
			args.Add("name", "John Smith");
			args.Add("NAMe2", "Mark Twain");
			args.Add("name3", "Billy Bob");
			args.Add("vaLUe", 14);
			Assert.AreEqual("Billy Bob", proxy.Invoke(new Root(), args));
		}

		[TestMethod]
		public void WithoutParams()
		{
			MemberProxy proxy = typeof(Root).GetMember<MemberProxy>("Child.Childs[0].GetChild3('John Smith', 'Mark Twain' as string).GetRoot(0, 'Mark Twain').GetValue(1000 as decimal).GetValue()");
			Assert.AreEqual("Billy Bob", proxy.Invoke(new Root()));
		}
	}
}
