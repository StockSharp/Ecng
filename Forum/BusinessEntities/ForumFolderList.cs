namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class ForumFolderList : ForumBaseNamedEntityList<ForumFolder>
	{
		public ForumFolderList(IStorage storage)
			: base(storage)
		{
		}

		#region Root

		private static ForumFolder _root;

		public ForumFolder Root
		{
			get { return _root ?? (_root = ReadById((long)Identities.ForumFolderRoot)); }
		}

		#endregion

		#region PrivateAdmin

		private static ForumFolder _privateAdmin;

		public ForumFolder PrivateAdmin
		{
			get { return _privateAdmin ?? (_privateAdmin = ReadById((long)Identities.ForumFolderPrivateAdmin)); }
		}

		#endregion

		#region PrivateModerator

		private static ForumFolder _privateModerator;

		public ForumFolder PrivateModerator
		{
			get { return _privateModerator ?? (_privateModerator = ReadById((long)Identities.ForumFolderPrivateModerator)); }
		}

		#endregion

		#region PrivateEditor

		private static ForumFolder _privateEditor;

		public ForumFolder PrivateEditor
		{
			get { return _privateEditor ?? (_privateEditor = ReadById((long)Identities.ForumFolderPrivateEditor)); }
		}

		#endregion

		#region Common

		private static ForumFolder _common;

		public ForumFolder Common
		{
			get { return _common ?? (_common = ReadById((long)Identities.ForumFolderCommon)); }
		}

		#endregion

		#region News

		private static ForumFolder _news;

		public ForumFolder News
		{
			get { return _news ?? (_news = ReadById((long)Identities.ForumFolderNews)); }
		}

		#endregion

		#region Articles

		private static ForumFolder _articles;

		public ForumFolder Articles
		{
			get { return _articles ?? (_articles = ReadById((long)Identities.ForumFolderArticles)); }
		}

		#endregion
	}

	[Serializable]
	class ChildForumFolderList : ForumFolderList
	{
		public ChildForumFolderList(IStorage storage, ForumFolder parent)
			: base(storage)
		{
			AddFilter("Parent", parent);
		}
	}
}