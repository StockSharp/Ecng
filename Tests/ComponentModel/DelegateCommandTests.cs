namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class DelegateCommandTests : BaseTestClass
{
	private static readonly object _registryLock = new();

	[TestMethod]
	public void Execute_InvokesAction()
	{
		var executed = false;
		var cmd = new DelegateCommand(() => executed = true);

		cmd.Execute(null);

		executed.AssertTrue();
	}

	[TestMethod]
	public void Execute_WithParameter_InvokesAction()
	{
		object receivedParam = null;
		var cmd = new DelegateCommand<string>(p => receivedParam = p);

		cmd.Execute("test");

		receivedParam.AssertEqual("test");
	}

	[TestMethod]
	public void CanExecute_NoDelegate_ReturnsTrue()
	{
		var cmd = new DelegateCommand(() => { });

		cmd.CanExecute(null).AssertTrue();
	}

	[TestMethod]
	public void CanExecute_WithDelegate_ReturnsResult()
	{
		var canExec = true;
		var cmd = new DelegateCommand<int>(_ => { }, _ => canExec);

		cmd.CanExecute(0).AssertTrue();

		canExec = false;
		cmd.CanExecute(0).AssertFalse();
	}

	[TestMethod]
	public void CanExecute_WithParameter_PassesParameter()
	{
		var cmd = new DelegateCommand<int>(_ => { }, p => p > 0);

		cmd.CanExecute(1).AssertTrue();
		cmd.CanExecute(0).AssertFalse();
		cmd.CanExecute(-1).AssertFalse();
	}

	[TestMethod]
	public void RaiseCanExecuteChanged_InvokesEvent()
	{
		var cmd = new DelegateCommand(() => { });
		var eventRaised = false;

		cmd.CanExecuteChanged += (s, e) => eventRaised = true;
		cmd.RaiseCanExecuteChanged();

		eventRaised.AssertTrue();
	}

	[TestMethod]
	public void RaiseCanExecuteChanged_PassesSenderCorrectly()
	{
		var cmd = new DelegateCommand(() => { });
		object sender = null;

		cmd.CanExecuteChanged += (s, e) => sender = s;
		cmd.RaiseCanExecuteChanged();

		sender.AssertSame(cmd);
	}

	[TestMethod]
	public void Constructor_NullExecute_ThrowsArgumentNullException()
	{
		ThrowsExactly<ArgumentNullException>(() => new DelegateCommand((Action)null));
		ThrowsExactly<ArgumentNullException>(() => new DelegateCommand<string>(null));
	}

	[TestMethod]
	public void GenericCommand_ExecuteWithCorrectType()
	{
		var result = 0;
		var cmd = new DelegateCommand<int>(v => result = v * 2);

		cmd.Execute(5);

		result.AssertEqual(10);
	}

	[TestMethod]
	public void NonGenericCommand_WithObjectParameter()
	{
		object received = null;
		var cmd = new DelegateCommand(p => received = p, _ => true);

		cmd.Execute("hello");

		received.AssertEqual("hello");
	}

	[TestMethod]
	public void Command_WithCanExecute_RegistersInRegistry()
	{
		lock (_registryLock)
		{
			var registry = new TestCommandRegistry();
			var oldRegistry = DelegateCommandSettings.Registry;

			try
			{
				DelegateCommandSettings.Registry = registry;

				var cmd = new DelegateCommand<int>(_ => { }, _ => true);

				registry.RegisteredCommands.Count.AssertEqual(1);
				registry.RegisteredCommands[0].AssertSame(cmd);
			}
			finally
			{
				DelegateCommandSettings.Registry = oldRegistry;
			}
		}
	}

	[TestMethod]
	public void Command_WithoutCanExecute_DoesNotRegister()
	{
		lock (_registryLock)
		{
			var registry = new TestCommandRegistry();
			var oldRegistry = DelegateCommandSettings.Registry;

			try
			{
				DelegateCommandSettings.Registry = registry;

				var cmd = new DelegateCommand<int>(_ => { });

				registry.RegisteredCommands.Count.AssertEqual(0);
			}
			finally
			{
				DelegateCommandSettings.Registry = oldRegistry;
			}
		}
	}

	[TestMethod]
	public void Command_Dispose_UnregistersFromRegistry()
	{
		lock (_registryLock)
		{
			var registry = new TestCommandRegistry();
			var oldRegistry = DelegateCommandSettings.Registry;

			try
			{
				DelegateCommandSettings.Registry = registry;

				var cmd = new DelegateCommand<int>(_ => { }, _ => true);
				registry.RegisteredCommands.Count.AssertEqual(1);

				cmd.Dispose();

				registry.UnregisteredCommands.Count.AssertEqual(1);
				registry.UnregisteredCommands[0].AssertSame(cmd);
			}
			finally
			{
				DelegateCommandSettings.Registry = oldRegistry;
			}
		}
	}

	[TestMethod]
	public void Registry_RevalidateAll_RaisesCanExecuteChanged()
	{
		lock (_registryLock)
		{
			var registry = new TestCommandRegistry();
			var oldRegistry = DelegateCommandSettings.Registry;

			try
			{
				DelegateCommandSettings.Registry = registry;

				var cmd1 = new DelegateCommand<int>(_ => { }, _ => true);
				var cmd2 = new DelegateCommand<int>(_ => { }, _ => true);

				var raised1 = false;
				var raised2 = false;

				cmd1.CanExecuteChanged += (s, e) => raised1 = true;
				cmd2.CanExecuteChanged += (s, e) => raised2 = true;

				registry.RevalidateAll();

				raised1.AssertTrue();
				raised2.AssertTrue();
			}
			finally
			{
				DelegateCommandSettings.Registry = oldRegistry;
			}
		}
	}

	private class TestCommandRegistry : ICommandRegistry
	{
		public List<IRevalidatableCommand> RegisteredCommands { get; } = [];
		public List<IRevalidatableCommand> UnregisteredCommands { get; } = [];

		public void Register(IRevalidatableCommand command) => RegisteredCommands.Add(command);

		public void Unregister(IRevalidatableCommand command) => UnregisteredCommands.Add(command);

		public void RevalidateAll()
		{
			foreach (var cmd in RegisteredCommands)
				cmd.RaiseCanExecuteChanged();
		}
	}
}
