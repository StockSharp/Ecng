#if NET10_0_OR_GREATER

namespace Ecng.Tests.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Ecng.ComponentModel;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

[TestClass]
public class MvvmGeneratorTests : BaseTestClass
{
	[TestMethod]
	public void ObservableProperty_GeneratesProperty_RaisesPropertyChanged()
	{
		var vm = new MvvmNotifiableSample();
		var changed = new List<string>();
		((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		vm.Name = "abc";

		vm.Name.AssertEqual("abc");
		changed.AssertContains(nameof(vm.Name));
	}

	[TestMethod]
	public void ObservableProperty_NoChange_DoesNotNotify()
	{
		var vm = new MvvmNotifiableSample { Name = "x" };
		var changed = new List<string>();
		((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		vm.Name = "x";

		changed.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ObservableProperty_OnNotifiable_RaisesPropertyChanging()
	{
		var vm = new MvvmNotifiableSample();
		var changing = new List<string>();
		((INotifyPropertyChanging)vm).PropertyChanging += (_, e) => changing.Add(e.PropertyName);

		vm.Name = "abc";

		changing.AssertContains(nameof(vm.Name));
	}

	[TestMethod]
	public void ObservableProperty_PartialHooks_CalledInOrderWithValues()
	{
		var vm = new MvvmNotifiableSample();

		vm.Name = "joe";

		// Changing (value, then old/new) before the change; Changed (value, then old/new) after.
		var expected = new[] { "changing:joe", "changing2:->joe", "changed:joe", "changed2:->joe" };
		vm.Log.Count.AssertEqual(expected.Length);

		for (var i = 0; i < expected.Length; i++)
			vm.Log[i].AssertEqual(expected[i]);
	}

	[TestMethod]
	public void NotifyPropertyChangedFor_RaisesDependentProperty()
	{
		var vm = new MvvmNotifiableSample();
		var changed = new List<string>();
		((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		vm.Age = 42;

		changed.AssertContains(nameof(vm.Age));
		changed.AssertContains(nameof(vm.Display));
	}

	[TestMethod]
	public void ObservableProperty_OnViewModelBase_RaisesPropertyChanged()
	{
		var vm = new MvvmViewModelBaseSample();
		var changed = new List<string>();
		((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		vm.Title = "hello";

		vm.Title.AssertEqual("hello");
		changed.AssertContains(nameof(vm.Title));
	}

	[TestMethod]
	public void RelayCommand_Sync_Executes()
	{
		var vm = new MvvmSyncCommandsSample();

		vm.RunCommand.Execute(null);

		vm.RunCount.AssertEqual(1);
	}

	[TestMethod]
	public void RelayCommand_Sync_CanExecute_Gates()
	{
		var vm = new MvvmSyncCommandsSample { CanRun = false };

		vm.RunCommand.CanExecute(null).AssertFalse();

		vm.CanRun = true;
		vm.RunCommand.CanExecute(null).AssertTrue();
	}

	[TestMethod]
	public void RelayCommand_WithParameter_PassesArgument()
	{
		var vm = new MvvmSyncCommandsSample();

		vm.AddCommand.Execute(7);

		vm.LastAdded.AssertEqual(7);
	}

	[TestMethod]
	public void NotifyCanExecuteChangedFor_RaisesCanExecuteChanged()
	{
		var vm = new MvvmSyncCommandsSample();
		var raised = 0;
		vm.RunCommand.CanExecuteChanged += (_, _) => raised++;

		vm.Enabled = true;

		raised.AssertGreater(0);
	}

	[TestMethod]
	public void RelayCommand_Sync_ReturnsSameInstance()
	{
		var vm = new MvvmSyncCommandsSample();

		var a = vm.RunCommand;
		var b = vm.RunCommand;

		ReferenceEquals(a, b).AssertTrue();
	}

	[TestMethod]
	public async Task RelayCommand_Async_Executes_And_Cancels()
	{
		var vm = new MvvmAsyncCommandsSample();
		var cmd = vm.DownloadCommand;

		var run = cmd.ExecuteAsync();

		await vm.Entered.Task;
		cmd.IsExecuting.AssertTrue();
		vm.Started.AssertEqual(1);

		// Cancel through the generated cancel command.
		vm.DownloadCancelCommand.Execute(null);

		try
		{
			await run;
		}
		catch (OperationCanceledException)
		{
		}

		cmd.IsExecuting.AssertFalse();
	}

	[TestMethod]
	public void RelayCommand_AsyncSuffix_StrippedFromCommandName()
	{
		var vm = new MvvmAsyncCommandsSample();

		// SaveAsync -> SaveCommand (an AsyncCommand).
		IsNotNull(vm.SaveCommand);
		(vm.SaveCommand is AsyncCommand).AssertTrue();
	}

	[TestMethod]
	public void ObservableProperty_OnPartialProperty_Works()
	{
		var vm = new MvvmPartialPropertySample();
		var changed = new List<string>();
		((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		vm.Caption = "hi";

		vm.Caption.AssertEqual("hi");
		changed.AssertContains(nameof(vm.Caption));
	}

	[TestMethod]
	public void ObservableProperty_OnPartialProperty_ForwardsAttributes()
	{
		var prop = typeof(MvvmPartialPropertyAttrSample).GetProperty(nameof(MvvmPartialPropertyAttrSample.Token));

		IsNotNull(prop);
		(prop.GetCustomAttributes(typeof(System.ComponentModel.BrowsableAttribute), false).Length > 0).AssertTrue();
	}

	[TestMethod]
	public async Task RelayCommand_AllowConcurrentExecutions_CanExecuteWhileRunning()
	{
		var vm = new MvvmConcurrentSample();
		var cmd = vm.WorkCommand;

		var run = cmd.ExecuteAsync();
		await vm.Entered.Task;

		cmd.IsExecuting.AssertTrue();
		// With concurrency allowed the command stays executable while already running.
		cmd.CanExecute(null).AssertTrue();

		cmd.Cancel();

		try
		{
			await run;
		}
		catch (OperationCanceledException)
		{
		}
	}

	[TestMethod]
	public void NotifyPropertyChangedRecipients_BroadcastsOnChange()
	{
		IMessenger m = new StrongReferenceMessenger();
		var vm = new MvvmRecipientSample(m);

		PropertyChangedMessage<int> got = null;
		var listener = new object();
		m.Register<object, PropertyChangedMessage<int>>(listener, (r, msg) => got = msg);

		vm.Level = 3;

		IsNotNull(got);
		got.NewValue.AssertEqual(3);
		got.OldValue.AssertEqual(0);
		got.PropertyName.AssertEqual(nameof(vm.Level));
	}

	[TestMethod]
	public void Diagnostic_BroadcastWithoutRecipientBase_IsReported()
		=> AssertDiagnostic("ECNGMVVM008", @"
using Ecng.ComponentModel;
namespace T { public partial class Vm : NotifiableObject { [ObservableProperty][NotifyPropertyChangedRecipients] private int _a; } }");

	[TestMethod]
	public void Generated_Code_Is_ClsCompliant()
	{
		const string source = @"
using System;
using Ecng.ComponentModel;

[assembly: CLSCompliant(true)]

namespace ClsSample
{
	public partial class SampleVm : NotifiableObject
	{
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Display))]
		private int _count;

		public string Display => Count.ToString();

		[RelayCommand(CanExecute = nameof(CanRun))]
		private void Run() { }

		private bool CanRun() => true;

		[RelayCommand(IncludeCancelCommand = true)]
		private System.Threading.Tasks.Task DownloadAsync(System.Threading.CancellationToken token)
			=> System.Threading.Tasks.Task.CompletedTask;
	}
}";

		var (generatorDiagnostics, output) = RunGenerator(source);

		// The generator itself must not report any diagnostics for valid input.
		generatorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error).AssertFalse();

		var diagnostics = output.GetDiagnostics();

		var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
		if (errors.Length > 0)
			Fail($"Compilation errors:\n{errors.Select(e => e.ToString()).JoinNL()}");

		// No CLS-compliance warnings (CS3001/CS3002/CS3003/CS3009/...) may be produced.
		var clsWarnings = diagnostics.Where(d => d.Id.StartsWith("CS3", StringComparison.Ordinal)).ToArray();
		if (clsWarnings.Length > 0)
			Fail($"CLS warnings:\n{clsWarnings.Select(w => w.ToString()).JoinNL()}");
	}

	[TestMethod]
	public void Diagnostic_NotPartial_IsReported()
		=> AssertDiagnostic("ECNGMVVM002", @"
using Ecng.ComponentModel;
namespace T { public class Vm : NotifiableObject { [ObservableProperty] private int _a; } }");

	[TestMethod]
	public void Diagnostic_NoNotifyBase_IsReported()
		=> AssertDiagnostic("ECNGMVVM003", @"
using Ecng.ComponentModel;
namespace T { public partial class Vm { [ObservableProperty] private int _a; } }");

	[TestMethod]
	public void Diagnostic_NameConflict_IsReported()
		=> AssertDiagnostic("ECNGMVVM004", @"
using Ecng.ComponentModel;
namespace T { public partial class Vm : NotifiableObject { [ObservableProperty] private int _a; public int A { get; set; } } }");

	[TestMethod]
	public void Diagnostic_BadCanExecute_IsReported()
		=> AssertDiagnostic("ECNGMVVM005", @"
using Ecng.ComponentModel;
namespace T { public partial class Vm : NotifiableObject { [RelayCommand(CanExecute = ""Missing"")] private void Run() { } } }");

	[TestMethod]
	public void Diagnostic_UnsupportedSignature_IsReported()
		=> AssertDiagnostic("ECNGMVVM006", @"
using Ecng.ComponentModel;
namespace T { public partial class Vm : NotifiableObject { [RelayCommand] private void Run(int a, int b) { } } }");

	[TestMethod]
	public void Diagnostic_UnknownNotifyTarget_IsReported()
		=> AssertDiagnostic("ECNGMVVM007", @"
using Ecng.ComponentModel;
namespace T { public partial class Vm : NotifiableObject { [ObservableProperty][NotifyPropertyChangedFor(""Nope"")] private int _a; } }");

	// Runs the MVVM generator over the source and asserts the expected diagnostic id is reported.
	private void AssertDiagnostic(string id, string source)
	{
		var (generatorDiagnostics, _) = RunGenerator(source);

		if (!generatorDiagnostics.Any(d => d.Id == id))
			Fail($"Expected diagnostic {id}, got: {string.Join(", ", generatorDiagnostics.Select(d => d.Id))}");
	}

	// Runs the MVVM generator over the source against the current runtime's references plus
	// Ecng.ComponentModel, returning the generator diagnostics and the updated compilation.
	private static (System.Collections.Immutable.ImmutableArray<Diagnostic> generatorDiagnostics, Compilation output) RunGenerator(string source)
	{
		var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);

		var refs = tpa
			.Where(p => p.Length > 0)
			.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
			.Append(MetadataReference.CreateFromFile(typeof(NotifiableObject).Assembly.Location))
			.ToArray();

		var compilation = CSharpCompilation.Create(
			"MvvmGenTest",
			[CSharpSyntaxTree.ParseText(source)],
			refs,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		CSharpGeneratorDriver
			.Create(new MvvmGenerator().AsSourceGenerator())
			.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

		return (generatorDiagnostics, output);
	}
}

#endif
