namespace Ecng.Reflection.Emit
{
	using System;

	using Ecng.Collections;

	public class AttributeGeneratorList<T> : BaseList<AttributeGenerator>
	{
		private readonly T _owner;

		internal AttributeGeneratorList(T owner)
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