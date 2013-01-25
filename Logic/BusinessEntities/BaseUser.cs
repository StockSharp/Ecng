namespace Ecng.Logic.BusinessEntities
{
	//using Ecng.Data;

	//[Audit((byte)AuditSchemas.User)]
	[QueryStringId("uid")]
	public abstract class BaseUser<TUser, TRole> : BaseEntity<TUser, TRole>
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		//[PageLoad(ListType = typeof(UserRoleList), BulkLoad = true)]
		//public abstract TRoleList Roles { get; protected set; }
	}
}