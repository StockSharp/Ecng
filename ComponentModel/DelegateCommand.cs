namespace Ecng.ComponentModel;

using System;
using System.Windows.Input;

/// <summary>
/// Interface for commands that support CanExecute revalidation.
/// </summary>
public interface IRevalidatableCommand : ICommand
{
	/// <summary>
	/// Invoke CanExecuteChanged event.
	/// </summary>
	void RaiseCanExecuteChanged();
}

/// <summary>
/// Registry for commands that need automatic CanExecute revalidation.
/// </summary>
public interface ICommandRegistry
{
	/// <summary>
	/// Register a command for automatic revalidation.
	/// </summary>
	/// <param name="command">Command to register.</param>
	void Register(IRevalidatableCommand command);

	/// <summary>
	/// Unregister a command.
	/// </summary>
	/// <param name="command">Command to unregister.</param>
	void Unregister(IRevalidatableCommand command);

	/// <summary>
	/// Raise CanExecuteChanged on all registered commands.
	/// </summary>
	void RevalidateAll();
}

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
public class DelegateCommand<T> : IRevalidatableCommand
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
