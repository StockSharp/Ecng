namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	[Flags]
	[Serializable]
	public enum PermissionTypes
	{
		Create = 1,
		Read = Create << 1,
		Update = Read << 1,
		Delete = Update << 1,
		DeleteAll = Delete << 1,
		List = DeleteAll << 1,
		FullControl = PermissionTypes.Create | PermissionTypes.Read | PermissionTypes.Update | PermissionTypes.Delete | PermissionTypes.DeleteAll | PermissionTypes.List,
	}

	[Serializable]
	[Audit((byte)AuditSchemas.SecurityEntry)]
	public class SecurityEntry : ForumBaseEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.Role)]
		public ForumRole Role { get; set; }

		[AuditField((byte)AuditFields.Permissions)]
		public PermissionTypes Permissions { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.ForumFolder)]
		public ForumFolder ForumFolder { get; set; }
	}
}