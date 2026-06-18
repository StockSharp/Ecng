#if NET10_0_OR_GREATER

namespace Ecng.Tests.ComponentModel;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.ComponentModel;

// Observable properties on a NotifiableObject base (has both NotifyChanged and NotifyChanging).
partial class MvvmNotifiableSample : NotifiableObject
{
	[ObservableProperty]
	private string _name;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Display))]
	private int _age;

	public string Display => $"{Name} ({Age})";

	// Hook log to verify call order and values.
	public List<string> Log { get; } = [];

	partial void OnNameChanging(string value) => Log.Add($"changing:{value}");
	partial void OnNameChanging(string oldValue, string newValue) => Log.Add($"changing2:{oldValue}->{newValue}");
	partial void OnNameChanged(string value) => Log.Add($"changed:{value}");
	partial void OnNameChanged(string oldValue, string newValue) => Log.Add($"changed2:{oldValue}->{newValue}");
}

// Observable properties on a ViewModelBase base (has OnPropertyChanged, no PropertyChanging).
partial class MvvmViewModelBaseSample : ViewModelBase
{
	[ObservableProperty]
	private string _title;
}

// Synchronous commands with CanExecute and a property that revalidates a command.
partial class MvvmSyncCommandsSample : ViewModelBase
{
	public int RunCount;
	public int LastAdded;
	public bool CanRun = true;

	[RelayCommand(CanExecute = nameof(CanRunMethod))]
	private void Run() => RunCount++;

	private bool CanRunMethod() => CanRun;

	[RelayCommand]
	private void Add(int value) => LastAdded = value;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(RunCommand))]
	private bool _enabled;
}

// Asynchronous commands: cancellation token flow, IncludeCancelCommand and Async-suffix stripping.
partial class MvvmAsyncCommandsSample : ViewModelBase
{
	public int Started;
	public readonly TaskCompletionSource<bool> Entered = new();

	[RelayCommand(IncludeCancelCommand = true)]
	private async Task Download(CancellationToken token)
	{
		Started++;
		Entered.TrySetResult(true);
		await Task.Delay(Timeout.Infinite, token);
	}

	[RelayCommand]
	private Task SaveAsync() => Task.CompletedTask;
}

// [ObservableProperty] on a C# 13 partial property (no separate backing field).
partial class MvvmPartialPropertySample : NotifiableObject
{
	[ObservableProperty]
	public partial string Caption { get; set; }
}

// Attributes on a partial property must end up on the generated property (the supported way to
// forward attributes, since the compiler merges both partial declarations).
partial class MvvmPartialPropertyAttrSample : NotifiableObject
{
	[ObservableProperty]
	[System.ComponentModel.Browsable(false)]
	public partial string Token { get; set; }
}

// [NotifyPropertyChangedRecipients] on an ObservableRecipient broadcasts a PropertyChangedMessage.
partial class MvvmRecipientSample : ObservableRecipient
{
	public MvvmRecipientSample(IMessenger messenger)
		: base(messenger)
	{
	}

	[ObservableProperty]
	[NotifyPropertyChangedRecipients]
	private int _level;
}

// AllowConcurrentExecutions lets a second execution start while the first is still running.
partial class MvvmConcurrentSample : ViewModelBase
{
	public readonly TaskCompletionSource<bool> Entered = new();

	[RelayCommand(AllowConcurrentExecutions = true)]
	private async Task Work(CancellationToken token)
	{
		Entered.TrySetResult(true);
		await Task.Delay(Timeout.Infinite, token);
	}
}

#endif
