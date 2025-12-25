namespace Ecng.ComponentModel;

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
