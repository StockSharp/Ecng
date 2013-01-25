//using Ecng.Data;

namespace Ecng.Test.Reflection
{
	using System;
	using System.Reflection;

	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[MetaExtension]
	public abstract class AbsClass
	{
		#region IntProp

		[NotImpException]
		public abstract int IntProp { get; set; }

		#endregion

		#region IntProp2

		[DefaultImp]
		public abstract int IntProp2 { get; set; }

		#endregion

		#region StringProp

		[NotImpException]
		public abstract string StringProp { get; set; }

		#endregion

		#region StringProp2

		[DefaultImp]
		public abstract string StringProp2 { get; set; }

		#endregion

		#region CustomAccessorProp

		public abstract string CustomAccessorProp { [DefaultImp]get; [NotImpException]set; }

		#endregion

		#region CustomAccessorProp2

		[NotImpException]
		public abstract string CustomAccessorProp2 { [DefaultImp]get; set; }

		#endregion

		#region CustomAccessorProp3

		[DefaultImp]
		public abstract string CustomAccessorProp3 { [NotImpException]get; set; }

		#endregion

		#region CustomAccessorProp4

		public virtual string CustomAccessorProp4
		{
			get { return "Mark Twain"; }
			[DefaultImp]
			set { throw new NotImplementedException(); }
		}

		#endregion

		[DefaultImp]
		public abstract object this[int index] { get; set; }

		[DefaultImp]
		public abstract object this[object index] { get; }

		[DefaultImp]
		public abstract object this[string index] { set; }

		[DefaultImp]
		public abstract void MustOverrideMethod();

		[DefaultImp]
		public abstract void MustOverrideMethod2();

		[DefaultImp]
		public abstract object MustOverrideMethod3();

		[DefaultImp]
		public abstract object MustOverrideMethod4(object value);
	}

	[MetaExtension]
	public abstract class AbsConstructClass
	{
		public AbsConstructClass()
		{
		}

		public AbsConstructClass(string value)
		{
			_string = value;
		}

		protected AbsConstructClass(int value)
		{
			_int = value;
		}

		protected AbsConstructClass(string value, int value2)
		{
			_string = value;
			_int = value2;
		}

		protected AbsConstructClass(out int value, out string value2)
		{
			value = 10;
			value2 = "Joe Smith";
		}

		protected AbsConstructClass(params object[] value)
		{
			Assert.AreEqual(2, value.Length);
			value[0] = 10;
			value[1] = "Joe Smith";
		}

		#region Int

		private int _int;

		public int Int
		{
			get { return _int; }
			set { _int = value; }
		}

		#endregion

		#region String

		private string _string;

		public string String
		{
			get { return _string; }
			set { _string = value; }
		}

		#endregion
	}

	[MetaExtension]
	public abstract class AbsPropClass
	{
		#region IntProp

		[DefaultImp]
		public abstract int IntProp { get; set; }

		#endregion

		#region GetIntProp

		[DefaultImp]
		public abstract int GetIntProp { get; }

		#endregion

		#region SetIntProp

		[DefaultImp]
		public abstract int SetIntProp { set; }

		#endregion
	}

	[MetaExtension]
	public abstract class AbsMethodClass
	{
		public void RaiseProtectedMethod()
		{
			ProtectedMethod();
		}

		[DefaultImp]
		protected abstract void ProtectedMethod();

		[DefaultImp]
		public abstract void Method();

		[DefaultImp]
		public abstract int IntMethod();

		[DefaultImp]
		public abstract string StringMethod();

		[DefaultImp]
		public abstract void Method(ref int value);

		[DefaultImp]
		public abstract void Method(ref string value);

		[DefaultImp]
		public abstract void Method(ref int value, ref string value2);

		[DefaultImp]
		public abstract void RefOutMethod(ref int value, out string value2);

		[DefaultImp]
		public abstract void OutOutMethod(out int value, out string value2);

		[DefaultImp]
		public abstract string StringOutOutMethod(out int value, out string value2);

		[NotImpException]
		public abstract void NotImpMethod(out int value, out string value2);
	}

	/// <summary>
	/// Summary description for MetaExtensionTest
	/// </summary>
	[TestClass]
	public class MetaExtensionTest
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
		[ExpectedException(typeof(NotImplementedException))]
		public void NotImpIntProp()
		{
			Create<AbsClass>().IntProp = 10;
		}

