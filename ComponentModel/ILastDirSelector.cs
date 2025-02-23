namespace Ecng.ComponentModel;

/// <summary>
/// Interface describes last directory selector.
/// </summary>
public interface ILastDirSelector
{
	/// <summary>
	/// Try to get value by control name.
	/// </summary>
	/// <param name="ctrlName">Control name.</param>
	/// <param name="value">The value.</param>
	/// <returns>Opeation result.</returns>
	bool TryGetValue(string ctrlName, out string value);

	/// <summary>
	/// Set value by control name.
	/// </summary>
	/// <param name="ctrlName">Control name.</param>
	/// <param name="value">The value.</param>
	void SetValue(string ctrlName, string value);
}
