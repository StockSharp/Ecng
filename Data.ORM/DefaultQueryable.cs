namespace Ecng.Serialization;

using System.Collections;

public class DefaultQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
	public DefaultQueryable(IQueryProvider provider, Expression expression)
	{
		if (expression != null && !typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
			throw new ArgumentException($"Not assignable from {expression.Type}.", nameof(expression));

		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_expression = expression ?? Expression.Constant(this);
	}

	Type IQueryable.ElementType => typeof(T);

	private readonly Expression _expression;
	Expression IQueryable.Expression => _expression;

	private IQueryProvider _provider;
	IQueryProvider IQueryable.Provider => _provider;

	internal void ReplaceProvider(IQueryProvider provider)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_expression.ReplaceSource(provider);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _provider.Execute<IEnumerable>(_expression).GetEnumerator();

	IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
		=> _provider.Execute<IAsyncEnumerable<T>>(_expression).GetAsyncEnumerator(cancellationToken);
}