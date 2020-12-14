namespace Ecng.Xaml
{
	using System;
	using System.Windows.Input;

	/// <summary>
	/// Delegate command capable of taking argument.
	/// <typeparam name="T">The argument type.</typeparam>
	/// </summary>
	public class DelegateCommand<T> : ICommand
	{
		private readonly Action<T> _execute;
		private readonly Predicate<T> _canExecute;
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly EventHandler _requerySuggestedHandler;

		/// <summary>
		/// Creates a new command with conditional execution.
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic.</param>
		/// </summary>
		public DelegateCommand(Action<T> execute, Predicate<T> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;

			if(canExecute != null)
			{
				// CommandManager.RequerySuggested хранит ссылки на обработчики как weak ref, поэтому важно хранить ссылку на делегат.
				// https://docs.microsoft.com/en-us/dotnet/api/System.Windows.Input.CommandManager.RequerySuggested
				_requerySuggestedHandler = CommandManagerOnRequerySuggested;
				CommandManager.RequerySuggested += _requerySuggestedHandler;
			}
		}

		private void CommandManagerOnRequerySuggested(object sender, EventArgs e) => CanExecuteChanged?.Invoke(this, e);

		public void Execute(object parameter) => _execute((T) parameter);

		public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T) parameter);

		public event EventHandler CanExecuteChanged;
	}

	public class DelegateCommand : DelegateCommand<object>
	{
		public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null) : base(execute, canExecute) { }
	}
}