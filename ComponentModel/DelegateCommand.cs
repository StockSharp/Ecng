namespace Ecng.ComponentModel;

using System;
using System.Windows.Input;

/// <summary>
/// Delegate command capable of taking argument.
/// </summary>
/// <typeparam name="T">The argument type.</typeparam>
public class DelegateCommand<T> : ICommand
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
	}

	/// <inheritdoc />
	public void Execute(object parameter) => _execute((T)parameter);

	/// <inheritdoc />
	public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

	/// <inheritdoc />
	public event EventHandler CanExecuteChanged;

	/// <summary>
	/// Invoke <see cref="CanExecuteChanged"/> event.
	/// </summary>
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
