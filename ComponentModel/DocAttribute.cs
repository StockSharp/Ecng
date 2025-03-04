namespace Ecng.ComponentModel;

using System;

using Ecng.Common;

/// <summary>
/// Online doc url attribute.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DocAttribute"/>.
/// </remarks>
/// <param name="docUrl">Online doc url.</param>
public class DocAttribute(string docUrl) : Attribute
{
	/// <summary>
	/// Online doc url.
	/// </summary>
	public string DocUrl { get; } = docUrl.ThrowIfEmpty(nameof(docUrl));
}