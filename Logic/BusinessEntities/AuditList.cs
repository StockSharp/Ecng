namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	public class AuditList<TUser, TRole> : BaseEntityList<Audit<TUser, TRole>, TUser, TRole>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		public AuditList(IStorage storage)
			: base(storage)
		{
		}
	}

	class BaseEntityAuditList<TUser, TRole> : AuditList<TUser, TRole>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		public BaseEntityAuditList(IStorage storage, BaseEntity<TUser, TRole> entity)
			: base(storage)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			AddFilter("EntityId", entity.Id);
		}
	}
}