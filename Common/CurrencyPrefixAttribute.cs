namespace Ecng.Common;

using System;

/// <summary>
/// Attribute that specifies the prefix/symbol for a currency enum member.
/// </summary>
/// <remarks>
/// Initialize <see cref="CurrencyPrefixAttribute"/>.
/// </remarks>
/// <param name="prefix"><see cref="Prefix"/></param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class CurrencyPrefixAttribute(string prefix) : Attribute
{
	/// <summary>
	/// Currency symbol or prefix.
	/// </summary>
	public string Prefix { get; } = prefix.ThrowIfEmpty(nameof(prefix));
}