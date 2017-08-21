namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	public class VectorIconAttribute : Attribute
	{
		public string Icon { get; }

		public VectorIconAttribute(string icon)
		{
			if (icon.IsEmpty())
				throw new ArgumentNullException(nameof(icon));

			Icon = icon;
		}
	}
}