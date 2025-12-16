namespace Ecng.Linq;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Wrapper that converts <see cref="IEnumerable{T}"/> to <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements.</typeparam>
/// <param name="source">The source enumerable.</param>
public class SyncAsyncEnumerable<T>(IEnumerable<T> source) : IAsyncEnumerable<T>
{
	private readonly IEnumerable<T> _source = source ?? throw new ArgumentNullException(nameof(source));

	IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
		=> new Enumerator(_source.GetEnumerator(), cancellationToken);

	private class Enumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken) : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
		private readonly CancellationToken _cancellationToken = cancellationToken;

		T IAsyncEnumerator<T>.Current => _enumerator.Current;

		ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
		{
			_cancellationToken.ThrowIfCancellationRequested();
			return new ValueTask<bool>(_enumerator.MoveNext());
		}

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			_enumerator.Dispose();
			return default;
		}
	}
}