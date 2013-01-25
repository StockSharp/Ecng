namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.Serialization;
	using Ecng.Logic.BusinessEntities;

	#endregion

	[Serializable]
	public enum ViewResultTypes
	{
		Always,
		AfterVote,
		AfterExpiration,
	}

	[Serializable]
	[Audit((byte)AuditSchemas.Poll)]
	public class Poll : ForumBaseEntity
	{
		#region Topic

		[RelationSingle]
		[AuditField((byte)AuditFields.Topic)]
		[Index]
		public Topic Topic { get; set; }

		#endregion

		#region CanAddNewChoice

		[AuditField((byte)AuditFields.CanAddNewChoice)]
		public bool CanAddNewChoice { get; set; }

		#endregion

		#region MultiSelect

		[AuditField((byte)AuditFields.MultiSelect)]
		public bool MultiSelect { get; set; }

		#endregion

		#region ExpirationDate

		[Nullable]
		[AuditField((byte)AuditFields.ExpirationDate)]
		public DateTime? ExpirationDate { get; set; }

		#endregion

		#region ViewResultType

		[AuditField((byte)AuditFields.ViewResultType)]
		public ViewResultTypes ViewResultType { get; set; }

		#endregion

		#region Choices

		private PollChoiceList _choices;

		[RelationMany(typeof(PollChoiceList), BulkLoad = true)]
		public PollChoiceList Choices
		{
			get { return _choices; }
			protected set { _choices = value; }
		}

		#endregion

		#region Votes

		private PollVoteList _votes;

		[RelationMany(typeof(PollVoteList), BulkLoad = true)]
		public PollVoteList Votes
		{
			get { return _votes; }
			protected set { _votes = value; }
		}

		#endregion

		#region Users

		[RelationMany(typeof(PollUserList))]
		public ForumUserList Users { get; protected set; }

		#endregion
	}
}