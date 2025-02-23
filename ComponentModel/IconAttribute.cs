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
	/// <param name="icon"><see cref="Icon"/></param>
	/// <param name="isFullPath"><see cref="IsFullPath"/></param>
	public class IconAttribute(string icon, bool isFullPath = false) : Attribute
	{
		/// <summary>
		/// Icon url.
		/// </summary>
		public string Icon { get; } = icon.ThrowIfEmpty(nameof(icon));

		/// <summary>
		/// <see cref="Icon"/> is full path.
		/// </summary>
		public bool IsFullPath { get; } = isFullPath;
	}
}