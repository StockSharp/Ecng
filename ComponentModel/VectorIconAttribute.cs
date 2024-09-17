namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	public class VectorIconAttribute(string icon) : Attribute
	{
		public string Icon { get; } = icon.ThrowIfEmpty(nameof(icon));
	}
}