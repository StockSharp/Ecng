namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	#endregion

	public enum TopicTypes
	{
		Discuss,
		Locked,
		Announce,
		Sticky,
	}

	[Serializable]
	[Audit((byte)AuditSchemas.Topic)]
	public class Topic : ForumBaseNamedEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.Forum)]
		public Forum Forum { get; set; }

		[AuditField((byte)AuditFields.Type)]
		public TopicTypes Type { get; set; }

		[RelationMany(typeof(TopicMessageList), BulkLoad = true)]
		public MessageList Messages { get; protected set; }

		[RelationMany(typeof(TopicTopicTagList))]
		public TopicTagList Tags { get; protected set; }

		[RelationMany(typeof(TopicViewList), BulkLoad = true)]
		public ViewList Views { get; protected set; }

		[RelationMany(typeof(TopicModerationVoteList))]
		public ModerationVoteList ModerationVotes { get; protected set; }

		[RelationMany(typeof(TopicGradeList))]
		public GradeList Grades { get; protected set; }
	}
}