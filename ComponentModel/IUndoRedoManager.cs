namespace Ecng.ComponentModel;

using System.Windows.Input;

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