namespace Ecng.ComponentModel;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Ecng.Common;

/// <summary>
/// Async delegate command capable of taking argument.
/// </summary>
/// <typeparam name="T">The argument type.</typeparam>
public class AsyncCommand<T> : NotifiableObject, IRevalidatableCommand, IDisposable
{
	private readonly Func<T, CancellationToken, Task> _execute;
	private readonly Func<T, bool> _canExecute;
	private readonly bool _allowMultipleExecution;

	private CancellationTokenSource _cts;
	private bool _isExecuting;
	private bool _isDisposed;

	/// <summary>
	/// Creates a new async command.
	/// </summary>
	/// <param name="execute">The async execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	/// <param name="allowMultipleExecution">Allow multiple concurrent executions.</param>
	public AsyncCommand(
		Func<T, CancellationToken, Task> execute,
		Func<T, bool> canExecute = null,
		bool allowMultipleExecution = false)
	{
		_execute = execute ?? throw new ArgumentNullException(nameof(execute));
		_canExecute = canExecute;
		_allowMultipleExecution = allowMultipleExecution;

		CancelCommand = new DelegateCommand(_ => Cancel(), _ => IsExecuting);

		if (_canExecute != null)
			DelegateCommandSettings.Registry?.Register(this);
	}

	/// <summary>
	/// Creates a new async command without cancellation support.
	/// </summary>
	/// <param name="execute">The async execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	/// <param name="allowMultipleExecution">Allow multiple concurrent executions.</param>
	public AsyncCommand(
		Func<T, Task> execute,
		Func<T, bool> canExecute = null,
		bool allowMultipleExecution = false)
		: this((p, _) => execute(p), canExecute, allowMultipleExecution)
	{
		if (execute is null)
			throw new ArgumentNullException(nameof(execute));
	}

	/// <summary>
	/// Gets whether the command is currently executing.
	/// </summary>
	public bool IsExecuting
	{
		get => _isExecuting;
		private set
		{
			if (_isExecuting == value)
				return;

			_isExecuting = value;
			NotifyChanged();
			RaiseCanExecuteChanged();
			CancelCommand.RaiseCanExecuteChanged();
		}
	}

	/// <summary>
	/// Gets the command to cancel the current execution.
	/// </summary>
	public IRevalidatableCommand CancelCommand { get; }

	/// <summary>
	/// Gets whether cancellation has been requested.
	/// </summary>
	public bool IsCancellationRequested => _cts?.IsCancellationRequested ?? false;

	/// <inheritdoc />
	public event EventHandler CanExecuteChanged;

	/// <inheritdoc />
	public bool CanExecute(object parameter)
	{
		if (_isDisposed)
			return false;

		if (!_allowMultipleExecution && IsExecuting)
			return false;

		return _canExecute == null || _canExecute((T)parameter);
	}

	/// <inheritdoc />
	public async void Execute(object parameter)
	{
		await ExecuteAsync((T)parameter);
	}

	/// <summary>
	/// Executes the command asynchronously.
	/// </summary>
	/// <param name="parameter">The command parameter.</param>
	/// <returns>A task representing the async operation.</returns>
	public async Task ExecuteAsync(T parameter)
	{
		if (!CanExecute(parameter))
			return;

		_cts?.Dispose();
		_cts = new CancellationTokenSource();

		IsExecuting = true;

		try
		{
			await _execute(parameter, _cts.Token);
		}
		finally
		{
			IsExecuting = false;
		}
	}

	/// <summary>
	/// Cancels the current execution.
	/// </summary>
	public void Cancel()
	{
		_cts?.Cancel();
	}

	/// <inheritdoc />
	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

	/// <summary>
	/// Disposes the command and cancels any pending operation.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes the command resources.
	/// </summary>
	/// <param name="disposing">True if disposing managed resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			DelegateCommandSettings.Registry?.Unregister(this);
			Cancel();
			_cts?.Dispose();
			_cts = null;
			(CancelCommand as IDisposable)?.Dispose();
		}

		_isDisposed = true;
	}
}

/// <summary>
/// Async delegate command without argument.
/// </summary>
public class AsyncCommand : AsyncCommand<object>
{
	/// <summary>
	/// Creates a new async command.
	/// </summary>
	/// <param name="execute">The async execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	/// <param name="allowMultipleExecution">Allow multiple concurrent executions.</param>
	public AsyncCommand(
		Func<CancellationToken, Task> execute,
		Func<bool> canExecute = null,
		bool allowMultipleExecution = false)
		: base((_, ct) => execute(ct), canExecute != null ? _ => canExecute() : null, allowMultipleExecution)
	{
		if (execute is null)
			throw new ArgumentNullException(nameof(execute));
	}

	/// <summary>
	/// Creates a new async command without cancellation support.
	/// </summary>
	/// <param name="execute">The async execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	/// <param name="allowMultipleExecution">Allow multiple concurrent executions.</param>
	public AsyncCommand(
		Func<Task> execute,
		Func<bool> canExecute = null,
		bool allowMultipleExecution = false)
		: base(_ => execute(), canExecute != null ? _ => canExecute() : null, allowMultipleExecution)
	{
		if (execute is null)
			throw new ArgumentNullException(nameof(execute));
	}

	/// <summary>
	/// Executes the command asynchronously.
	/// </summary>
	/// <returns>A task representing the async operation.</returns>
	public Task ExecuteAsync() => ExecuteAsync(null);
}
