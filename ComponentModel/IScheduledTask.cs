namespace Ecng.ComponentModel;

/// <summary>
/// Represents a task that operates within a working time schedule.
/// </summary>
public interface IScheduledTask
{
	/// <summary>
	/// Working time settings.
	/// </summary>
	WorkingTime WorkingTime { get; }

	/// <summary>
	/// Whether the task can start.
	/// </summary>
	bool CanStart { get; }

	/// <summary>
	/// Whether the task can stop.
	/// </summary>
	bool CanStop { get; }
}
