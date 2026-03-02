namespace Ecng.Serialization;

public static class ViewProcessorRegistry
{
	private static readonly SynchronizedDictionary<Type, IViewProcessor> _cache = [];

	public static IViewProcessor GetProcessor(Type viewType)
		=> _cache.SafeAdd(viewType,
			key =>
				key
					.GetAttribute<ViewProcessorAttribute>()
					.ProcessorType
					.CreateInstance<IViewProcessor>());

	public static IViewProcessor<TEntity, TId> GetProcessor<TEntity, TId>()
		=> (IViewProcessor<TEntity, TId>)GetProcessor(typeof(TEntity));
}
