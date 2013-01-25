namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.Web.UI.WebControls;

	public interface IStorage
	{
		long GetCount<TEntity>();

		TEntity Add<TEntity>(TEntity entity);

		TEntity GetBy<TEntity>(SerializationItemCollection by);

		TEntity GetById<TEntity>(object id);

		IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, SortDirection direction);

		TEntity Update<TEntity>(TEntity entity);

		void Remove<TEntity>(TEntity entity);

		void Clear<TEntity>();

		void ClearCache();

		BatchContext BeginBatch();
		void CommitBatch();
		void EndBatch();
	}
}