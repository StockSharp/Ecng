namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Icon attribute.
	/// </summary>
	public class IconAttribute : Attribute
	{
		/// <summary>
		/// Icon url.
		/// </summary>
		public string Icon { get; }

		public bool IsFullPath { get; }

		/// <summary>
		/// Create <see cref="IconAttribute"/>.
		/// </summary>
		/// <param name="icon">Icon url.</param>
		/// <param name="isFullPath"></param>
		public IconAttribute(string icon, bool isFullPath = false)
		{
			Icon = icon.ThrowIfEmpty(nameof(icon));
			IsFullPath = isFullPath;
		}
	}
}