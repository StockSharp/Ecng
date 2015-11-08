namespace Ecng.Xaml
{
	using System;
	using System.Windows.Input;

	public class DelegateCommand : ICommand
	{
		private readonly Predicate<object> _canExecute;
		private readonly Action<object> _execute;

		public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
		{
			if (execute == null)
				throw new ArgumentNullException(nameof(execute));

			_execute = execute;
			_canExecute = canExecute;
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute(parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			add
			{
				if (_canExecute != null)
					CommandManager.RequerySuggested += value;
			}
			remove
			{
				if (_canExecute != null)
					CommandManager.RequerySuggested -= value;
			}
		}
	}

	/// <summary>
	/// Delegate command capable of taking argument.
	/// <typeparam name="T">The argument type.</typeparam>
	/// </summary>
	public class DelegateCommand<T> : ICommand
	{
		private readonly Action<T> _execute = null;
		private readonly Predicate<T> _canExecute = null;

		/// <summary>
		/// Creates a new command that can always execute.
		/// <param name="execute">The execution logic.</param>
		/// </summary>
		public DelegateCommand(Action<T> execute)
			: this(execute, null)
		{
		}

		/// <summary>
		/// Creates a new command with conditional execution.
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic.</param>
		/// </summary>
		public DelegateCommand(Action<T> execute, Predicate<T> canExecute)
		{
			if (execute == null)
				throw new ArgumentNullException(nameof(execute));

			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute((T) parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			add
			{
				if (_canExecute != null)
					CommandManager.RequerySuggested += value;
			}
			remove
			{
				if (_canExecute != null)
					CommandManager.RequerySuggested -= value;
			}
		}

		public void Execute(object parameter)
		{
			_execute((T) parameter);
		}
	}
}