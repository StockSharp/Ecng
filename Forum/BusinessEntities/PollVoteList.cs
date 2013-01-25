namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;

	[Serializable]
	public class PollVoteList : ForumBaseEntityList<PollVote>
	{
		#region PollVoteList.ctor()

		public PollVoteList(IStorage storage, Poll poll)
			: this(storage)
		{
			AddFilter(poll);
		}

		protected PollVoteList(IStorage storage)
			: base(storage)
		{
		}

		#endregion

		public virtual IEnumerable<PollVote> ReadAllByPoll(Poll poll)
		{
			throw new NotSupportedException();
		}
	}

	[Serializable]
	class ChoicePollVoteList : PollVoteList
	{
		#region ChoicePollVoteList.ctor()

		public ChoicePollVoteList(IStorage storage, PollChoice choice)
			: base(storage)
		{
			AddFilter("Choice", choice);
		}

		#endregion
	}

	class UserPollVoteList : PollVoteList
	{
		#region Private Fields

		private readonly ForumUser _user;

		#endregion

		#region UserPollVoteList.ctor()

		public UserPollVoteList(IStorage storage, ForumUser user)
			: base(storage)
		{
			_user = user;
			AddFilter(user);
		}

		#endregion

		public override IEnumerable<PollVote> ReadAllByPoll(Poll poll)
		{
			return this.Where(vote => vote.Poll == poll);
		}
	}
}