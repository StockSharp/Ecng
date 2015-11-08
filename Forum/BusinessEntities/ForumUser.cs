namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.ComponentModel;
	using Ecng.Security;
	using Ecng.Serialization;
	using Ecng.Web;
	using Ecng.Logic.BusinessEntities;

	#endregion

	[Serializable]
	public enum MailFormats
	{
		Html,
		Plain,
	}

	[Serializable]
	[Index(FieldName = "Email")]
	[Audit((byte)AuditSchemas.User)]
	[Entity("User")]
	public class ForumUser : BaseUser<ForumUser, ForumRole>, IWebUser
	{
		public ForumUser()
		{
			PageSize = HttpHelper.DefaultPageSize;
			MailFormat = MailFormats.Html;
			LastLoginDate =
			LastActivityDate =
			LastPasswordChangedDate =
			LastPasswordQuestionAndAnswerChangedDate =
			LastLockOutDate
				= CreationDate;
		}

		//#region FirstName

		//[String(128)]
		//[Validation/*(IsNullable = true)*/]
		//[AuditField((byte)AuditFields.FirstName)]
		//public string FirstName { get; set; }

		//#endregion

		//#region LastName

		//[String(128)]
		//[Validation/*(IsNullable = true)*/]
		//[AuditField((byte)AuditFields.LastName)]
		//public string LastName { get; set; }

		//#endregion

		#region Email

		[String(128)]
		[Validation]
		[AuditField((byte)AuditFields.Email)]
		public string Email { get; set; }

		#endregion

		#region HomePage

		[String(1024)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.HomePage)]
		public string HomePage { get; set; }

		#endregion

		#region TimeZone

		[String(256)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.TimeZone)]
		public string TimeZone { get; set; }

		#endregion

		#region Location

		[String(1024)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Location)]
		public string Location { get; set; }

		#endregion

		#region Occupation

		[String(2048)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Occupation)]
		public string Occupation { get; set; }

		#endregion

		#region EnableNewsletter

		[AuditField((byte)AuditFields.EnableNewsletter)]
		public bool EnableNewsletter { get; set; }

		#endregion

		#region EnableNotification

		[AuditField((byte)AuditFields.EnableNotification)]
		public bool EnableNotification { get; set; }

		#endregion

		#region Signature

		[String(2048)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Signature)]
		public string Signature { get; set; }

		#endregion

		#region PageSize

		[AuditField((byte)AuditFields.PageSize)]
		[Range(MinValue = 0, MaxValue = 50)]
		public int PageSize { get; set; }

		#endregion

		#region MailFormat

		[AuditField((byte)AuditFields.MailFormat)]
		public MailFormats MailFormat { get; set; }

		#endregion

		#region Files

		[RelationMany(typeof(UserFileList), BulkLoad = true)]
		public FileList Files { get; protected set; }

		#endregion

		#region Polls

		[RelationMany(typeof(UserPollList))]
		public PollList Polls { get; protected set; }

		#endregion

		#region Votes

		[RelationMany(typeof(UserPollVoteList))]
		public PollVoteList Votes { get; protected set; }

		#endregion

		#region Topics

		[RelationMany(typeof(UserTopicList))]
		public TopicList Topics { get; protected set; }

		#endregion

		#region Messages

		[RelationMany(typeof(UserMessageList))]
		public MessageList Messages { get; protected set; }

		#endregion

		#region Articles

		[RelationMany(typeof(ArticleUserTopicList))]
		public TopicList Articles { get; protected set; }

		#endregion

		#region BannedEntries

		[RelationMany(typeof(UserBannedEntryList))]
		public BannedEntryList BannedEntries { get; protected set; }

		#endregion

		#region ActiveBannedEntries

		[RelationMany(typeof(UserActiveBannedEntryList))]
		public BannedEntryList ActiveBannedEntries { get; protected set; }

		#endregion

		#region Password

		[InnerSchema]
		[NameOverride("Hash", "Password")]
		[NameOverride("Salt", "PasswordSalt")]
		[AuditField((byte)AuditFields.Password, FieldName = "Hash")]
		[AuditField((byte)AuditFields.PasswordSalt, FieldName = "Salt")]
		public Secret Password { get; set; }

		#endregion

		//#region PasswordQuestion

		//[String(1024)]
		//[Validation]
		//[AuditField((byte)AuditFields.PasswordQuestion)]
		//public string PasswordQuestion { get; set; }

		//#endregion

		//#region PasswordAnswer

		//[InnerSchema]
		//[NameOverride("Hash", "PasswordAnswer")]
		//[NameOverride("Salt", "PasswordAnswerSalt")]
		//[AuditField((byte)AuditFields.PasswordAnswer, FieldName = "Hash")]
		//[AuditField((byte)AuditFields.PasswordAnswerSalt, FieldName = "Salt")]
		//public Password PasswordAnswer { get; set; }

		//#endregion

		#region LastLoginDate

		[AuditField((byte)AuditFields.LastLoginDate)]
		public DateTime LastLoginDate { get; set; }

		#endregion

		#region LastActivityDate

		[AuditField((byte)AuditFields.LastActivityDate)]
		public DateTime LastActivityDate { get; set; }

		#endregion

		#region LastPasswordChangedDate

		[AuditField((byte)AuditFields.LastPasswordChangedDate)]
		public DateTime LastPasswordChangedDate { get; set; }

		#endregion

		#region LastPasswordQuestionAndAnswerChangedDate

		[AuditField((byte)AuditFields.LastPasswordQuestionAndAnswerChangedDate)]
		public DateTime LastPasswordQuestionAndAnswerChangedDate { get; set; }

		#endregion

		#region LastLockOutDate

		[AuditField((byte)AuditFields.LastLockOutDate)]
		public DateTime LastLockOutDate { get; set; }

		#endregion

		#region IsApproved

		[AuditField((byte)AuditFields.IsApproved)]
		public bool IsApproved { get; set; }

		#endregion

		#region IsLockedOut

		[AuditField((byte)AuditFields.IsLockedOut)]
		public bool IsLockedOut { get; set; }

		#endregion

		#region HideActivity

		[AuditField((byte)AuditFields.HideActivity)]
		public bool HideActivity { get; set; }

		#endregion

		//#region IncomingMessages

		//[PageLoad(ListType = typeof(IncomingMessageList))]
		//public MessageList IncomingMessages { get; protected set; }

		//#endregion

		//#region OutgoingMessages

		//[PageLoad(ListType = typeof(OutgoingMessageList))]
		//public MessageList OutgoingMessages { get; protected set; }

		//#endregion

		#region PrivateTopics

		[RelationMany(typeof(UserPrivateTopicList))]
		public TopicList PrivateTopics { get; protected set; }

		#endregion

		//#region NewPrivateMessages

		//[PageLoad(ListType = typeof(NewPrivateUserMessageList))]
		//public MessageList NewPrivateMessages { get; protected set; }

		//#endregion

		#region Icq

		[String(128)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Icq)]
		public string Icq { get; set; }

		#endregion

		#region Msn

		[String(128)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Msn)]
		public string Msn { get; set; }

		#endregion

		#region Yim

		[String(128)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Yim)]
		public string Yim { get; set; }

		#endregion

		#region Aim

		[String(128)]
		[Validation/*(IsNullable = true)*/]
		[AuditField((byte)AuditFields.Aim)]
		public string Aim { get; set; }

		#endregion

		#region Roles

		[RelationMany(typeof(UserRoleList), BulkLoad = true)]
		public ForumRoleList Roles { get; protected set; }

		#endregion

		#region ReceivedGrades

		[RelationMany(typeof(ReceivedUserGradeList))]
		public GradeList ReceivedGrades { get; protected set; }

		#endregion

		#region SettedGrades

		[RelationMany(typeof(SettedUserGradeList))]
		public GradeList SettedGrades { get; protected set; }

		#endregion

		#region Visits

		[RelationMany(typeof(UserVisitList))]
		public VisitList Visits { get; protected set; }

		#endregion

		#region IWebUser Members

		[Ignore]
		object IWebUser.Key => Id;

		[Ignore]
		string IWebUser.Name => Email;

		[Ignore]
		string IWebUser.Description => string.Empty;

		[Ignore]
		string IWebUser.PasswordQuestion
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		[Ignore]
		Secret IWebUser.PasswordAnswer
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		[Ignore]
		bool IWebUser.IsLockedOut
		{
			get { return IsLockedOut || Deleted; }
			set { IsLockedOut = value; }
		}

		IWebRoleCollection IWebUser.Roles => Roles;

		#endregion
	}
}