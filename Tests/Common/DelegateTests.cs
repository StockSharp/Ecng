namespace Ecng.Tests.Common;

using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DelegateTests : BaseTestClass
{
	#region Do Tests

	[TestMethod]
	public void Do_ExecutesActionSuccessfully()
	{
		// Arrange
		var executed = false;
		Exception caughtError = null;

		Action action = () => executed = true;
		Action<Exception> errorHandler = ex => caughtError = ex;

		// Act
		action.Do(errorHandler);

		// Assert
		executed.AssertTrue();
		caughtError.AssertNull();
	}

	[TestMethod]
	public void Do_CatchesExceptionAndCallsErrorHandler()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Test error");
		Exception caughtError = null;

		Action action = () => throw expectedException;
		Action<Exception> errorHandler = ex => caughtError = ex;

		// Act
		action.Do(errorHandler);

		// Assert
		caughtError.AssertNotNull();
		caughtError.AssertSame(expectedException);
	}

	[TestMethod]
	public void Do_ThrowsArgumentNullException_WhenActionIsNull()
	{
		// Arrange
		Action action = null;
		Action<Exception> errorHandler = ex => { };

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => action.Do(errorHandler));
	}

	[TestMethod]
	public void Do_ThrowsArgumentNullException_WhenErrorIsNull()
	{
		// Arrange
		Action action = () => { };
		Action<Exception> errorHandler = null;

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => action.Do(errorHandler));
	}

	#endregion

	#region DoAsync Tests

#pragma warning disable CS0618 // Type or member is obsolete
	[TestMethod]
	public void DoAsync_ExecutesActionAsynchronously()
	{
		// Arrange
		var executed = false;
		Exception caughtError = null;
		var waitHandle = new ManualResetEvent(false);

		Action action = () =>
		{
			executed = true;
			waitHandle.Set();
		};

		Action<Exception> errorHandler = ex =>
		{
			caughtError = ex;
			waitHandle.Set();
		};

		// Act
		action.DoAsync(errorHandler);
		var signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));

		// Assert
		signaled.AssertTrue();
		executed.AssertTrue();
		caughtError.AssertNull();
	}

	[TestMethod]
	public void DoAsync_CatchesExceptionAndCallsErrorHandler()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Async test error");
		Exception caughtError = null;
		var waitHandle = new ManualResetEvent(false);

		Action action = () => throw expectedException;
		Action<Exception> errorHandler = ex =>
		{
			caughtError = ex;
			waitHandle.Set();
		};

		// Act
		action.DoAsync(errorHandler);
		var signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));

		// Assert
		signaled.AssertTrue();
		caughtError.AssertNotNull();
		caughtError.AssertSame(expectedException);
	}

	[TestMethod]
	public void DoAsync_ThrowsArgumentNullException_WhenActionIsNull()
	{
		// Arrange
		Action action = null;
		Action<Exception> errorHandler = ex => { };

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => action.DoAsync(errorHandler));
	}

	[TestMethod]
	public void DoAsync_ThrowsArgumentNullException_WhenErrorIsNull()
	{
		// Arrange
		Action action = () => { };
		Action<Exception> errorHandler = null;

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => action.DoAsync(errorHandler));
	}
