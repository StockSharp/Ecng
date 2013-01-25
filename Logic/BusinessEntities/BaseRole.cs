namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	[Serializable]
	//[Audit((byte)AuditSchemas.Role)]
	[QueryStringId("rid")]
	public abstract class BaseRole<TUser, TRole> : BaseEntity<TUser, TRole>
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		protected BaseRole()
		{
			Description = string.Empty;
		}

		[String(1, 512)]
		[Validation]
		[Index]
		[AuditField((byte)AuditFields.Name)]
		public string Name { get; set; }

		[String(2048)]
		[Validation]
		[AuditField((byte)AuditFields.Description)]
		public string Description { get; set; }

		public override string ToString()
		{
			return Name;
		}

		//[PageLoad(ListType = typeof(RoleUserList))]
		//public abstract TUserList Users { get; protected set; }
	}
}