		[TestMethod]
		public void ImpIntProp()
		{
			AbsClass obj = Create<AbsClass>();
			obj.IntProp2 = 10;
			Assert.AreEqual(10, obj.IntProp2);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void NotImpStringProp()
		{
			Create<AbsClass>().StringProp = "John Smith";
		}

		[TestMethod]
		public void ImpStringProp()
		{
			AbsClass obj = Create<AbsClass>();
			obj.StringProp2 = "John Smith";
			Assert.AreEqual("John Smith", obj.StringProp2);
		}

		[TestMethod]
		public void CustomAccessorPropGet()
		{
			AbsClass obj = Create<AbsClass>();
			Assert.IsNull(obj.CustomAccessorProp);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void CustomAccessorPropSet()
		{
			AbsClass obj = Create<AbsClass>();
			obj.CustomAccessorProp = null;
		}

		[TestMethod]
		public void CustomAccessorProp2Get()
		{
			AbsClass obj = Create<AbsClass>();
			Assert.IsNull(obj.CustomAccessorProp2);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void CustomAccessorProp2Set()
		{
			AbsClass obj = Create<AbsClass>();
			obj.CustomAccessorProp2 = null;
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void CustomAccessorProp3Get()
		{
			AbsClass obj = Create<AbsClass>();
			string s = obj.CustomAccessorProp3;
		}

		[TestMethod]
		public void CustomAccessorProp3Set()
		{
			AbsClass obj = Create<AbsClass>();
			obj.CustomAccessorProp3 = null;
		}

		[TestMethod]
		public void CustomAccessorProp4()
		{
			AbsClass obj = Create<AbsClass>();
			Assert.AreEqual("Mark Twain", obj.CustomAccessorProp4);
			obj.CustomAccessorProp4 = null;
		}

		[TestMethod]
		public void DefContructor()
		{
			Create<AbsConstructClass>();
		}

		[TestMethod]
		public void OneStringValueContructor()
		{
			Assert.AreEqual("Mark Twain", Create<string, AbsConstructClass>("Mark Twain").String);
		}

		[TestMethod]
		public void OneIntValueContructor()
		{
			Assert.AreEqual(10, Create<int, AbsConstructClass>(10).Int);
		}

		[TestMethod]
		public void TwoValuesContructor()
		{
			AbsConstructClass obj = Create<object[], AbsConstructClass>(new object[] { "Mark Twain", 10 });
			Assert.AreEqual("Mark Twain", obj.String);
			Assert.AreEqual(10, obj.Int);
		}

		[TestMethod]
		public void TwoOutValuesContructor()
		{
			object[] args = new object[2] { 0, "" };
			AbsConstructClass obj = Create<object[], AbsConstructClass>(args);
			Assert.AreEqual(10, args[0]);
			Assert.AreEqual("Joe Smith", args[1]);
		}

		[TestMethod]
		public void ParamValueContructor()
		{
			Type entityType = MetaExtension.Create(typeof(AbsConstructClass));
			object[] args = new object[2];
			AbsConstructClass obj = entityType.GetMember<ConstructorInfo>(typeof(object[])).CreateInstance<AbsConstructClass>(args);
			Assert.AreEqual(10, args[0]);
			Assert.AreEqual("Joe Smith", args[1]);
		}		

		[TestMethod]
		public void PropAccessors()
		{
			AbsPropClass obj = Create<AbsPropClass>();
			obj.IntProp = 10;
			Assert.AreEqual(10, obj.IntProp);
			Assert.AreEqual(0, obj.GetIntProp);
			obj.SetIntProp = int.MaxValue;
		}

		[TestMethod]
		public void ProtectedMethod()
		{
			Create<AbsMethodClass>().RaiseProtectedMethod();
		}

		[TestMethod]
		public void AbsMethod()
		{
			Create<AbsMethodClass>().Method();
		}

		[TestMethod]
		public void IntMethod()
		{
			Create<AbsMethodClass>().IntMethod();
		}

		[TestMethod]
		public void StringMethod()
		{
			Create<AbsMethodClass>().StringMethod();
		}

		[TestMethod]
		public void RefIntMethod()
		{
			int value = 10;
			Create<AbsMethodClass>().Method(ref value);
			Assert.AreEqual(10, value);
		}

		[TestMethod]
		public void RefStringMethod()
		{
			string value = "Mark Twain";
			Create<AbsMethodClass>().Method(ref value);
			Assert.AreEqual("Mark Twain", value);
		}

		[TestMethod]
		public void RefIntRefStringMethod()
		{
			int value = 10;
			string value2 = "Mark Twain";
			Create<AbsMethodClass>().Method(ref value, ref value2);
			Assert.AreEqual(10, value);
			Assert.AreEqual("Mark Twain", value2);
		}

		[TestMethod]
		public void RefOutMethod()
		{
			int value = 10;
			string value2;
			Create<AbsMethodClass>().RefOutMethod(ref value, out value2);
			Assert.AreEqual(10, value);
			Assert.IsNull(value2);
		}

		[TestMethod]
		public void OutOutMethod()
		{
			int value;
			string value2;
			Create<AbsMethodClass>().OutOutMethod(out value, out value2);
			Assert.AreEqual(0, value);
			Assert.IsNull(value2);
		}

		[TestMethod]
		public void StringOutOutMethod()
		{
			int value;
			string value2;
			Assert.IsNull(Create<AbsMethodClass>().StringOutOutMethod(out value, out value2));
			Assert.AreEqual(0, value);
			Assert.IsNull(value2);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void NotImpMethod()
		{
			int value;
			string value2;
			Create<AbsMethodClass>().NotImpMethod(out value, out value2);
		}

		private static T Create<T>()
		{
			return Create<object[], T>(null);
		}

		private static T Create<A, T>(A args)
		{
			return MetaExtension.Create(typeof(T)).CreateInstance<T>(args);
		}
	}
}
