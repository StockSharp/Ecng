namespace Ecng.Forum.BusinessEntities
{
	using Ecng.Logic.BusinessEntities;

	public class ForumRootObject : LogicRootObject<ForumUser, ForumRole>
	{
		public ForumRootObject(string name, LogicDatabase<ForumUser, ForumRole> database)
			: base(name, database)
		{
		}

		public SecurityEntryList SecurityEntries { get; private set; }
		public ForumFolderList ForumFolders { get; private set; }
		public ForumList Forums { get; private set; }
		public GradeList Grades { get; private set; }
		public ForumUserList OnlineUsers { get; private set; }
		public PollList Polls { get; private set; }
		public BannedEntryList BannedEntries { get; private set; }
		public BannedEntryList ActiveBannedEntries { get; private set; }
		public FileList Files { get; private set; }
		public FileList SpellPictures { get; private set; }
		public ViewList Views { get; private set; }
		public MessageList Messages { get; private set; }
		public TopicList Topics { get; private set; }
		public ModerationVoteList ModerationVotes { get; private set; }
		public MessageTaglineList Taglines { get; private set; }
		public TopicTagList Tags { get; private set; }
		public VisitList Visits { get; private set; }
		public ForumUserList Users { get; private set; }
		public ForumRoleList Roles { get; private set; }

		protected override BaseUserList<ForumUser, ForumRole> GetUsers()
		{
			return Users;
		}

		public override void Initialize()
		{
			base.Initialize();

			SecurityEntries = new SecurityEntryList(Database) { BulkLoad = true };
			ForumFolders = new ForumFolderList(Database) { BulkLoad = true };
			Forums = new ForumList(Database) { BulkLoad = true };
			Grades = new GradeList(Database);
			OnlineUsers = new OnlineUserList(Database);
			Polls = new PollList(Database);
			BannedEntries = new BannedEntryList(Database);
			ActiveBannedEntries = new ActiveBannedEntryList(Database);
			Files = new FileList(Database);
			SpellPictures = new SpellPictureFileList(Database) { BulkLoad = true };
			Views = new ViewList(Database);
			Messages = new MessageList(Database);
			Topics = new TopicList(Database);
			Taglines = new MessageTaglineList(Database);
			Tags = new TopicTagList(Database);
			ModerationVotes = new ModerationVoteList(Database);
			Visits = new VisitList(Database);
			Roles = new ForumRoleList(Database) { BulkLoad = true };
			Users = new ForumUserList(Database) { BulkLoad = true };
		}
	}
}
