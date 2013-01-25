namespace Ecng.Test.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	class TestInterceptor : Interceptor
	{
		public static bool Stopped = false;
		public static Queue<Action<InterceptContext, InterceptTypes>> Validators = new Queue<Action<InterceptContext, InterceptTypes>>();

		protected override void BeforeCall(InterceptContext context)
		{
			if (!Stopped)
				Validators.Dequeue()(context, InterceptTypes.Begin);

			base.BeforeCall(context);
		}

		protected override void AfterCall(InterceptContext context)
		{
			if (!Stopped)
				Validators.Dequeue()(context, InterceptTypes.End);

			base.AfterCall(context);
		}

		protected override void Catch(InterceptContext context)
		{
			if (!Stopped)
				Validators.Dequeue()(context, InterceptTypes.Catch);

			base.Catch(context);
		}

		protected override void Finally(InterceptContext context)
		{
			if (!Stopped)
				Validators.Dequeue()(context, InterceptTypes.Finally);

			base.Finally(context);
		}
	}

	class InterceptorException : Exception
	{
	}

	[MetaExtension]
	public class InterceptorEntity
	{
		[Interceptor(typeof(TestInterceptor))]
		protected InterceptorEntity()
		{
		}

		[Interceptor(typeof(TestInterceptor))]
		protected InterceptorEntity(string s)
		{
			Assert.AreEqual("John Smith", s);
		}

		[Interceptor(typeof(TestInterceptor))]
		protected InterceptorEntity(string s, out int i)
		{
			Assert.AreEqual("John Smith", s);
			i = 100;
		}

		[Interceptor(typeof(TestInterceptor))]
		protected InterceptorEntity(string s, out int i, ref int i2)
		{
			Assert.AreEqual("John Smith", s);
			Assert.AreEqual(10, i2);
			throw new InterceptorException();
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual void Run()
		{
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual void Run(int i)
		{
			Assert.AreEqual(100, i);
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual void Run(int i, string s)
		{
			Assert.AreEqual(100, i);
			Assert.AreEqual("John Smith", s);
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual void Run(int i, string s, ref int i2)
		{
			Assert.AreEqual(100, i);
			Assert.AreEqual("John Smith", s);
			Assert.AreEqual(10, i2);
			i2 = 1000;
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual void Run(int i, string s, ref int i2, out string s2)
		{
			Assert.AreEqual(100, i);
			Assert.AreEqual("John Smith", s);
			Assert.AreEqual(10, i2);
			i2 = 100;
			s2 = "Mark Twain";
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual void RunWithException(int i, string s, ref int i2, out string s2)
		{
			Assert.AreEqual(100, i);
			Assert.AreEqual("John Smith", s);
			Assert.AreEqual(10, i2);
			throw new InterceptorException();
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual int RunReturnValue()
		{
			return 10;
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual int RunReturnValue(string s)
		{
			Assert.AreEqual("John Smith", s);
			return 10;
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual int RunReturnValue(string s, out string s2)
		{
			Assert.AreEqual("John Smith", s);
			s2 = "Mark Twain";
			return 10;
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual int RunReturnValueWithException(string s, out string s2)
		{
			Assert.AreEqual("John Smith", s);
			throw new InterceptorException();
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual string RunReturnRef()
		{
			return "Mark Twain";
		}

		[Interceptor(typeof(TestInterceptor))]
		public virtual string RunReturnRef(string s)
		{
			Assert.AreEqual("John Smith", s);
			return "Mark Twain";
		}

		#region MyProperty

		private int _myVar = 100;

		[Interceptor(typeof(TestInterceptor))]
		public virtual int MyProperty
		{
			get { return _myVar; }
			set
			{
				Assert.AreEqual(10, value);
				_myVar = value;
			}
		}

		#endregion

		#region NonLogProp

		private int _nonLogProp = 100;

		[Interceptor(typeof(TestInterceptor), Type = InterceptTypes.None)]
		public virtual int NonLogProp
		{
			get { return _nonLogProp; }
			set
			{
				Assert.AreEqual(10, value);
				_nonLogProp = value;
			}
		}

		#endregion

		[Interceptor(typeof(TestInterceptor))]
		public virtual event EventHandler Event1;

		[Interceptor(typeof(TestInterceptor), Type = InterceptTypes.None)]
		public virtual event EventHandler NonLogEvent1;

		[Interceptor(typeof(TestInterceptor), Type = InterceptTypes.None)]
		public virtual void NonLogRun()
		{
		}

		[Interceptor(typeof(TestInterceptor), Type = InterceptTypes.End | InterceptTypes.Catch)]
		public virtual void NonBeginRun()
		{
		}
	}

	/// <summary>
	/// Summary description for LogInterceptorTest
	/// </summary>
	[TestClass]
	public class InterceptorTest
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

		[TestCleanup]
		public void MyTestCleanup()
		{
			TestInterceptor.Validators.Clear();
		}

		#region Ctor Tests

		[TestMethod]
		public void Ctor()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), ".ctor", null, null, null, false);
		}

		[TestMethod]
		public void CtorArgs()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), ".ctor", CreateArgs("s", "John Smith"), null, null, false);
		}

		[TestMethod]
		public void CtorArgsRef()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), ".ctor", CreateArgs("s", "John Smith"), CreateArgs("i", 100), null, false);
		}

		[TestMethod]
		public void CtorArgsException()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.End), ".ctor", CreateArgs("s", "John Smith", "i2", 10), CreateArgs("i", 100), null, true);
		}

		#endregion

		#region Void Method Tests

		[TestMethod]
		public void VoidMethod()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "Run", null, null, null, false);
		}

		[TestMethod]
		public void VoidMethodArgs()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "Run", CreateArgs("i", 100), null, null, false);
		}

		[TestMethod]
		public void VoidMethodArgs2()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "Run", CreateArgs("i", 100, "s", "John Smith"), null, null, false);
		}

		[TestMethod]
		public void VoidMethodArgsRef()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "Run", CreateArgs("i", 100, "s", "John Smith", "i2", 10), CreateArgs("i2", 1000), null, false);
		}

		[TestMethod]
		public void VoidMethodArgsRefOut()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "Run", CreateArgs("i", 100, "s", "John Smith", "i2", 10), CreateArgs("i2", 100, "s2", "Mark Twain"), null, false);
		}

		[TestMethod]
		public void VoidMethodException()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.End), "RunWithException", CreateArgs("i", 100, "s", "John Smith", "i2", 10), CreateArgs("i2", 100, "s2", "Mark Twain"), null, true);
		}

		#endregion

		#region Return Method Tests

		[TestMethod]
		public void ReturnMethod()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "RunReturnValue", null, null, 10, false);
		}

		[TestMethod]
		public void ReturnMethodArgs()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "RunReturnValue", CreateArgs("s", "John Smith"), null, 10, false);
		}

		[TestMethod]
		public void ReturnMethodArgsOut()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "RunReturnValue", CreateArgs("s", "John Smith"), CreateArgs("s2", "Mark Twain"), 10, false);
		}

		[TestMethod]
		public void ReturnMethodException()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.End), "RunReturnValueWithException", CreateArgs("s", "John Smith"), null, null, true);
		}

		[TestMethod]
		public void ReturnRefMethod()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "RunReturnRef", null, null, "Mark Twain", false);
		}

		[TestMethod]
		public void ReturnRefMethodArgs()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "RunReturnRef", CreateArgs("s", "John Smith"), null, "Mark Twain", false);
		}

		#endregion

		#region Property Tests

		[TestMethod]
		public void GetProperty()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "get_MyProperty", null, null, 100, false);
		}

		[TestMethod]
		public void SetProperty()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "set_MyProperty", CreateArgs("value", 10), null, null, false);
		}

		[TestMethod]
		public void GetNoLogProperty()
		{
			nTest(InterceptTypes.None, "get_NonLogProp", null, null, 100, false);
		}

		[TestMethod]
		public void SetNoLogProperty()
		{
			nTest(InterceptTypes.None, "set_NonLogProp", CreateArgs("value", 10), null, null, false);
		}

		#endregion

		#region Event Tests

		[TestMethod]
		public void AdviseEvent()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "add_Event1", CreateArgs("value", (EventHandler)delegate { }), null, null, false);
		}

		[TestMethod]
		public void UnadviseEvent()
		{
			nTest(InterceptTypes.All.Remove(InterceptTypes.Catch), "remove_Event1", CreateArgs("value", (EventHandler)delegate { }), null, null, false);
		}

		[TestMethod]
		public void AdviseNoLogEvent()
		{
			nTest(InterceptTypes.None, "add_NonLogEvent1", CreateArgs("value", (EventHandler)delegate { }), null, null, false);
		}

		[TestMethod]
		public void UnadviseNoLogEvent()
		{
			nTest(InterceptTypes.None, "remove_NonLogEvent1", CreateArgs("value", (EventHandler)delegate { }), null, null, false);
		}

		#endregion

		#region No Log Tests

		[TestMethod]
		public void NoLogMethod()
		{
			nTest(InterceptTypes.None, "NonLogRun", null, null, null, false);
		}

		[TestMethod]
		public void NoBeginMethod()
		{
			nTest(InterceptTypes.End, "NonBeginRun", null, null, null, false);
		}

		#endregion

		private static void nTest(InterceptTypes types, string methodName, Dictionary<string, object> inArgs, Dictionary<string, object> refArgs, object returnValue, bool hasException)
		{
			if (inArgs == null)
				inArgs = new Dictionary<string, object>();

			if (refArgs == null)
				refArgs = new Dictionary<string, object>();

			Type entityType = MetaExtension.Create(typeof(InterceptorEntity));

			var method = entityType.GetMember<MethodBase>(methodName, ReflectionHelper.GetArgTypes(ConcatArgs(inArgs, refArgs).Values.ToArray()));

			if (hasException)
				refArgs.Clear();

			if (types.Contains(InterceptTypes.Begin))
				TestInterceptor.Validators.Enqueue(CreateBeginValidator(methodName, inArgs));

			if (types.Contains(InterceptTypes.End))
				TestInterceptor.Validators.Enqueue(CreateEndValidator(methodName, inArgs, refArgs, returnValue, hasException));

			if (types.Contains(InterceptTypes.Catch))
				TestInterceptor.Validators.Enqueue(CreateCatchValidator(methodName, inArgs));

			if (types.Contains(InterceptTypes.Finally))
				TestInterceptor.Validators.Enqueue(CreateFinallyValidator(methodName, inArgs, refArgs, returnValue, hasException));

			var inArgsCopy = new Dictionary<string, object>();
			inArgs.CopyTo(inArgsCopy);

			foreach (var param in method.GetParameters())
			{
				if (!inArgsCopy.ContainsKey(param.Name))
					inArgsCopy.Add(param.Name, null);
			}

			try
			{
				if (methodName == ".ctor")
					((ConstructorInfo)method).Invoke(GetArgs(method, inArgsCopy));
				else
				{
					TestInterceptor.Stopped = true;
					var entity = entityType.CreateInstance<object>();
					TestInterceptor.Stopped = false;

					method.Invoke(entity, GetArgs(method, inArgsCopy));
				}
			}
			catch (TargetInvocationException ex)
			{
				if (!(ex.InnerException is InterceptorException && hasException))
					throw;
			}

			Assert.AreEqual(0, TestInterceptor.Validators.Count);
		}

		private static Action<InterceptContext, InterceptTypes> CreateBeginValidator(string methodName, IDictionary<string, object> inRefArgs)
		{
			return (ctx, type) =>
			{
				ValidateHeader(ctx, InterceptTypes.Begin, type, methodName);

				Assert.IsNull(ctx.Exception);
				Assert.IsNull(ctx.ReturnValue);

				Assert.IsTrue(inRefArgs.SequenceEqual(ctx.InRefArgs));

				Assert.IsNull(ctx.RefOutArgs);
			};
		}

		private static Action<InterceptContext, InterceptTypes> CreateEndValidator(string methodName, Dictionary<string, object> inRefArgs, Dictionary<string, object> refOutArgs, object returnValue, bool hasException)
		{
			return (ctx, type) =>
			{
				ValidateHeader(ctx, InterceptTypes.End, type, methodName);

				//if (hasException)
				//	Assert.IsInstanceOfType(ctx.Exception, typeof(InterceptorException));
				//else
				Assert.IsNull(ctx.Exception);

				if (methodName == ".ctor")
					Assert.IsInstanceOfType(ctx.ReturnValue, typeof(InterceptorEntity));
				else
					Assert.AreEqual(returnValue, ctx.ReturnValue);

				Assert.IsTrue(ConcatArgs(inRefArgs, refOutArgs).SequenceEqual(ConcatArgs(ctx.InRefArgs, ctx.RefOutArgs)));
			};
		}

		private static Action<InterceptContext, InterceptTypes> CreateCatchValidator(string methodName, Dictionary<string, object> inRefArgs)
		{
			return (ctx, type) =>
			{
				ValidateHeader(ctx, InterceptTypes.Catch, type, methodName);

				Assert.IsInstanceOfType(ctx.Exception, typeof(InterceptorException));
				Assert.IsNull(ctx.ReturnValue);
				Assert.IsTrue(inRefArgs.SequenceEqual(ctx.InRefArgs));
				Assert.IsNull(ctx.RefOutArgs);
			};
		}

		private static Action<InterceptContext, InterceptTypes> CreateFinallyValidator(string methodName, Dictionary<string, object> inRefArgs, Dictionary<string, object> refOutArgs, object returnValue, bool hasException)
		{
			return (ctx, type) =>
			{
				ValidateHeader(ctx, InterceptTypes.Finally, type, methodName);

				if (hasException)
				{
					Assert.IsInstanceOfType(ctx.Exception, typeof(InterceptorException));
					Assert.IsNull(ctx.ReturnValue);
					Assert.IsNull(ctx.RefOutArgs);
				}
				else
				{
					Assert.IsNull(ctx.Exception);

					if (methodName == ".ctor")
						Assert.IsInstanceOfType(ctx.ReturnValue, typeof(InterceptorEntity));
					else
						Assert.AreEqual(returnValue, ctx.ReturnValue);
				}

				Assert.IsTrue(ConcatArgs(inRefArgs, refOutArgs).SequenceEqual(ConcatArgs(ctx.InRefArgs, ctx.RefOutArgs)));
			};
		}

		private static Dictionary<string, object> ConcatArgs(IDictionary<string, object> inArgs, IDictionary<string, object> refArgs)
		{
			var retVal = new Dictionary<string, object>();

			if (inArgs != null)
			{
				foreach (var pair in inArgs)
					retVal.Add(pair.Key, pair.Value);
			}

			if (refArgs != null)
			{
				foreach (var pair in refArgs)
					retVal[pair.Key] = pair.Value;
			}

			return retVal;
		}

		private static Dictionary<string, object> CreateArgs(params object[] data)
		{
			Assert.IsTrue(data.Length % 2 == 0);

			var retVal = new Dictionary<string, object>();

			for (int i = 0; i < data.Length; i += 2)
				retVal.Add((string)data[i], data[i + 1]);

			return retVal;
		}

		private static object[] GetArgs(MethodBase method, Dictionary<string, object> inArgs)
		{
			var args = new List<object>();

			foreach (var param in method.GetParameters())
				args.Add(inArgs[param.Name]);

			return args.ToArray();
		}

		private static void ValidateHeader(InterceptContext ctx, InterceptTypes expectedType, InterceptTypes actualType, string methodName)
		{
			Assert.AreEqual(expectedType, actualType);
			Assert.AreEqual(typeof(InterceptorEntity), ctx.ReflectedType.BaseType);
			Assert.AreEqual(methodName, ctx.MethodName);
		}
	}
}
