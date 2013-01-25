namespace Ecng.Forum.BusinessEntities
{
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	public abstract class ForumBaseEntityList<TEntity> : BaseEntityList<TEntity, ForumUser, ForumRole>
		where TEntity : ForumBaseEntity
	{
		protected ForumBaseEntityList(IStorage storage)
			: base(storage)
		{
		}
	}
}