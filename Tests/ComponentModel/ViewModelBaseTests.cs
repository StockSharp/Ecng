namespace Ecng.Tests.ComponentModel;

using System.ComponentModel;
using System.Windows.Input;

using Ecng.ComponentModel;

[TestClass]
public class ViewModelBaseTests : BaseTestClass
{
	private class TestViewModel : ViewModelBase
	{
		private string _name;
		private int _age;

		public string Name
		{
			get => _name;
			set => SetField(ref _name, value);
		}

		public int Age
		{
			get => _age;
			set => SetField(ref _age, value);
		}

		public ICommand TestCommand { get; private set; }
		public ICommand TestCommand2 { get; private set; }

		public void InitCommands()
		{
			TestCommand = CreateCommand(() => { });
			TestCommand2 = CreateCommand<int>(_ => { });
		}

		public TCommand ExposeRegisterCommand<TCommand>(TCommand command)
			where TCommand : IDisposable
			=> RegisterCommand(command);

		public void TriggerPropertyChanged(string name) => OnPropertyChanged(name);
	}

	[TestMethod]
	public void SetField_ChangesValue_RaisesPropertyChanged()
	{
		var vm = new TestViewModel();
		var raised = false;
		string changedProperty = null;

		vm.PropertyChanged += (s, e) =>
		{
			raised = true;
			changedProperty = e.PropertyName;
		};

		vm.Name = "Test";

		raised.AssertTrue();
		changedProperty.AssertEqual(nameof(TestViewModel.Name));
		vm.Name.AssertEqual("Test");
	}

	[TestMethod]
	public void SetField_SameValue_DoesNotRaisePropertyChanged()
	{
		var vm = new TestViewModel { Name = "Test" };
		var raised = false;

		vm.PropertyChanged += (s, e) => raised = true;

		vm.Name = "Test";

		raised.AssertFalse();
	}

	[TestMethod]
	public void SetField_DifferentProperties_RaisesCorrectEvents()
	{
		var vm = new TestViewModel();
		var properties = new List<string>();

		vm.PropertyChanged += (s, e) => properties.Add(e.PropertyName);

		vm.Name = "John";
		vm.Age = 30;

		properties.Count.AssertEqual(2);
		properties[0].AssertEqual(nameof(TestViewModel.Name));
		properties[1].AssertEqual(nameof(TestViewModel.Age));
	}

	[TestMethod]
	public void OnPropertyChanged_ManualCall_RaisesEvent()
	{
		var vm = new TestViewModel();
		var raised = false;
		string changedProperty = null;

		vm.PropertyChanged += (s, e) =>
		{
			raised = true;
			changedProperty = e.PropertyName;
		};

		vm.TriggerPropertyChanged("CustomProperty");

		raised.AssertTrue();
		changedProperty.AssertEqual("CustomProperty");
	}

	[TestMethod]
	public void CreateCommand_ReturnsCommand()
	{
		var vm = new TestViewModel();
		vm.InitCommands();

		vm.TestCommand.AssertNotNull();
		vm.TestCommand2.AssertNotNull();
	}

	[TestMethod]
	public void CreateCommand_CommandIsExecutable()
	{
		var executed = false;
		var vm = new TestViewModel();

		using (vm)
		{
			var cmd = vm.ExposeRegisterCommand(new DelegateCommand(() => executed = true));
			cmd.Execute(null);
		}

		executed.AssertTrue();
	}

	[TestMethod]
	public void RegisterCommand_DisposesOnViewModelDispose()
	{
		var disposed = false;
		var vm = new TestViewModel();

		var disposable = new DisposableAction(() => disposed = true);
		vm.ExposeRegisterCommand(disposable);

		disposed.AssertFalse();

		vm.Dispose();

		disposed.AssertTrue();
	}

	[TestMethod]
	public void CreateCommand_DisposesOnViewModelDispose()
	{
		var vm = new TestViewModel();
		vm.InitCommands();

		var cmd = (DelegateCommand)vm.TestCommand;

		vm.Dispose();

		// Command should be disposed - check IsDisposed via base class
		cmd.IsDisposed.AssertTrue();
	}

	[TestMethod]
	public void Dispose_MultipleCommands_DisposesAll()
	{
		var count = 0;
		var vm = new TestViewModel();

		vm.ExposeRegisterCommand(new DisposableAction(() => count++));
		vm.ExposeRegisterCommand(new DisposableAction(() => count++));
		vm.ExposeRegisterCommand(new DisposableAction(() => count++));

		vm.Dispose();

		count.AssertEqual(3);
	}

	[TestMethod]
	public void Dispose_NoCommands_DoesNotThrow()
	{
		var vm = new TestViewModel();
		vm.Dispose();
	}

	[TestMethod]
	public void RegisterCommand_ReturnsCommand()
	{
		var vm = new TestViewModel();
		var cmd = new DelegateCommand(() => { });

		var result = vm.ExposeRegisterCommand(cmd);

		result.AssertSame(cmd);
	}

	private class DisposableAction : IDisposable
	{
		private readonly Action _action;

		public DisposableAction(Action action) => _action = action;

		public void Dispose() => _action();
	}
}
