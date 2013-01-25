namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	[Serializable]
	[Audit((byte)AuditSchemas.Forum)]
	public class Forum : ForumBaseNamedEntity
	{
		[AuditField((byte)AuditFields.IsLocked)]
		public bool IsLocked { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.Folder)]
		public ForumFolder Folder { get; set; }

		[RelationMany(typeof(ForumTopicList), CacheCount = true)]
		public TopicList Topics { get; protected set; }

		[RelationMany(typeof(ForumMessageList), CacheCount = true)]
		public MessageList Messages { get; protected set; }

		[RelationMany(typeof(ForumViewList))]
		public ViewList Views { get; protected set; }

		[RelationMany(typeof(ForumModerationVoteList))]
		public ModerationVoteList ModerationVotes { get; protected set; }

		[RelationMany(typeof(ForumTopicTagList))]
		public TopicTagList TopicTags { get; protected set; }

		[RelationMany(typeof(ForumGradeList))]
		public GradeList Grades { get; protected set; }
	}
}