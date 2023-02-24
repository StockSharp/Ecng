namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	public class VectorIconAttribute : Attribute
	{
		public string Icon { get; }

		public VectorIconAttribute(string icon)
		{
			Icon = icon.ThrowIfEmpty(nameof(icon));
		}
	}
}