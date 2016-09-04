namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	public class AuditList : BaseEntityList<Audit>
	{
		public AuditList(IStorage storage)
			: base(storage)
		{
		}
	}

	class BaseEntityAuditList : AuditList
	{
		public BaseEntityAuditList(IStorage storage, BaseEntity entity)
			: base(storage)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			AddFilter("EntityId", entity.Id);
		}
	}
}