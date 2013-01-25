namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	[Audit((byte)AuditSchemas.ForumFolder)]
	public class ForumFolder : ForumBaseNamedEntity
	{
		#region Parent

		[RelationSingle]
		[AuditField((byte)AuditFields.Parent)]
		public ForumFolder Parent { get; set; }

		#endregion

		#region Childs

		private ForumFolderList _childs;

		[RelationMany(typeof(ChildForumFolderList), BulkLoad = true)]
		public ForumFolderList Childs
		{
			get { return _childs; }
			protected set { _childs = value; }
		}

		#endregion

		#region Forums

		private ForumList _forums;

		[RelationMany(typeof(FolderForumList), BulkLoad = true)]
		public ForumList Forums
		{
			get { return _forums; }
			protected set { _forums = value; }
		}

		#endregion

		#region Topics

		private TopicList _topics;

		[RelationMany(typeof(ForumFolderTopicList))]
		public TopicList Topics
		{
			get { return _topics; }
			protected set { _topics = value; }
		}

		#endregion

		#region Messages

		private MessageList _messages;

		[RelationMany(typeof(ForumFolderMessageList))]
		public MessageList Messages
		{
			get { return _messages; }
			protected set { _messages = value; }
		}

		#endregion

		#region Entries

		private SecurityEntryList _entries;

		[RelationMany(typeof(ForumFolderSecurityEntryList), BulkLoad = true)]
		public SecurityEntryList Entries
		{
			get { return _entries; }
			protected set { _entries = value; }
		}

		#endregion

		#region Grades

		private GradeList _grades;

		[RelationMany(typeof(ForumFolderGradeList))]
		public GradeList Grades
		{
			get { return _grades; }
			protected set { _grades = value; }
		}

		#endregion
	}
}