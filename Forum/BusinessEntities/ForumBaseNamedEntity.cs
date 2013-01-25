namespace Ecng.Forum.BusinessEntities
{
	using Ecng.ComponentModel;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	public abstract class ForumBaseNamedEntity : ForumBaseEntity
	{
		protected ForumBaseNamedEntity()
		{
			Description = string.Empty;
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
			return Name;
		}
	}
}