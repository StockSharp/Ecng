namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class MessengerTests : BaseTestClass
{
	private sealed class TestMessage
	{
		public int Value;
	}

	private sealed class OtherMessage
	{
	}

	private sealed class Recipient : IRecipient<TestMessage>
	{
		public int Received;
		public int Last;

		public void Receive(TestMessage message)
		{
			Received++;
			Last = message.Value;
		}
	}

	private sealed class MultiRecipient : IRecipient<TestMessage>, IRecipient<OtherMessage>
	{
		public int Tests;
		public int Others;

		public void Receive(TestMessage message) => Tests++;
		public void Receive(OtherMessage message) => Others++;
	}

	[TestMethod]
	public void Strong_RegisterRecipient_Send_Receives()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new Recipient();

		m.Register(r);
		m.Send(new TestMessage { Value = 5 });

		r.Received.AssertEqual(1);
		r.Last.AssertEqual(5);
	}

	[TestMethod]
	public void Unregister_StopsDelivery()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new Recipient();

		m.Register(r);
		m.Unregister<TestMessage>(r);
		m.Send(new TestMessage());

		r.Received.AssertEqual(0);
	}

	[TestMethod]
	public void UnregisterAll_StopsAllDelivery()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new MultiRecipient();

		m.RegisterAll(r);
		m.UnregisterAll(r);
		m.Send(new TestMessage());
		m.Send(new OtherMessage());

		r.Tests.AssertEqual(0);
		r.Others.AssertEqual(0);
	}

	[TestMethod]
	public void HandlerOverload_ReceivesValue()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new Recipient();

		m.Register<Recipient, TestMessage>(r, (rec, msg) => rec.Last = msg.Value);
		m.Send(new TestMessage { Value = 9 });

		r.Last.AssertEqual(9);
	}

	[TestMethod]
	public void Token_ScopesDelivery()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new Recipient();

		m.Register<Recipient, TestMessage>(r, "channel", (rec, msg) => rec.Received++);

		m.Send(new TestMessage(), "channel");
		r.Received.AssertEqual(1);

		// Default-token send must not reach the "channel" registration.
		m.Send(new TestMessage());
		r.Received.AssertEqual(1);
	}

	[TestMethod]
	public void RegisterAll_RegistersEveryRecipientInterface()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new MultiRecipient();

		m.RegisterAll(r);
		m.Send(new TestMessage());
		m.Send(new OtherMessage());

		r.Tests.AssertEqual(1);
		r.Others.AssertEqual(1);
	}

	[TestMethod]
	public void IsRegistered_ReflectsState()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new Recipient();

		m.IsRegistered<TestMessage>(r).AssertFalse();
		m.Register(r);
		m.IsRegistered<TestMessage>(r).AssertTrue();
		m.Unregister<TestMessage>(r);
		m.IsRegistered<TestMessage>(r).AssertFalse();
	}

	[TestMethod]
	public void Send_ReturnsMessage()
	{
		IMessenger m = new StrongReferenceMessenger();

		var msg = m.Send(new TestMessage { Value = 7 });

		msg.Value.AssertEqual(7);
	}

	[TestMethod]
	public void DuplicateRegistration_Throws()
	{
		IMessenger m = new StrongReferenceMessenger();
		var r = new Recipient();

		m.Register(r);

		Throws<InvalidOperationException>(() => m.Register(r));
	}

	[TestMethod]
	public void Weak_CollectedRecipient_NotDelivered()
	{
		IMessenger m = new WeakReferenceMessenger();
		var counter = new StrongBox<int>();

		RegisterCollectibleRecipient(m, counter);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		m.Cleanup();
		m.Send(new TestMessage());

		counter.Value.AssertEqual(0);
	}

	[TestMethod]
	public void Weak_LiveRecipient_Delivered()
	{
		IMessenger m = new WeakReferenceMessenger();
		var r = new Recipient();

		m.Register(r);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		m.Send(new TestMessage { Value = 11 });

		r.Last.AssertEqual(11);
	}

	// Registers a recipient that is unreachable after this method returns, so a weak messenger lets it
	// be collected. The handler captures only the counter (never the recipient).
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RegisterCollectibleRecipient(IMessenger messenger, StrongBox<int> counter)
	{
		var recipient = new object();
		messenger.Register<object, TestMessage>(recipient, (r, msg) => counter.Value++);
	}

	[TestMethod]
	public void ObservableRecipient_IsActive_RegistersAndUnregisters()
	{
		IMessenger m = new StrongReferenceMessenger();
		var vm = new ActivatableRecipient(m);

		m.IsRegistered<TestMessage>(vm).AssertFalse();

		vm.IsActive = true;
		m.IsRegistered<TestMessage>(vm).AssertTrue();

		m.Send(new TestMessage());
		vm.Got.AssertEqual(1);

		vm.IsActive = false;
		m.IsRegistered<TestMessage>(vm).AssertFalse();
	}

	[TestMethod]
	public void ObservableRecipient_SetProperty_Broadcasts()
	{
		IMessenger m = new StrongReferenceMessenger();
		var vm = new BroadcastingRecipient(m);

		PropertyChangedMessage<int> got = null;
		var listener = new object();
		m.Register<object, PropertyChangedMessage<int>>(listener, (r, msg) => got = msg);

		vm.Count = 10;

		IsNotNull(got);
		got.OldValue.AssertEqual(0);
		got.NewValue.AssertEqual(10);
		got.PropertyName.AssertEqual(nameof(BroadcastingRecipient.Count));
		ReferenceEquals(got.Sender, vm).AssertTrue();
	}

	private sealed class ActivatableRecipient : ObservableRecipient, IRecipient<TestMessage>
	{
		public int Got;

		public ActivatableRecipient(IMessenger messenger)
			: base(messenger)
		{
		}

		public void Receive(TestMessage message) => Got++;
	}

	private sealed class BroadcastingRecipient : ObservableRecipient
	{
		public BroadcastingRecipient(IMessenger messenger)
			: base(messenger)
		{
		}

		private int _count;

		public int Count
		{
			get => _count;
			set => SetProperty(ref _count, value, broadcast: true);
		}
	}
}
