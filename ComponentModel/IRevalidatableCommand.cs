namespace Ecng.ComponentModel;

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
