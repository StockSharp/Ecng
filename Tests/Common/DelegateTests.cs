namespace Ecng.Tests.Common;

using System.ComponentModel;
using System.Reflection;

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
		ThrowsExactly<ArgumentNullException>(() => action.Do(errorHandler));
	}

	[TestMethod]
	public void Do_ThrowsArgumentNullException_WhenErrorIsNull()
	{
		// Arrange
		Action action = () => { };
		Action<Exception> errorHandler = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => action.Do(errorHandler));
	}

	#endregion

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
		var results = new List<int>();
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
		var results = new List<int>();
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
		ThrowsExactly<NullReferenceException>(() => action.GetInvocationList());
	}

	[TestMethod]
	public void GetInvocationList_InvocationOrderIsPreserved()
	{
		// Arrange
		var results = new List<int>();
		Action<int> first = _ => results.Add(1);
		Action<int> second = _ => results.Add(2);
		var combined = first.AddDelegate(second);

		// Act
		var list = combined.GetInvocationList();
		foreach (var d in list)
			d.DynamicInvoke(0);

		// Assert - order must be preserved
		results.Count.AssertEqual(2);
		results[0].AssertEqual(1);
		results[1].AssertEqual(2);
	}

	[TestMethod]
	public void GetInvocationList_SameDelegateAddedTwice_ReturnsTwoEntries()
	{
		// Arrange
		var count = 0;
		Action increment = () => count++;
		var combined = increment.AddDelegate(increment);

		// Act
		var list = combined.GetInvocationList();
		var items = 0;
		foreach (var d in list)
		{
			items++;
			d.DynamicInvoke();
		}

		// Assert
		items.AssertEqual(2);
		count.AssertEqual(2);
	}

	[TestMethod]
	public void GetInvocationList_DelegatesAreBoundToCorrectTargets()
	{
		// Arrange
		var t1 = new Target();
		var t2 = new Target();

		Action<int> a1 = t1.Add;
		Action<int> a2 = t2.Add;
		var combined = a1.AddDelegate(a2);

		// Act
		var list = combined.GetInvocationList();
		foreach (var d in list)
			d.DynamicInvoke(3);

		// Assert
		t1.Sum.AssertEqual(3);
		t2.Sum.AssertEqual(3);
	}

	private class Target
	{
		public int Sum;
		public void Add(int value) => Sum += value;
	}

	#endregion
}
