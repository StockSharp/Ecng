namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Icon attribute.
	/// </summary>
	/// <remarks>
	/// Create <see cref="IconAttribute"/>.
	/// </remarks>
	/// <param name="icon">Icon url.</param>
	/// <param name="isFullPath"></param>
	public class IconAttribute(string icon, bool isFullPath = false) : Attribute
	{
		/// <summary>
		/// Icon url.
		/// </summary>
		public string Icon { get; } = icon.ThrowIfEmpty(nameof(icon));

		public bool IsFullPath { get; } = isFullPath;
	}
}