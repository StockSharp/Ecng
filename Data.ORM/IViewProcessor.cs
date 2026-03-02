namespace Ecng.Serialization;

using System.Collections;

public interface IViewProcessor
{
	Type TableType { get; }
	Type ViewType { get; }

	ValueTask<IEnumerable> ReadRange(object[] ids, CancellationToken cancellationToken);
}

public interface IViewProcessor<TEntity, TId> : IViewProcessor
{
	IQueryable<TEntity> Select { get; }

	ValueTask<TEntity[]> ReadRange(TId[] ids, CancellationToken cancellationToken);

	ValueTask<TEntity> Create(TEntity view, CancellationToken cancellationToken);
	ValueTask<TEntity> ReadById(TId id, CancellationToken cancellationToken);
	ValueTask<TEntity> Update(TEntity view, CancellationToken cancellationToken);
	ValueTask<bool> Delete(TEntity view, CancellationToken cancellationToken);
}
