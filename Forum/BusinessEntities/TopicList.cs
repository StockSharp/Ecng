﻿namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;
	using Ecng.Logic.BusinessEntities;

	[Serializable]
	public class TopicList : ForumBaseNamedEntityList<Topic>
	{
		public TopicList(IStorage storage)
			: base(storage)
		{
		}

		private static Topic _chat;

		public Topic Chat
		{
			get { return _chat ?? (_chat = ReadById((long)Identities.TopicChat)); }
		}

		private static Topic _offtopic;

		public Topic Offtopic
		{
			get { return _offtopic ?? (_offtopic = ReadById((long)Identities.TopicOfftopic)); }
		}

		private static Topic _introduction;

		public Topic Introduction
		{
			get { return _introduction ?? (_introduction = ReadById((long)Identities.TopicIntroduction)); }
		}

		//private static Topic _guide;

		//public Topic Guide
		//{
		//    get
		//    {
		//        if (_guide == null)
		//            _guide = ReadById((long)Identities.TopicGuide];

		//        return _guide;
		//    }
		//}

		private static Topic _spells;

		public Topic Spells
		{
			get { return _spells ?? (_spells = ReadById((long)Identities.TopicSpells)); }
		}

		private static Topic _items;

		public Topic Items
		{
			get { return _items ?? (_items = ReadById((long)Identities.TopicItems)); }
		}

		private static Topic _worlds;

		public Topic Worlds
		{
			get { return _worlds ?? (_worlds = ReadById((long)Identities.TopicWorlds)); }
		}

		private static Topic _about;

		public Topic About
		{
			get { return _about ?? (_about = ReadById((long)Identities.TopicAbout)); }
		}

		private static Topic _systemRequirements;

		public Topic SystemRequirements
		{
			get { return _systemRequirements ?? (_systemRequirements = ReadById((long)Identities.TopicSystemRequirements)); }
		}

		private static Topic _forums;

		public Topic Forums
		{
			get { return _forums ?? (_forums = ReadById((long)Identities.TopicForums)); }
		}

		private static Topic _blogs;

		public Topic Blogs
		{
			get { return _blogs ?? (_blogs = ReadById((long)Identities.TopicBlogs)); }
		}

		private static Topic _polls;

		public Topic Polls
		{
			get { return _polls ?? (_polls = ReadById((long)Identities.TopicPolls)); }
		}

		private static Topic _codex;

		public Topic Codex
		{
			get { return _codex ?? (_codex = ReadById((long)Identities.TopicCodex)); }
		}

		private static Topic _whoIsWho;

		public Topic WhoIsWho
		{
			get { return _whoIsWho ?? (_whoIsWho = ReadById((long)Identities.TopicWhoIsWho)); }
		}

		private static Topic _profile;

		public Topic Profile
		{
			get { return _profile ?? (_profile = ReadById((long)Identities.TopicProfile)); }
		}

		private static Topic _races;

		public Topic Races
		{
			get { return _races ?? (_races = ReadById((long)Identities.TopicRaces)); }
		}

		private static Topic _classes;

		public Topic Classes
		{
			get { return _classes ?? (_classes = ReadById((long)Identities.TopicClasses)); }
		}

		private static Topic _inventory;

		public Topic Inventory
		{
			get { return _inventory ?? (_inventory = ReadById((long)Identities.TopicInventory)); }
		}

		private static Topic _spellbook;

		public Topic SpellBook
		{
			get { return _spellbook ?? (_spellbook = ReadById((long)Identities.TopicSpellBook)); }
		}

		private static Topic _quests;

		public Topic Quests
		{
			get { return _quests ?? (_quests = ReadById((long)Identities.TopicQuests)); }
		}

		private static Topic _money;

		public Topic Money
		{
			get { return _money ?? (_money = ReadById((long)Identities.TopicMoney)); }
		}

		private static Topic _interface;

		public Topic Interface
		{
			get { return _interface ?? (_interface = ReadById((long)Identities.TopicInterface)); }
		}

		private static Topic _journal;

		public Topic Journal
		{
			get { return _journal ?? (_journal = ReadById((long)Identities.TopicJournal)); }
		}

		public void AddPrivateTopic(Topic topic, ForumUser user)
		{
			ExecuteNonQuery("AddPrivateTopic", new SerializationItemCollection
			{
				new SerializationItem(new VoidField<long>("Topic"), topic.Id),
				new SerializationItem(new VoidField<long>("User"), user.Id),
				new SerializationItem(new VoidField<bool>("Deleted"), false),
			});
		}

		public virtual TopicList ByUser(ForumUser user)
		{
			throw new NotSupportedException();
		}
	}

	[Serializable]
	class UserTopicList : TopicList
	{
		public UserTopicList(IStorage storage, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}

	[Serializable]
	class ForumTopicList : TopicList
	{
		private readonly Forum _forum;

		public ForumTopicList(IStorage storage, Forum forum)
			: base(storage)
		{
			AddFilter(forum);
			_forum = forum;
		}

		public override TopicList ByUser(ForumUser user)
		{
			return new ForumUserTopicList(Database, _forum, user);
		}
	}

	[Serializable]
	class ForumUserTopicList : TopicList
	{
		public ForumUserTopicList(IStorage storage, Forum forum, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}

	[Serializable]
	class TagTopicList : TopicList
	{
		public TagTopicList(IStorage storage, TopicTag tag)
			: base(storage)
		{
			AddFilter(new VoidField<long>("TopicTag"), tag);
		}
	}

	class ArticleUserTopicList : UserTopicList
	{
		#region ArticleUserTopicList.ctor()

		public ArticleUserTopicList(IStorage storage, ForumUser user)
			: base(storage, user)
		{
		}

		#endregion
	}

	class UserPrivateTopicList : UserTopicList
	{
		#region UserPrivateTopicList.ctor()

		public UserPrivateTopicList(IStorage storage, ForumUser user)
			: base(storage, user)
		{
		}

		#endregion

		public override bool Contains(Topic item)
		{
			return ExecuteScalar<bool>("UserPrivateTopicOwn", item, LogicHelper<ForumUser, ForumRole>.CurrentUser);
		}
	}

	class ForumFolderTopicList : TopicList
	{
		#region ForumFolderTopicList.ctor()

		public ForumFolderTopicList(IStorage storage, ForumFolder folder)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Folder"), folder);
		}

		#endregion
	}

	//class BlogTopicList : TopicList
	//{
	//    #region BlogTopicList.ctor()

	//    public BlogTopicList()
	//    {
	//        InitializeFilter();
	//    }

	//    #endregion
	//}
}