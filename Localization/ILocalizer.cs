namespace Ecng.Localization;

/// <summary>
/// The interface for localizing strings.
/// </summary>
public interface ILocalizer
{
	/// <summary>
	/// Localizes the string.
	/// </summary>
	/// <param name="enStr">The string to localize on English.</param>
	/// <returns>The localized string.</returns>
	public string Localize(string enStr);
}