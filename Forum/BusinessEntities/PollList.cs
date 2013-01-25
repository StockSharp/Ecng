namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Serialization;

	[Serializable]
	public class PollList : ForumBaseEntityList<Poll>
	{
		private readonly static Dictionary<Topic, Poll> _polls = new Dictionary<Topic, Poll>();

		public PollList(IStorage storage)
			: base(storage)
		{
		}

		public virtual Poll ReadByTopic(Topic topic)
		{
			return _polls.SafeAdd(topic, key => ReadBy(key));
		}
	}

	[Serializable]
	class UserPollList : PollList
	{
		public UserPollList(IStorage storage, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}
}