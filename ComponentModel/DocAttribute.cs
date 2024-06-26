﻿namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Online doc url attribute.
	/// </summary>
	public class DocAttribute : Attribute
	{
		/// <summary>
		/// Online doc url.
		/// </summary>
		public string DocUrl { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DocAttribute"/>.
		/// </summary>
		/// <param name="docUrl">Online doc url.</param>
		public DocAttribute(string docUrl)
		{
			DocUrl = docUrl.ThrowIfEmpty(nameof(docUrl));
		}
	}
}