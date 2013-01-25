namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Linq;

	using Ecng.Serialization;

	[Serializable]
	public class SecurityEntryList : ForumBaseEntityList<SecurityEntry>
	{
		public SecurityEntryList(IStorage storage)
			: base(storage)
		{
		}

		public SecurityEntry ReadByRole(ForumRole role)
		{
			return this.FirstOrDefault(entry => entry.Role == role);
		}
	}

	class RoleSecurityEntryList : SecurityEntryList
	{
		#region RoleSecurityEntryList.ctor()

		public RoleSecurityEntryList(IStorage storage, ForumRole role)
			: base(storage)
		{
			AddFilter(role);
		}

		#endregion
	}

	class ForumFolderSecurityEntryList : SecurityEntryList
	{
		#region ForumFolderSecurityEntryList.ctor()

		public ForumFolderSecurityEntryList(IStorage storage, ForumFolder folder)
			: base(storage)
		{
			AddFilter(folder);
		}

		#endregion
	}
}