namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Net;

	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Logic.BusinessEntities;

	[Serializable]
	[Audit((byte)AuditSchemas.Message)]
	public class Message : ForumBaseEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.Parent)]
		public Message Parent { get; set; }

		[RelationMany(typeof(ChildMessageList), BulkLoad = true)]
		public MessageList Childs { get; protected set; }

		[String(int.MaxValue)]
		[Validation]
		[AuditField((byte)AuditFields.Body)]
		public string Body { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.Topic)]
		public Topic Topic { get; set; }

		[RelationMany(typeof(MessageModerationVoteList), BulkLoad = true)]
		public ModerationVoteList ModerationVotes { get; protected set; }

		[RelationMany(typeof(MessageTaglineList), BulkLoad = true)]
		public MessageTaglineList Taglines { get; protected set; }

		[RelationMany(typeof(MessageGradeList))]
		public GradeList Grades { get; protected set; }

		[RelationMany(typeof(MessageFileList), BulkLoad = true)]
		public FileList Attachments { get; protected set; }

		[IpAddress]
		public IPAddress IpAddress { get; set; }
	}
}