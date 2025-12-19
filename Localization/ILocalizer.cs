namespace Ecng.Localization;

/// <summary>
/// The interface for localizing strings.
/// </summary>
public interface ILocalizer
{
	/// <summary>
	/// Localizes the string.
	/// </summary>
	/// <param name="enStr">The string to localize in English.</param>
	/// <returns>The localized string.</returns>
	public string Localize(string enStr);

	/// <summary>
	/// Localizes the string.
	/// </summary>
	/// <param name="key">The key of the string to localize.</param>
	/// <returns>The localized string.</returns>
	public string LocalizeByKey(string key);
}