namespace Ecng.ComponentModel;

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
