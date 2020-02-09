namespace Ecng.Logic.BusinessEntities
{
	public static class Extensions
	{
		public static long DefaultId = -1;

		public static bool IsNotSaved<TEntity>(this TEntity entity)
			where TEntity : BaseEntity
		{
			return entity.Id == DefaultId;
		}
	}
}