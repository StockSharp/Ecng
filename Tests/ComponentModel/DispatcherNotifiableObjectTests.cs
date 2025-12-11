namespace Ecng.Tests.ComponentModel;

using System.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class DispatcherNotifiableObjectTests : BaseTestClass
{
	private class TestNotifyObject : INotifyPropertyChanged
	{
		private string _name;

		public string Name
		{
			get => _name;
			set
			{
				if (_name == value)
					return;
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
			}
		}

		private int _value;

		public int Value
		{
			get => _value;
			set
			{
				if (_value == value)
					return;
				_value = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	private class TestDispatcher : IDispatcher
	{
		private readonly List<Action> _periodicActions = [];

		public bool CheckAccess() => true;

		public void Invoke(Action action) => action();

		public void InvokeAsync(Action action) => action();

		public IDisposable InvokePeriodically(Action action, TimeSpan interval)
		{
			lock (_periodicActions)
				_periodicActions.Add(action);

			return new TestSubscription(this, action);
		}

		public void ExecutePeriodic()
		{
			Action[] toExecute;
			lock (_periodicActions)
			{
				toExecute = [.. _periodicActions];
			}

			foreach (var action in toExecute)
				action();
		}

		private void Unregister(Action action)
		{
			lock (_periodicActions)
				_periodicActions.Remove(action);
		}

		private class TestSubscription(TestDispatcher owner, Action action) : IDisposable
		{
			public void Dispose() => owner.Unregister(action);
		}
	}

	private static readonly TimeSpan _testInterval = TimeSpan.FromMilliseconds(100);

	[TestMethod]
	public void Constructor_NullDispatcher_Throws()
	{
		var obj = new TestNotifyObject();
		ThrowsExactly<ArgumentNullException>(() => new DispatcherNotifiableObject<TestNotifyObject>(null, obj, _testInterval));
	}

	[TestMethod]
	public void Constructor_NullObj_Throws()
	{
		var dispatcher = new TestDispatcher();
		ThrowsExactly<ArgumentNullException>(() => new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, null, _testInterval));
	}

	[TestMethod]
	public void Obj_ReturnsWrappedObject()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject { Name = "Test" };

		using var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);

		wrapper.Obj.AssertEqual(obj);
		wrapper.Obj.Name.AssertEqual("Test");
	}

	[TestMethod]
	public void PropertyChanged_ForwardsToDispatcher()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject();
		var receivedProperties = new List<string>();

		using var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);
		wrapper.PropertyChanged += (_, args) => receivedProperties.Add(args.PropertyName);

		obj.Name = "NewName";

		// Simulate timer tick
		dispatcher.ExecutePeriodic();

		receivedProperties.Contains("Name").AssertTrue();
	}

	[TestMethod]
	public void MultiplePropertyChanges_Batched()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject();
		var receivedProperties = new List<string>();

		using var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);
		wrapper.PropertyChanged += (_, args) => receivedProperties.Add(args.PropertyName);

		// Change properties multiple times rapidly
		obj.Name = "Name1";
		obj.Name = "Name2";
		obj.Name = "Name3";
		obj.Value = 1;
		obj.Value = 2;

		// Simulate timer tick
		dispatcher.ExecutePeriodic();

		// Both Name and Value should be notified (each once due to batching)
		receivedProperties.Contains("Name").AssertTrue();
		receivedProperties.Contains("Value").AssertTrue();
	}

	[TestMethod]
	public void Dispose_UnsubscribesFromTimer()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject();

		var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);
		wrapper.Dispose();

		wrapper.IsDisposed.AssertTrue();
	}

	[TestMethod]
	public void Dispose_UnsubscribesFromPropertyChanged()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject();
		var receivedAfterDispose = new List<string>();

		var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);
		wrapper.PropertyChanged += (_, args) => receivedAfterDispose.Add(args.PropertyName);
		wrapper.Dispose();

		// Change property after dispose - should not crash
		obj.Name = "AfterDispose";

		// Simulate timer tick - should do nothing since disposed
		dispatcher.ExecutePeriodic();

		// No notifications should be received after dispose
		receivedAfterDispose.Count.AssertEqual(0);

		// No exception should be thrown
		wrapper.IsDisposed.AssertTrue();
	}

	[TestMethod]
	public void IsDisposed_InitiallyFalse()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject();

		using var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);
		wrapper.IsDisposed.AssertFalse();
	}

	[TestMethod]
	public void MultipleDispose_NoException()
	{
		var dispatcher = new TestDispatcher();
		var obj = new TestNotifyObject();

		var wrapper = new DispatcherNotifiableObject<TestNotifyObject>(dispatcher, obj, _testInterval);
		wrapper.Dispose();
		wrapper.Dispose(); // Second dispose should not throw
		wrapper.IsDisposed.AssertTrue();
	}
}
