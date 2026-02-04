namespace Ecng.Tests.ComponentModel;

using System.Threading;
using System.Threading.Tasks;

using Ecng.ComponentModel;

[TestClass]
public class AsyncCommandTests : BaseTestClass
{
	[TestMethod]
	public async Task ExecuteAsync_InvokesAction()
	{
		var executed = false;
		var cmd = new AsyncCommand(() =>
		{
			executed = true;
			return Task.CompletedTask;
		});

		await cmd.ExecuteAsync();

		executed.AssertTrue();
	}

	[TestMethod]
	public async Task ExecuteAsync_WithCancellationToken_PassesToken()
	{
		CancellationToken receivedToken = default;
		var cmd = new AsyncCommand(ct =>
		{
			receivedToken = ct;
			return Task.CompletedTask;
		});

		await cmd.ExecuteAsync();

		receivedToken.CanBeCanceled.AssertTrue();
	}

	[TestMethod]
	public async Task ExecuteAsync_WithParameter_InvokesAction()
	{
		object receivedParam = null;
		var cmd = new AsyncCommand<string>(p =>
		{
			receivedParam = p;
			return Task.CompletedTask;
		});

		await cmd.ExecuteAsync("test");

		receivedParam.AssertEqual("test");
	}

	[TestMethod]
	public async Task IsExecuting_TrueDuringExecution()
	{
		var tcs = new TaskCompletionSource<bool>();
		var wasExecuting = false;

		var cmd = new AsyncCommand(async () =>
		{
			await tcs.Task;
		});

		var executeTask = cmd.ExecuteAsync();
		wasExecuting = cmd.IsExecuting;
		tcs.SetResult(true);
		await executeTask;

		wasExecuting.AssertTrue();
		cmd.IsExecuting.AssertFalse();
	}

