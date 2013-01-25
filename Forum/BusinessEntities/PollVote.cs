namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	[Serializable]
	[Audit((byte)AuditSchemas.PollVote)]
	public class PollVote : ForumBaseEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.Poll)]
		public Poll Poll { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.Choice)]
		public PollChoice Choice { get; set; }
	}
}