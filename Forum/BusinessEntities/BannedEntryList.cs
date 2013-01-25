namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class BannedEntryList : ForumBaseEntityList<BannedEntry>
	{
		public BannedEntryList(IStorage storage)
			: base(storage)
		{
		}
	}

	[Serializable]
	class ActiveBannedEntryList : BannedEntryList
	{
		public ActiveBannedEntryList(IStorage storage)
			: base(storage)
		{
		}
	}

	[Serializable]
	class UserBannedEntryList : BannedEntryList
	{
		public UserBannedEntryList(IStorage storage, ForumUser bannedUser)
			: base(storage)
		{
			AddFilter("BannedUser", bannedUser);
		}
	}

	[Serializable]
	class UserActiveBannedEntryList : UserBannedEntryList
	{
		public UserActiveBannedEntryList(IStorage storage, ForumUser bannedUser)
			: base(storage, bannedUser)
		{
		}
	}
}