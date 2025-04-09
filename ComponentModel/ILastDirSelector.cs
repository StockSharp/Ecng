namespace Ecng.ComponentModel;

using System.Collections.Generic;

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

/// <summary>
/// In-memory implementation of <see cref="ILastDirSelector"/>.
/// </summary>
public class InMemoryLastDirSelector : ILastDirSelector
{
	private readonly Dictionary<string, string> _cache = [];

	void ILastDirSelector.SetValue(string ctrlName, string value)
		=> _cache[ctrlName] = value;

	bool ILastDirSelector.TryGetValue(string ctrlName, out string value)
		=> _cache.TryGetValue(ctrlName, out value);
}