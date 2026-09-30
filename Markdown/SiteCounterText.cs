namespace Ecng.Markdown;

using SmartFormat.Utilities;

/// <summary>
/// What a site counter reads as once its value is known: the number, and the word form the number asks for
/// when the author wrote the forms ("@user_count(пользователь/пользователя/пользователей)").
/// </summary>
public static class SiteCounterText
{
	/// <summary>
	/// The text a counter stands for.
	/// </summary>
	/// <param name="key">The key the host resolves the counter by.</param>
	/// <param name="forms">The word forms written after it, in the order its language's plural rule counts them; empty when the number stands alone.</param>
	/// <param name="data">The resolved data.</param>
	/// <returns>The number with its word form, or null when the value is not known.</returns>
	public static string TryResolve(string key, IReadOnlyList<string> forms, ResolvedMarkdownData data)
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(forms);
		ArgumentNullException.ThrowIfNull(data);

		if (!data.Counters.TryGetValue(key, out var text) || text.IsEmpty())
			return null;

		if (forms.Count == 0)
			return text;

		long? value = data.CounterValues.TryGetValue(key, out var count) ? count : null;

		// A non-breaking space: the number and its noun are read as one, and a line break between them is not.
		return $"{text}\u00A0{forms[PickForm(forms.Count, value, data.Language)]}";
	}

	private static int PickForm(int formsCount, long? value, string language)
	{
		// Without the number or a rule for the language, the last form - the general plural - is the one that
		// reads right for most numbers.
		if (value is null || language.IsEmpty() || !PluralRules.IsoLangToDelegate.TryGetValue(language, out var rule))
			return formsCount - 1;

		var index = rule(value.Value, formsCount);

		return index >= 0 && index < formsCount ? index : formsCount - 1;
	}
}
