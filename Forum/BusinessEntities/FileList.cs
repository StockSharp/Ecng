namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class FileList : ForumBaseNamedEntityList<File>
	{
		public FileList(IStorage storage)
			: base(storage)
		{
		}
	}

	[Serializable]
	class UserFileList : FileList
	{
		public UserFileList(IStorage storage, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}

	class SpellPictureFileList : FileList
	{
		public SpellPictureFileList(IStorage storage)
			: base(storage)
		{
		}
	}

	class MessageFileList : FileList
	{
		public MessageFileList(IStorage storage, Message message)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Message"), message);
			OverrideCreateDelete = true;
		}
	}
}