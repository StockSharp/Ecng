namespace Ecng.ComponentModel
{
	using System.Collections.Generic;

	using Ecng.Common;

	public class EntityProperty
	{
		public string Name { get; set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }

		public EntityProperty Parent { get; set; }

		public IEnumerable<EntityProperty> Properties { get; set; }

		public string FullDisplayName => Parent is null ? DisplayName : $"{Parent.FullDisplayName} -> {DisplayName}";

		public string ParentName => Parent is null ? string.Empty : Parent.Name;

		public override string ToString()
		{
			return $"{Name} ({FullDisplayName})";
		}
	}
}
