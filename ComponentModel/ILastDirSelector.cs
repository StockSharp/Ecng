namespace Ecng.ComponentModel;

/// <summary>
/// Interface describes last directory selector.
/// </summary>
public interface ILastDirSelector
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="ctrlName"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	bool TryGetValue(string ctrlName, out string value);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="ctrlName"></param>
	/// <param name="value"></param>
	void SetValue(string ctrlName, string value);
}
