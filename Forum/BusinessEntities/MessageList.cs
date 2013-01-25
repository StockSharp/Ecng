namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class MessageList : ForumBaseEntityList<Message>
	{
		public MessageList(IStorage storage)
			: base(storage)
		{
		}

		public MessageList Search(string query, DateTime beginDate, DateTime endDate)
		{
			return new SearchMessageList(Database, query, beginDate, endDate);
		}
	}

	class SearchMessageList : MessageList
	{
		public SearchMessageList(IStorage storage, string query, DateTime beginDate, DateTime endDate)
			: base(storage)
		{
			AddFilter(
				new Tuple<string, object>("Query", query),
				new Tuple<string, object>("BeginDate", beginDate),
				new Tuple<string, object>("EndDate", endDate));
		}
	}

	[Serializable]
	class TopicMessageList : MessageList
	{
		private readonly Topic _topic;

		public TopicMessageList(IStorage storage, Topic topic)
			: base(storage)
		{
			_topic = topic;
			AddFilter(topic);
		}

		public override int IndexOf(Message item)
		{
			return ExecuteScalar<int>("IndexOf", new SerializationItemCollection
			{
				new SerializationItem(new VoidField<long>("Topic"), _topic.Id),
				new SerializationItem(new VoidField<long>("Message"), item.Id)
			});
		}
	}

	[Serializable]
	class ForumMessageList : MessageList
	{
		public ForumMessageList(IStorage storage, Forum forum)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Forum"), forum);
		}
	}

	[Serializable]
	class UserMessageList : MessageList
	{
		public UserMessageList(IStorage storage, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}

	//class NewPrivateUserMessageList : UserMessageList
	//{
	//    #region NewPrivateUserMessageList.ctor()

	//    public NewPrivateUserMessageList(IStorage storage, User user)
	//        : base(database, user)
	//    {
	//    }

	//    #endregion
	//}

	[Serializable]
	class ChildMessageList : MessageList
	{
		public ChildMessageList(IStorage storage, Message parent)
			: base(storage)
		{
			AddFilter("Parent", parent);
		}
	}

	//class IncomingMessageList : UserMessageList
	//{
	//    #region IncomingMessageList.ctor()

	//    public IncomingMessageList(IStorage storage, User user)
	//        : base(database, user)
	//    {
	//    }

	//    #endregion
	//}

	//class OutgoingMessageList : UserMessageList
	//{
	//    #region OutgoingMessageList.ctor()

	//    public OutgoingMessageList(IStorage storage, User user)
	//        : base(database, user)
	//    {
	//    }

	//    #endregion
	//}

	class ForumFolderMessageList : MessageList
	{
		#region ForumFolderMessageList.ctor()

		public ForumFolderMessageList(IStorage storage, ForumFolder folder)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Folder"), folder);
		}

		#endregion
	}
}