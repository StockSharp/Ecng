namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class DelegateCommandTests : BaseTestClass
{
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
}
