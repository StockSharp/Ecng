namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	public class ModerationVoteList : ForumBaseEntityList<ModerationVote>
	{
		public ModerationVoteList(IStorage storage)
			: base(storage)
		{
		}
	}

	[Serializable]
	class UserModerationVoteList : ModerationVoteList
	{
		public UserModerationVoteList(IStorage storage, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}

	[Serializable]
	class ForumModerationVoteList : ModerationVoteList
	{
		public ForumModerationVoteList(IStorage storage, Forum forum)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Forum"), forum);
		}
	}

	[Serializable]
	class TopicModerationVoteList : ModerationVoteList
	{
		public TopicModerationVoteList(IStorage storage, Topic topic)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Topic"), topic);
		}
	}

	[Serializable]
	class MessageModerationVoteList : ModerationVoteList
	{
		public MessageModerationVoteList(IStorage storage, Message message)
			: base(storage)
		{
			AddFilter(message);
		}
	}
}