namespace Ecng.Test.Reflection
{
	using System;
	using System.ComponentModel;

	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[MetaExtension]
	public interface IAbsInterface
	{
		#region IntProp

		[DefaultImp]
		int IntProp { get; set; }

		#endregion
	}

	[MetaExtension]
	public interface IAbsInterface2 : IAbsInterface, ICloneable
	{
	}

	[MetaExtension]
	public interface IAbsNotifyInterface : INotifyPropertyChanged
	{
		#region IntProp

		[NotImpException]
		int IntProp { get; set; }

		#endregion

		#region StringProp

		[DefaultImp]
		string StringProp { get; set; }

		#endregion;
	}

	/*
	[MetaExtension]
	public abstract class AbsNotifyClass : INotifyPropertyChanged
	{
		#region Prop

		[DefaultImp]
		public abstract string Prop { get; set; }

		#endregion

		#region INotifyPropertyChanged Members

		public virtual event PropertyChangedEventHandler PropertyChanged
		{
			add { }
			remove { }
		}

		#endregion
	}

	[MetaExtension]
	public abstract class AbsNotifyClass2 : INotifyPropertyChanged
	{
		#region Prop

		[Wrapper(typeof(Nullable<>))]
		public abstract string Prop { get; set; }

		#endregion

		#region INotifyPropertyChanged Members

		public virtual event PropertyChangedEventHandler PropertyChanged
		{
			add { }
			remove { }
		}

		#endregion
	}

	[MetaExtension]
	public abstract class AbsNotifyClass2 : AbsNotifyClass, INotifyPropertyChanged
	{
		[DefaultImp]
		public new abstract event PropertyChangedEventHandler PropertyChanged;
	}

	[MetaExtension]
	public abstract class AbsNotifyClass3 : AbsNotifyClass, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged Members

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { throw new Exception("The method or operation is not implemented."); }
			remove { throw new Exception("The method or operation is not implemented."); }
		}

		#endregion
	}

	[MetaExtension]
	public abstract class AbsEventClass : Component
	{
		public int InvokeCount;

		[NotImpException]
		public abstract event EventHandler<EventArgs> AbsEvent;

		[DefaultImp]
		public abstract event EventHandler<EventArgs> AbsEvent2;

		public void RaiseEvent(EventArgs e)
		{
			((EventHandler<EventArgs>)base.Events["AbsEvent"])(this, e);
		}

		public void RaiseEvent2(EventArgs e)
		{
			((EventHandler<EventArgs>)base.Events["AbsEvent2"])(this, e);
		}
	}
	*/

	/// <summary>
	/// Summary description for InterfaceMetaExtensionTest
	/// </summary>
	[TestClass]
	public class InterfaceMetaExtensionTest
	{
		public InterfaceMetaExtensionTest()
		{
			//
			// TODO: Add constructor logic here
			//
		}

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
		public void Interface()
		{
			IAbsInterface obj = Create<IAbsInterface>();
			obj.IntProp = 10;
			Assert.AreEqual(10, obj.IntProp);
		}

		[TestMethod]
		public void InterfaceInheritance()
		{
			IAbsInterface2 obj = Create<IAbsInterface2>();
			obj.IntProp = 10;
			Assert.AreEqual(10, obj.IntProp);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void NotImpInterfaceNotify()
		{
			IAbsNotifyInterface obj = Create<IAbsNotifyInterface>();
			obj.PropertyChanged += delegate { };
			obj.IntProp = 10;
		}

		[TestMethod]
		public void InterfaceNotify()
		{
			IAbsNotifyInterface obj = Create<IAbsNotifyInterface>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.Fail();
				//Assert.AreEqual("StringProp", e.PropertyName);
				//Assert.AreEqual("Mark Twain", ((IAbsNotifyInterface)sender).StringProp);
				//obj = null;
			};
			obj.StringProp = "Mark Twain";
			//Assert.IsNull(obj);
		}

		/*
		[TestMethod]
		public void AbsClassNotify()
		{
			AbsNotifyClass obj = Create<AbsNotifyClass>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop", e.PropertyName);
				Assert.AreEqual("Mark Twain", ((AbsNotifyClass)sender).Prop);
				obj = null;
			};
			obj.Prop = "Mark Twain";
			Assert.IsNull(obj);
		}

		[TestMethod]
		public void AbsClassNotify2()
		{
			AbsNotifyClass2 obj = Create<AbsNotifyClass2>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop", e.PropertyName);
				Assert.AreEqual("Mark Twain", ((AbsNotifyClass2)sender).Prop);
				obj = null;
			};
			obj.Prop = "Mark Twain";
			Assert.IsNull(obj);
		}

		[TestMethod]
		public void AbsClassNotify3()
		{
			AbsNotifyClass3 obj = Create<AbsNotifyClass3>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop", e.PropertyName);
				Assert.AreEqual("Mark Twain", ((AbsNotifyClass3)sender).Prop);
				obj = null;
			};
			obj.Prop = "Mark Twain";
			Assert.IsNull(obj);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void NotImpEvent()
		{
			AbsEventClass obj = Create<AbsEventClass>();
			obj.AbsEvent += delegate { obj.InvokeCount += 1; };
		}

		[TestMethod]
		public void ImpEvent()
		{
			AbsEventClass obj = Create<AbsEventClass>();
			obj.AbsEvent2 += delegate { obj.InvokeCount += 1; };
			obj.RaiseEvent2(EventArgs.Empty);
			Assert.AreEqual(1, obj.InvokeCount);
		}
		*/

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
