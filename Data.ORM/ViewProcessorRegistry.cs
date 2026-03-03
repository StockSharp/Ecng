namespace Ecng.Serialization;

/// <summary>
/// Provides cached access to <see cref="IViewProcessor"/> instances by view entity type.
/// </summary>
public static class ViewProcessorRegistry
{
	private static readonly SynchronizedDictionary<Type, IViewProcessor> _cache = [];

	/// <summary>
	/// Gets or creates a cached <see cref="IViewProcessor"/> for the specified view type.
	/// </summary>
	/// <param name="viewType">The view entity type.</param>
	/// <returns>The view processor instance.</returns>
	public static IViewProcessor GetProcessor(Type viewType)
		=> _cache.SafeAdd(viewType,
			key =>
				key
					.GetAttribute<ViewProcessorAttribute>()
					.ProcessorType
					.CreateInstance<IViewProcessor>());

	/// <summary>
	/// Gets or creates a cached typed <see cref="IViewProcessor{TEntity, TId}"/> for the specified entity and id types.
	/// </summary>
	/// <typeparam name="TEntity">The view entity type.</typeparam>
	/// <typeparam name="TId">The entity identifier type.</typeparam>
	/// <returns>The typed view processor instance.</returns>
	public static IViewProcessor<TEntity, TId> GetProcessor<TEntity, TId>()
		=> (IViewProcessor<TEntity, TId>)GetProcessor(typeof(TEntity));
}
