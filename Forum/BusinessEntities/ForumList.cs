namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class ForumList : ForumBaseNamedEntityList<Forum>
	{
		public ForumList(IStorage storage)
			: base(storage)
		{
		}

		#region News

		private static Forum _news;

		public Forum News => _news ?? (_news = ReadById((long)Identities.ForumNews));

		#endregion

		#region ShutdownErrors

		private static Forum _shutdownErrors;

		public Forum ShutdownErrors => _shutdownErrors ?? (_shutdownErrors = ReadById((long)Identities.ForumShutdownErrors));

		#endregion

		#region SecurityErrors

		private static Forum _securityErrors;

		public Forum SecurityErrors => _securityErrors ?? (_securityErrors = ReadById((long)Identities.ForumSecurityErrors));

		#endregion

		#region LogicErrors

		private static Forum _logicErrors;

		public Forum LogicErrors => _logicErrors ?? (_logicErrors = ReadById((long)Identities.ForumLogicErrors));

		#endregion

		#region ClientErrors

		private static Forum _clientErrors;

		public Forum ClientErrors => _clientErrors ?? (_clientErrors = ReadById((long)Identities.ForumClientErrors));

		#endregion

		#region ServerErrors

		private static Forum _serverErrors;

		public Forum ServerErrors => _serverErrors ?? (_serverErrors = ReadById((long)Identities.ForumServerErrors));

		#endregion

		#region Trash

		private static Forum _trash;

		public Forum Trash => _trash ?? (_trash = ReadById((long)Identities.ForumTrash));

		#endregion

		#region PrivateMessages

		private static Forum _privateMessages;

		public Forum PrivateMessages => _privateMessages ?? (_privateMessages = ReadById((long)Identities.ForumPrivateMessages));

		#endregion

		#region JournalEntries

		private static Forum _journalEntries;

		public Forum JournalEntries => _journalEntries ?? (_journalEntries = ReadById((long)Identities.ForumJournalEntries));

		#endregion

		#region Faq

		private static Forum _faq;

		public Forum Faq => _faq ?? (_faq = ReadById((long)Identities.ForumFaq));

		#endregion

		#region Rules

		private static Forum _rules;

		public Forum Rules => _rules ?? (_rules = ReadById((long)Identities.ForumRules));

		#endregion

		#region Market

		private static Forum _market;

		public Forum Market => _market ?? (_market = ReadById((long)Identities.ForumMarket));

		#endregion

		#region Blogs

		private static Forum _blogs;

		public Forum Blogs => _blogs ?? (_blogs = ReadById((long)Identities.ForumBlogs));

		#endregion
	}

	[Serializable]
	class FolderForumList : ForumList
	{
		#region FolderForumList.ctor()

		public FolderForumList(IStorage storage, ForumFolder folder)
			: base(storage)
		{
			AddFilter("Folder", folder);
		}

		#endregion
	}
}