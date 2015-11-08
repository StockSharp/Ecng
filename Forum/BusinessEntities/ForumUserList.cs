namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Web;

	#endregion

	[Serializable]
	public class ForumUserList : BaseUserList<ForumUser, ForumRole>, IWebUserCollection
	{
		private readonly static Field _emailField = SchemaManager.GetSchema<ForumUser>().Fields["Email"];

		public ForumUserList(IStorage storage)
			: base(storage, 0)
		{
		}

		private static ForumUser _gallery;

		public ForumUser Gallery => _gallery ?? (_gallery = ReadById((long)Identities.UserGallery));

		private static ForumUser _image;

		public ForumUser Image => _image ?? (_image = ReadById((long)Identities.UserImage));

		public override ForumUser ReadByName(string userName)
		{
			return ReadByEmail(userName);
		}

		public ForumUser ReadByEmail([Length(128)]string email)
		{
			return Read(_emailField, email);
		}

		public IEnumerable<ForumUser> ReadAllByEmail([Length(128)]string emailMatch, long startIndex, long count)
		{
			if (emailMatch.IsEmpty())
				throw new ArgumentNullException(nameof(emailMatch));

			return ReadAll("Email", string.Empty, startIndex, count, new SerializationItemCollection { new SerializationItem(new VoidField<string>("EmailMatch"), emailMatch) });
		}

		#region IWebUserCollection

		IEnumerator<IWebUser> IEnumerable<IWebUser>.GetEnumerator()
		{
			return this.Cast<IWebUser>().GetEnumerator();
		}

		void ICollection<IWebUser>.Add(IWebUser user)
		{
			Add((ForumUser)user);
		}

		bool ICollection<IWebUser>.Contains(IWebUser user)
		{
			return Contains((ForumUser)user);
		}

		void ICollection<IWebUser>.CopyTo(IWebUser[] array, int arrayIndex)
		{
			CopyTo((ForumUser[])array, arrayIndex);
		}

		bool ICollection<IWebUser>.Remove(IWebUser user)
		{
			return Remove((ForumUser)user);
		}

		IWebUser IWebUserCollection.GetByName(string userName)
		{
			return ReadByName(userName);
		}

		IWebUser IWebUserCollection.GetByEmail(string email)
		{
			return ReadByEmail(email);
		}

		IWebUser IWebUserCollection.GetByKey(object key)
		{
			return ReadById(key);
		}

		#endregion
	}

	[Serializable]
	class OnlineUserList : ForumUserList
	{
		public OnlineUserList(IStorage storage)
			: base(storage)
		{
		}
	}

	[Serializable]
	class RoleUserList : ForumUserList
	{
		public RoleUserList(IStorage storage, ForumRole role)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Role"), role);
		}
	}

	class PollUserList : ForumUserList
	{
		#region PollUserList.ctor()

		public PollUserList(IStorage storage, Poll poll)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Poll"), poll);
		}

		#endregion
	}

	class PollChoiceUserList : ForumUserList
	{
		#region PollChoiceUserList.ctor()

		public PollChoiceUserList(IStorage storage, PollChoice choice)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Choice"), choice);
		}

		#endregion
	}
}