namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Data;

	public class AuditList : BaseEntityList<Audit>
	{
		public AuditList(Database database)
			: base(database)
		{
		}
	}

	class BaseEntityAuditList : AuditList
	{
		public BaseEntityAuditList(Database database, BaseEntity entity)
			: base(database)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			base.InitializeFilter("EntityId", entity.Id);
		}
	}
}