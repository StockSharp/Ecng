namespace Ecng.Serialization;

interface IRelationManyList<TEntity> : IAsyncCollection<TEntity>
{
	IStorage Storage { get; }

	ValueTask<IQueryable<TEntity>> TryInitBulkLoad(CancellationToken cancellationToken);
}