namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.ComponentModel;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	[Serializable]
	public enum ModerationVoteTypes
	{
		Delete,
		Move,
		DoNotChange,
	}

	[Serializable]
	[Audit((byte)AuditSchemas.ModerationVote)]
	public class ModerationVote : ForumBaseEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.Message)]
		public Message Message { get; set; }

		[AuditField((byte)AuditFields.Type)]
		public ModerationVoteTypes Type { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.DestinationForum)]
		public Forum DestinationForum { get; set; }

		[String(2048)]
		[AuditField((byte)AuditFields.Reason)]
		public string Reason { get; set; }
	}
}