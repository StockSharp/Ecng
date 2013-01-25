namespace Ecng.Reflection.Emit
{
	using System;

	using Ecng.Collections;

	public class AttributeGeneratorList : BaseList<AttributeGenerator>
	{
		private readonly object _owner;

		internal AttributeGeneratorList(object owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");

			_owner = owner;
		}

		protected override void OnAdded(AttributeGenerator item)
		{
			item.SetCustomAttribute(_owner);
			base.OnAdded(item);
		}
	}
}