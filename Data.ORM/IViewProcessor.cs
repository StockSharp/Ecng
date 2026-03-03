namespace Ecng.Serialization;

using System.Collections;

/// <summary>
/// Non-generic interface for processing view entities with CRUD operations.
/// </summary>
public interface IViewProcessor
{
	/// <summary>
	/// Gets the underlying table entity type.
	/// </summary>
	Type TableType { get; }

	/// <summary>
	/// Gets the view entity type.
	/// </summary>
	Type ViewType { get; }

	/// <summary>
	/// Reads a range of entities by their identifiers.
	/// </summary>
	/// <param name="ids">Array of entity identifiers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Enumerable of matching entities.</returns>
	ValueTask<IEnumerable> ReadRange(object[] ids, CancellationToken cancellationToken);
}

/// <summary>
/// Generic interface for processing view entities with typed CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The view entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public interface IViewProcessor<TEntity, TId> : IViewProcessor
{
	/// <summary>
	/// Gets the queryable source for the view entities.
	/// </summary>
	IQueryable<TEntity> Select { get; }

	/// <summary>
	/// Reads a range of entities by their typed identifiers.
	/// </summary>
	/// <param name="ids">Array of typed entity identifiers.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Array of matching entities.</returns>
	ValueTask<TEntity[]> ReadRange(TId[] ids, CancellationToken cancellationToken);

	/// <summary>
	/// Creates a new view entity.
	/// </summary>
	/// <param name="view">The entity to create.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The created entity.</returns>
	ValueTask<TEntity> Create(TEntity view, CancellationToken cancellationToken);

	/// <summary>
	/// Reads a single entity by its identifier.
	/// </summary>
	/// <param name="id">The entity identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The matching entity.</returns>
	ValueTask<TEntity> ReadById(TId id, CancellationToken cancellationToken);

	/// <summary>
	/// Updates an existing view entity.
	/// </summary>
	/// <param name="view">The entity to update.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated entity.</returns>
	ValueTask<TEntity> Update(TEntity view, CancellationToken cancellationToken);

	/// <summary>
	/// Deletes a view entity.
	/// </summary>
	/// <param name="view">The entity to delete.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the entity was deleted; otherwise, <see langword="false"/>.</returns>
	ValueTask<bool> Delete(TEntity view, CancellationToken cancellationToken);
}
