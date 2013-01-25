namespace Ecng.Common
{
	using System;

	public abstract class NameAttribute : OrderedAttribute
	{
		protected NameAttribute(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			Name = name;
		}

		public string Name { get; private set; }
	}
}