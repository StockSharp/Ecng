namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Linq;

	using Ecng.Serialization;

	[Serializable]
	public class ViewList : ForumBaseEntityList<View>
	{
		public ViewList(IStorage storage)
			: base(storage)
		{
		}

		public virtual View ReadLastView(ForumUser user)
		{
			return Read("User", "LastView", user);
		}
	}

	class TopicViewList : ViewList
	{
		//private readonly Topic _topic;

		#region TopicViewList.ctor()

		public TopicViewList(IStorage storage, Topic topic)
			: base(storage)
		{
			AddFilter(topic);
			//_topic = topic;
		}

		#endregion

		public override View ReadLastView(ForumUser user)
		{
			return this.FirstOrDefault(view => view.User == user);
		}
	}

	class ForumViewList : ViewList
	{
		private readonly Forum _forum;

		#region ForumViewList.ctor()

		public ForumViewList(IStorage storage, Forum forum)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Forum"), forum);
			_forum = forum;
		}

		#endregion

		public override View ReadLastView(ForumUser user)
		{
			return Read("UserAndForum", "LastView", user, _forum);
		}
	}
}