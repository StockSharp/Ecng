namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	[Audit((byte)AuditSchemas.PollChoice)]
	public class PollChoice : ForumBaseNamedEntity
	{
		#region Number

		[AuditField((byte)AuditFields.Number)]
		public int Number { get; set; }

		#endregion

		#region Poll

		[RelationSingle]
		[AuditField((byte)AuditFields.Poll)]
		public Poll Poll { get; set; }

		#endregion

		#region Votes

		[RelationMany(typeof(ChoicePollVoteList))]
		public PollVoteList Votes { get; protected set; }

		#endregion

		#region Users

		[RelationMany(typeof(PollChoiceUserList))]
		public ForumUserList Users { get; protected set; }

		#endregion
	}
}