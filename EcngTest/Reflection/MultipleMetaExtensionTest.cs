namespace Ecng.Test.Reflection
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[MetaExtension]
	[MetaExtension(Order = 1)]
	[MetaExtension(Order = 2)]
	public class MultipleMetaExtensionClass : INotifyPropertyChanged
	{
		[DefaultImp(Order = 0)]
		[NotImpException(Order = 1)]
		public virtual int Prop1
		{
			get { throw new Exception(); }
			set { throw new Exception(); }
		}

		[NotImpException(Order = 0)]
		[DefaultImp(Order = 1)]
		public virtual int Prop2
		{
			get { throw new Exception(); }
			set { throw new Exception(); }
		}

		public virtual int Prop3
		{
			[NotImpException(Order = 0)]
			[DefaultImp(Order = 1)]
			get { throw new Exception(); }
			[DefaultImp(Order = 0)]
			[NotImpException(Order = 1)]
			set { throw new Exception(); }
		}

		[DefaultImp(Order = 0)]
		public virtual int Prop4
		{
			get { throw new Exception(); }
			[Interceptor(typeof(NotifyInterceptor), Order = 1)]
			set { throw new Exception(); }
		}

		[Wrapper(typeof(NullableEx<>), Order = 0)]
		[Interceptor(typeof(ValidatorInterceptor), Order = 2)]
		[Range(MinValue = 10)]
		public virtual int Prop5
		{
			get { throw new Exception(); }
			[Interceptor(typeof(NotifyInterceptor), Order = 1)]
			set { throw new Exception(); }
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	[MetaExtension]
	[MetaExtension(Order = 1)]
	[MetaExtension(Order = 2)]
	public interface IMultipleMetaExtension : INotifyPropertyChanged
	{
		[DefaultImp(Order = 0)]
		[NotImpException(Order = 1)]
		int Prop1
		{
			get;
			set;
		}

		[NotImpException(Order = 0)]
		[DefaultImp(Order = 1)]
		int Prop2
		{
			get;
			set;
		}

		int Prop3
		{
			[NotImpException(Order = 0)]
			[DefaultImp(Order = 1)]
			get;
			[DefaultImp(Order = 0)]
			[NotImpException(Order = 1)]
			set;
		}

		[DefaultImp(Order = 0)]
		int Prop4
		{
			get;
			[Interceptor(typeof(NotifyInterceptor), Order = 1)]
			set;
		}

		[Wrapper(typeof(NullableEx<>), Order = 0)]
		[Interceptor(typeof(ValidatorInterceptor), Order = 2)]
		[Range(MinValue = 3)]
		int Prop5
		{
			get;
			[Interceptor(typeof(NotifyInterceptor), Order = 1)]
			set;
		}
	}

	/// <summary>
	/// Summary description for MultipleMetaExtensionTest
	/// </summary>
	[TestClass]
	public class MultipleMetaExtensionTest
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
		public void DefNotImpPropTest()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			obj.Prop1 = 10;
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void InterfaceDefNotImpPropTest()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			obj.Prop1 = 10;
		}

		[TestMethod]
		public void NotImpDefPropTest()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			obj.Prop2 = 10;
			Assert.AreEqual(10, obj.Prop2);
		}

		[TestMethod]
		public void InterfaceNotImpDefPropTest()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			obj.Prop2 = 10;
			Assert.AreEqual(10, obj.Prop2);
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void DefNotImpPropTest2()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			obj.Prop3 = 10;
		}

		[TestMethod]
		[ExpectedException(typeof(NotImplementedException))]
		public void InterfaceDefNotImpPropTest2()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			obj.Prop3 = 10;
		}

		[TestMethod]
		public void NotImpDefPropTest2()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			Assert.AreEqual(0, obj.Prop3);
		}

		[TestMethod]
		public void InterfaceNotImpDefPropTest2()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			Assert.AreEqual(0, obj.Prop3);
		}

		[TestMethod]
		public void NotifyPropTest()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop4", e.PropertyName);
				Assert.AreEqual(100, ((MultipleMetaExtensionClass)sender).Prop4);
				obj = null;
			};
			obj.Prop4 = 100;
			Assert.IsNull(obj);
		}

		[TestMethod]
		public void InterfaceNotifyPropTest()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop4", e.PropertyName);
				Assert.AreEqual(100, ((IMultipleMetaExtension)sender).Prop4);
				obj = null;
			};
			obj.Prop4 = 100;
			Assert.IsNull(obj);
		}

		[TestMethod]
		public void NotifyPropTest2()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop5", e.PropertyName);
				Assert.AreEqual(100, ((MultipleMetaExtensionClass)sender).Prop5);
				obj = null;
			};
			obj.Prop5 = 100;
			Assert.IsNull(obj);
		}

		[TestMethod]
		public void InterfaceNotifyPropTest2()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.AreEqual("Prop5", e.PropertyName);
				Assert.AreEqual(100, ((IMultipleMetaExtension)sender).Prop5);
				obj = null;
			};
			obj.Prop5 = 100;
			Assert.IsNull(obj);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void NotifyPropTest3()
		{
			MultipleMetaExtensionClass obj = nCreate<MultipleMetaExtensionClass>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.Fail();
			};
			obj.Prop5 = 1;
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void InterfaceNotifyPropTest3()
		{
			IMultipleMetaExtension obj = nCreate<IMultipleMetaExtension>();
			obj.PropertyChanged += (sender, e) =>
			{
				Assert.Fail();
			};
			obj.Prop5 = 1;
		}

		private static T nCreate<T>()
		{
			return MetaExtension.Create(typeof(T)).CreateInstance<T>();
		}
	}
}
