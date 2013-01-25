namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.Web;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	[Audit((byte)AuditSchemas.Role)]
	[Entity("Role")]
	public class ForumRole : BaseRole<ForumUser, ForumRole>, IWebRole
	{
		[RelationMany(typeof(RoleUserList))]
		public ForumUserList Users { get; protected set; }

		[RelationMany(typeof(RoleSecurityEntryList), BulkLoad = true)]
		public SecurityEntryList Entries { get; protected set; }

		IWebUserCollection IWebRole.Users
		{
			get { return Users; }
		}
	}
}