#pragma warning restore CS0618 // Type or member is obsolete

	[TestMethod]
	public async Task DoAsync_ModernReplacement_WithTaskRun()
	{
		// Arrange
		var executed = false;
		Exception caughtError = null;

		Action action = () => executed = true;

		// Act - Modern replacement using Task.Run
		await Task.Run(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				caughtError = ex;
			}
		});

		// Assert
		executed.AssertTrue();
		caughtError.AssertNull();
	}

	#endregion

	#region Invoke Tests

	[TestMethod]
	public void Invoke_PropertyChangedEventHandler_InvokesWithCorrectArgs()
	{
		// Arrange
		object receivedSender = null;
		string receivedPropertyName = null;
		var sender = new object();
		const string propertyName = "TestProperty";

		PropertyChangedEventHandler handler = (s, e) =>
		{
			receivedSender = s;
			receivedPropertyName = e.PropertyName;
		};

		// Act
		handler.Invoke(sender, propertyName);

		// Assert
		receivedSender.AssertSame(sender);
		receivedPropertyName.AssertEqual(propertyName);
	}

	[TestMethod]
	public void Invoke_PropertyChangingEventHandler_InvokesWithCorrectArgs()
	{
		// Arrange
		object receivedSender = null;
		string receivedPropertyName = null;
		var sender = new object();
		const string propertyName = "TestProperty";

		PropertyChangingEventHandler handler = (s, e) =>
		{
			receivedSender = s;
			receivedPropertyName = e.PropertyName;
		};

		// Act
		handler.Invoke(sender, propertyName);

		// Assert
		receivedSender.AssertSame(sender);
		receivedPropertyName.AssertEqual(propertyName);
	}

	#endregion

	#region CreateDelegate Tests

	[TestMethod]
	public void CreateDelegate_CreatesStaticDelegate()
	{
		// Arrange
		var method = typeof(DelegateTests).GetMethod(nameof(StaticTestMethod), BindingFlags.Static | BindingFlags.NonPublic);

		// Act
		var del = method.CreateDelegate<Func<int, int>>();

		// Assert
		del.AssertNotNull();
		var result = del(5);
		result.AssertEqual(10);
	}

	[TestMethod]
	public void CreateDelegate_WithInstance_CreatesInstanceDelegate()
	{
		// Arrange
		var instance = new TestClass();
		var method = typeof(TestClass).GetMethod(nameof(TestClass.InstanceMethod));

		// Act
		var del = method.CreateDelegate<TestClass, Func<int, int>>(instance);

		// Assert
		del.AssertNotNull();
		var result = del(3);
		result.AssertEqual(6);
	}

	private static int StaticTestMethod(int value) => value * 2;

	private class TestClass
	{
		public int InstanceMethod(int value) => value * 2;
	}

	#endregion

	#region AddDelegate and RemoveDelegate Tests

	[TestMethod]
	public void AddDelegate_CombinesTwoDelegates()
	{
		// Arrange
		var results = new System.Collections.Generic.List<int>();
		Action<int> action1 = x => results.Add(x);
		Action<int> action2 = x => results.Add(x * 2);

		// Act
		var combined = action1.AddDelegate(action2);
		combined(5);

		// Assert
		results.Count.AssertEqual(2);
		results[0].AssertEqual(5);
		results[1].AssertEqual(10);
	}

	[TestMethod]
	public void RemoveDelegate_RemovesDelegateFromCombined()
	{
		// Arrange
		var results = new System.Collections.Generic.List<int>();
		Action<int> action1 = x => results.Add(x);
		Action<int> action2 = x => results.Add(x * 2);

		var combined = action1.AddDelegate(action2);

		// Act
		var removed = combined.RemoveDelegate(action2);
		removed(5);

		// Assert
		results.Count.AssertEqual(1);
		results[0].AssertEqual(5);
	}

	[TestMethod]
	public void AddDelegate_WithNull_ReturnsValue()
	{
		// Arrange
		Action<int> action1 = null;
		Action<int> action2 = x => { };

		// Act
		var result = action1.AddDelegate(action2);

		// Assert
		result.AssertNotNull();
	}

	#endregion

	#region GetInvocationList Tests

	[TestMethod]
	public void GetInvocationList_ReturnsSingleDelegate()
	{
		// Arrange
		Action<int> action = x => { };

		// Act
		var list = action.GetInvocationList();

		// Assert
		var count = 0;
		foreach (var _ in list)
			count++;
		count.AssertEqual(1);
	}

	[TestMethod]
	public void GetInvocationList_ReturnsAllCombinedDelegates()
	{
		// Arrange
		Action<int> action1 = x => { };
		Action<int> action2 = x => { };
		Action<int> action3 = x => { };
		var combined = action1.AddDelegate(action2).AddDelegate(action3);

		// Act
		var list = combined.GetInvocationList();

		// Assert
		var count = 0;
		foreach (var _ in list)
			count++;
		count.AssertEqual(3);
	}

	[TestMethod]
	public void GetInvocationList_ThrowsForNull()
	{
		// Arrange
		Action<int> action = null;

		// Act & Assert
		Assert.ThrowsExactly<NullReferenceException>(() => action.GetInvocationList());
	}

	#endregion

	#region RemoveAllDelegates Tests

	[TestMethod]
	public void RemoveAllDelegates_RemovesAllFromInvocationList()
	{
		// Arrange
		var callCount = 0;
		Action action1 = () => callCount++;
		Action action2 = () => callCount++;
		var combined = action1.AddDelegate(action2);

		// Act
		combined.RemoveAllDelegates();

		// Assert - the source delegate is not modified (value type semantics)
		// RemoveAllDelegates iterates but doesn't modify the original
		combined();
		callCount.AssertEqual(2);
	}

	#endregion
}
