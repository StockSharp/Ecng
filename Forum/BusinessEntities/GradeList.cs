namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class GradeList : ForumBaseEntityList<Grade>
	{
		public GradeList(IStorage storage)
			: base(storage)
		{
		}

		public virtual int TotalValue
		{
			get { return ExecuteScalar<int>("TotalValue", new SerializationItemCollection()); }
		}
	}

	class MessageGradeList : GradeList
	{
		private readonly Message _message;

		public MessageGradeList(IStorage storage, Message message)
			: base(storage)
		{
			_message = message;
			AddFilter(message);
		}

		public override int TotalValue
		{
			get { return ExecuteScalar<int>("MessageTotalValue", _message); }
		}
	}

	abstract class UserGradeList : GradeList
	{
		private readonly ForumUser _user;
		private readonly string _morph;

		protected UserGradeList(IStorage storage, ForumUser user, string morph)
			: base(storage)
		{
			_user = user;
			_morph = morph;
		}

		public override int TotalValue
		{
			get { return ExecuteScalar<int>(_morph, _user); }
		}
	}

	[Serializable]
	class ReceivedUserGradeList : UserGradeList
	{
		public ReceivedUserGradeList(IStorage storage, ForumUser user)
			: base(storage, user, "ReceivedUserTotalValue")
		{
			AddFilter(user);
		}
	}

	[Serializable]
	class SettedUserGradeList : UserGradeList
	{
		public SettedUserGradeList(IStorage storage, ForumUser user)
			: base(storage, user, "SettedUserTotalValue")
		{
			AddFilter(user);
		}
	}

	[Serializable]
	class TopicGradeList : GradeList
	{
		private readonly Topic _topic;

		public TopicGradeList(IStorage storage, Topic topic)
			: base(storage)
		{
			_topic = topic;
			AddFilter(new VoidField<long>("Topic"), topic);
		}

		public override int TotalValue
		{
			get { return ExecuteScalar<int>("TopicTotalValue", _topic); }
		}
	}

	[Serializable]
	class ForumGradeList : GradeList
	{
		private readonly Forum _forum;

		public ForumGradeList(IStorage storage, Forum forum)
			: base(storage)
		{
			_forum = forum;
			AddFilter(new VoidField<long>("Forum"), forum);
		}

		public override int TotalValue
		{
			get { return ExecuteScalar<int>("ForumTotalValue", _forum); }
		}
	}

	[Serializable]
	class ForumFolderGradeList : GradeList
	{
		private readonly ForumFolder _folder;

		public ForumFolderGradeList(IStorage storage, ForumFolder folder)
			: base(storage)
		{
			_folder = folder;
			AddFilter(new VoidField<long>("Folder"), folder);
		}

		public override int TotalValue
		{
			get { return ExecuteScalar<int>("ForumFolderTotalValue", _folder); }
		}
	}
}