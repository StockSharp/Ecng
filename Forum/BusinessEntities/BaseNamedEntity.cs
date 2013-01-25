namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	public abstract class BaseNamedEntity : BaseEntity
	{
		protected BaseNamedEntity()
		{
			this.Description = string.Empty;
		}

		[String(1, 512)]
		[Validation]
		[Index]
		[AuditField((byte)AuditFields.Name)]
		public string Name { get; set; }

		[String(2048)]
		[Validation]
		[AuditField((byte)AuditFields.Description)]
		public string Description { get; set; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}