	[TestMethod]
	public async Task IsExecuting_RaisesPropertyChanged()
	{
		var tcs = new TaskCompletionSource<bool>();
		var changedCount = 0;

		var cmd = new AsyncCommand(async () =>
		{
			await tcs.Task;
		});

		cmd.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(AsyncCommand.IsExecuting))
				changedCount++;
		};

		var executeTask = cmd.ExecuteAsync();
		tcs.SetResult(true);
		await executeTask;

		changedCount.AssertEqual(2);
	}

	[TestMethod]
	public void CanExecute_NoDelegate_ReturnsTrue()
	{
		var cmd = new AsyncCommand(() => Task.CompletedTask);

		cmd.CanExecute(null).AssertTrue();
	}

	[TestMethod]
	public void CanExecute_WithDelegate_ReturnsResult()
	{
		var canExec = true;
		var cmd = new AsyncCommand(() => Task.CompletedTask, () => canExec);

		cmd.CanExecute(null).AssertTrue();

		canExec = false;
		cmd.CanExecute(null).AssertFalse();
	}

	[TestMethod]
	public async Task CanExecute_DuringExecution_ReturnsFalse()
	{
		var tcs = new TaskCompletionSource<bool>();

		var cmd = new AsyncCommand(async () =>
		{
			await tcs.Task;
		});

		var executeTask = cmd.ExecuteAsync();
		var canExecuteDuring = cmd.CanExecute(null);
		tcs.SetResult(true);
		await executeTask;

		canExecuteDuring.AssertFalse();
		cmd.CanExecute(null).AssertTrue();
	}

	[TestMethod]
	public async Task AllowMultipleExecution_True_CanExecuteDuringExecution()
	{
		var tcs = new TaskCompletionSource<bool>();

		var cmd = new AsyncCommand(async () =>
		{
			await tcs.Task;
		}, allowMultipleExecution: true);

		var executeTask = cmd.ExecuteAsync();
		var canExecuteDuring = cmd.CanExecute(null);
		tcs.SetResult(true);
		await executeTask;

		canExecuteDuring.AssertTrue();
	}

	[TestMethod]
	public async Task Cancel_RequestsCancellation()
	{
		var tcs = new TaskCompletionSource<bool>();
		CancellationToken capturedToken = default;

		var cmd = new AsyncCommand(async ct =>
		{
			capturedToken = ct;
			await tcs.Task;
		});

		var executeTask = cmd.ExecuteAsync();
		cmd.Cancel();
		tcs.SetResult(true);
		await executeTask;

		capturedToken.IsCancellationRequested.AssertTrue();
	}

	[TestMethod]
	public async Task CancelCommand_ExecutesCancellation()
	{
		var tcs = new TaskCompletionSource<bool>();
		CancellationToken capturedToken = default;

		var cmd = new AsyncCommand(async ct =>
		{
			capturedToken = ct;
			await tcs.Task;
		});

		var executeTask = cmd.ExecuteAsync();
		cmd.CancelCommand.Execute(null);
		tcs.SetResult(true);
		await executeTask;

		capturedToken.IsCancellationRequested.AssertTrue();
	}

	[TestMethod]
	public void CancelCommand_CanExecute_FalseWhenNotExecuting()
	{
		var cmd = new AsyncCommand(() => Task.CompletedTask);

		cmd.CancelCommand.CanExecute(null).AssertFalse();
	}

	[TestMethod]
	public async Task CancelCommand_CanExecute_TrueWhenExecuting()
	{
		var tcs = new TaskCompletionSource<bool>();

		var cmd = new AsyncCommand(async () =>
		{
			await tcs.Task;
		});

		var executeTask = cmd.ExecuteAsync();
		var canCancel = cmd.CancelCommand.CanExecute(null);
		tcs.SetResult(true);
		await executeTask;

		canCancel.AssertTrue();
	}

	[TestMethod]
	public void IsCancellationRequested_FalseInitially()
	{
		var cmd = new AsyncCommand(() => Task.CompletedTask);

		cmd.IsCancellationRequested.AssertFalse();
	}

	[TestMethod]
	public async Task IsCancellationRequested_TrueAfterCancel()
	{
		var tcs = new TaskCompletionSource<bool>();

		var cmd = new AsyncCommand(async () =>
		{
			await tcs.Task;
		});

		var executeTask = cmd.ExecuteAsync();
		cmd.Cancel();
		var isCancelled = cmd.IsCancellationRequested;
		tcs.SetResult(true);
		await executeTask;

		isCancelled.AssertTrue();
	}

	[TestMethod]
	public void RaiseCanExecuteChanged_InvokesEvent()
	{
		var cmd = new AsyncCommand(() => Task.CompletedTask);
		var eventRaised = false;

		cmd.CanExecuteChanged += (s, e) => eventRaised = true;
		cmd.RaiseCanExecuteChanged();

		eventRaised.AssertTrue();
	}

	[TestMethod]
	public void Constructor_NullExecute_ThrowsArgumentNullException()
	{
		ThrowsExactly<ArgumentNullException>(() => new AsyncCommand((Func<Task>)null));
		ThrowsExactly<ArgumentNullException>(() => new AsyncCommand((Func<CancellationToken, Task>)null));
		ThrowsExactly<ArgumentNullException>(() => new AsyncCommand<string>((Func<string, Task>)null));
		ThrowsExactly<ArgumentNullException>(() => new AsyncCommand<string>((Func<string, CancellationToken, Task>)null));
	}

	[TestMethod]
	public async Task GenericCommand_ExecuteWithCorrectType()
	{
		var result = 0;
		var cmd = new AsyncCommand<int>(v =>
		{
			result = v * 2;
			return Task.CompletedTask;
		});

		await cmd.ExecuteAsync(5);

		result.AssertEqual(10);
	}

	[TestMethod]
	public void CanExecute_WithParameter_PassesParameter()
	{
		var cmd = new AsyncCommand<int>(_ => Task.CompletedTask, p => p > 0);

		cmd.CanExecute(1).AssertTrue();
		cmd.CanExecute(0).AssertFalse();
		cmd.CanExecute(-1).AssertFalse();
	}

	[TestMethod]
	public void Dispose_CancelsExecution()
	{
		var tcs = new TaskCompletionSource<bool>();
		CancellationToken capturedToken = default;

		var cmd = new AsyncCommand(async ct =>
		{
			capturedToken = ct;
			await tcs.Task;
		});

		_ = cmd.ExecuteAsync();
		cmd.Dispose();

		capturedToken.IsCancellationRequested.AssertTrue();
	}

	[TestMethod]
	public void Dispose_CanExecuteReturnsFalse()
	{
		var cmd = new AsyncCommand(() => Task.CompletedTask);

		cmd.Dispose();

		cmd.CanExecute(null).AssertFalse();
	}

	[TestMethod]
	public async Task Execute_ViaICommand_Works()
	{
		var executed = false;
		var cmd = new AsyncCommand(() =>
		{
			executed = true;
			return Task.CompletedTask;
		});

		((System.Windows.Input.ICommand)cmd).Execute(null);
		await Task.Delay(50, CancellationToken);

		executed.AssertTrue();
	}

	[TestMethod]
	public async Task ExecuteAsync_WhenCannotExecute_DoesNothing()
	{
		var executed = false;
		var cmd = new AsyncCommand(() =>
		{
			executed = true;
			return Task.CompletedTask;
		}, () => false);

		await cmd.ExecuteAsync();

		executed.AssertFalse();
	}

	[TestMethod]
	public async Task IsExecuting_SetsBackToFalse_OnException()
	{
		var cmd = new AsyncCommand(() => throw new InvalidOperationException("Test"));

		try
		{
			await cmd.ExecuteAsync();
		}
		catch (InvalidOperationException)
		{
		}

		cmd.IsExecuting.AssertFalse();
	}

	[TestMethod]
	public async Task ExecuteAsync_ThrowsException_Propagates()
	{
		var cmd = new AsyncCommand(() => throw new InvalidOperationException("Test error"));

		await ThrowsExactlyAsync<InvalidOperationException>(async () => await cmd.ExecuteAsync());
	}

	[TestMethod]
	public async Task ExecuteAsync_ThrowsException_WithParameter_Propagates()
	{
		var cmd = new AsyncCommand<int>(_ => throw new ArgumentException("Bad param"));

		await ThrowsExactlyAsync<ArgumentException>(async () => await cmd.ExecuteAsync(42));
	}

	[TestMethod]
	public async Task ExecuteAsync_OperationCanceledException_Propagates()
	{
		var tcs = new TaskCompletionSource<bool>();

		var cmd = new AsyncCommand(async ct =>
		{
			await tcs.Task;
			ct.ThrowIfCancellationRequested();
		});

		var task = cmd.ExecuteAsync();
		cmd.Cancel();
		tcs.SetResult(true);

		await ThrowsExactlyAsync<OperationCanceledException>(async () => await task);
	}

	[TestMethod]
	public async Task ExecuteAsync_TaskCanceledException_Propagates()
	{
		var cmd = new AsyncCommand(async ct =>
		{
			await Task.Delay(Timeout.Infinite, ct);
		});

		var task = cmd.ExecuteAsync();
		cmd.Cancel();

		await ThrowsExactlyAsync<TaskCanceledException>(async () => await task);
	}

	[TestMethod]
	public async Task ExecuteAsync_OnException_CanExecuteChangedRaised()
	{
		var cmd = new AsyncCommand(() => throw new InvalidOperationException("Test"));
		var canExecuteChangedCount = 0;

		cmd.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

		try
		{
			await cmd.ExecuteAsync();
		}
		catch (InvalidOperationException)
		{
		}

		canExecuteChangedCount.AssertEqual(2);
	}

	[TestMethod]
	public async Task ExecuteAsync_OnException_CancelCommandUpdated()
	{
		var cmd = new AsyncCommand(() => throw new InvalidOperationException("Test"));
		var cancelCanExecuteChangedCount = 0;

		cmd.CancelCommand.CanExecuteChanged += (s, e) => cancelCanExecuteChangedCount++;

		try
		{
			await cmd.ExecuteAsync();
		}
		catch (InvalidOperationException)
		{
		}

		cancelCanExecuteChangedCount.AssertEqual(2);
		cmd.CancelCommand.CanExecute(null).AssertFalse();
	}

	[TestMethod]
	public async Task ExecuteAsync_OnException_CanExecuteAgain()
	{
		var callCount = 0;
		var cmd = new AsyncCommand(() =>
		{
			callCount++;
			if (callCount == 1)
				throw new InvalidOperationException("First call fails");
			return Task.CompletedTask;
		});

		try
		{
			await cmd.ExecuteAsync();
		}
		catch (InvalidOperationException)
		{
		}

		cmd.CanExecute(null).AssertTrue();

		await cmd.ExecuteAsync();

		callCount.AssertEqual(2);
	}

	[TestMethod]
	public async Task ExecuteAsync_AsyncException_Propagates()
	{
		var cmd = new AsyncCommand(async () =>
		{
			await Task.Yield();
			throw new InvalidOperationException("Async error");
		});

		await ThrowsExactlyAsync<InvalidOperationException>(async () => await cmd.ExecuteAsync());
		cmd.IsExecuting.AssertFalse();
	}

	[TestMethod]
	public async Task ExecuteAsync_ExceptionAfterDelay_Propagates()
	{
		var cmd = new AsyncCommand(async ct =>
		{
			await Task.Delay(10, ct);
			throw new ApplicationException("Delayed error");
		});

		await ThrowsExactlyAsync<ApplicationException>(async () => await cmd.ExecuteAsync());
		cmd.IsExecuting.AssertFalse();
	}

	[TestMethod]
	public async Task ExecuteAsync_AggregateException_InnerExceptionPropagates()
	{
		var cmd = new AsyncCommand(async () =>
		{
			await Task.WhenAll(
				Task.FromException(new InvalidOperationException("Error 1")),
				Task.CompletedTask
			);
		});

		await ThrowsExactlyAsync<InvalidOperationException>(async () => await cmd.ExecuteAsync());
	}
}
