namespace Ecng.ComponentModel;

/// <summary>
/// Undo/redo manager.
/// </summary>
public interface IUndoRedoManager
{
	/// <summary>
	/// Undo command.
	/// </summary>
	ICommand UndoCommand { get; }

	/// <summary>
	/// Redo command.
	/// </summary>
	ICommand RedoCommand { get; }
}