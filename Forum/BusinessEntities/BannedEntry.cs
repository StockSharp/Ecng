namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.ComponentModel;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	[Audit((byte)AuditSchemas.BannedEntry)]
	public class BannedEntry : ForumBaseEntity
	{
		[AuditField((byte)AuditFields.EndDate)]
		public DateTime EndDate { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.Message)]
		public Message Message { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.Rule)]
		public Topic Rule { get; set; }

		[String(2048)]
		[Validation]
		[AuditField((byte)AuditFields.Comment)]
		public string Comment { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.BannedUser)]
		public ForumUser BannedUser { get; set; }
	}
}