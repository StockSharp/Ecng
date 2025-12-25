namespace Ecng.ComponentModel;

using System;

using Ecng.Common;

/// <summary>
/// Global settings for <see cref="DelegateCommand{T}"/>.
/// </summary>
public static class DelegateCommandSettings
{
	/// <summary>
	/// Global command registry for automatic CanExecute revalidation.
	/// Set this to a WPF-specific implementation that hooks into CommandManager.RequerySuggested.
	/// </summary>
	public static ICommandRegistry Registry { get; set; }
}

/// <summary>
/// Delegate command capable of taking argument.
/// </summary>
/// <typeparam name="T">The argument type.</typeparam>
public class DelegateCommand<T> : Disposable, IRevalidatableCommand
{
	private readonly Action<T> _execute;
	private readonly Func<T, bool> _canExecute;

	/// <summary>
	/// Creates a new command with conditional execution.
	/// </summary>
	/// <param name="execute">The execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	public DelegateCommand(Action<T> execute, Func<T, bool> canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException(nameof(execute));
		_canExecute = canExecute;

		if (_canExecute != null)
			DelegateCommandSettings.Registry?.Register(this);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		DelegateCommandSettings.Registry?.Unregister(this);
		base.DisposeManaged();
	}

	/// <inheritdoc />
	public void Execute(object parameter) => _execute((T)parameter);

	/// <inheritdoc />
	public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

	/// <inheritdoc />
	public event EventHandler CanExecuteChanged;

	/// <inheritdoc />
	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Delegate command without argument.
/// </summary>
public class DelegateCommand : DelegateCommand<object>
{
	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="execute">The execution logic.</param>
	public DelegateCommand(Action execute)
		: this(_ => execute())
	{
		if (execute is null)
			throw new ArgumentNullException(nameof(execute));
	}

	/// <summary>
	/// Creates a new command with conditional execution.
	/// </summary>
	/// <param name="execute">The execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
		: base(execute, canExecute)
	{
	}
}
