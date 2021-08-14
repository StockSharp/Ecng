namespace Ecng.Reflection.Emit
{
	using System;

	using Ecng.Collections;

	public class AttributeGeneratorList : BaseList<AttributeGenerator>
	{
		private readonly object _owner;

		internal AttributeGeneratorList(object owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		protected override void OnAdded(AttributeGenerator item)
		{
			item.SetCustomAttribute(_owner);
			base.OnAdded(item);
		}
	}
}