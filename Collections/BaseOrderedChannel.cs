namespace Ecng.Collections;

using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Runtime.CompilerServices;

using Ecng.Common;

/// <summary>
/// Represents a base class for an ordered blocking queue that sorts values based on a specified key.
/// </summary>
/// <typeparam name="TSort">The type used to determine the sort order of values.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the queue.</typeparam>
/// <typeparam name="TCollection">The type of the inner collection, which must implement <see cref="ICollection{T}"/> and <see cref="IQueue{T}"/> for tuples of <typeparamref name="TSort"/> and <typeparamref name="TValue"/>.</typeparam>
public abstract class BaseOrderedChannel<TSort, TValue, TCollection>
	where TCollection : ICollection<(TSort, TValue)>, IQueue<(TSort sort, TValue value)>
{
	private readonly Lock _sync = new();
	private readonly TCollection _collection;
	private Channel<(TSort sort, TValue value)> _channel;
	private bool _isClosed;

	/// <inheritdoc />
	public int Count
	{
		get
		{
			using (_sync.EnterScope())
				return _collection.Count;
		}
	}

	/// <summary>
	/// Gets the maximum number of values that the queue can hold.
	/// </summary>
	public int MaxSize { get; }

	/// <summary>
	/// Initializes a new instance with a specified maximum size.
	/// </summary>
	/// <param name="collection">Inner collection.</param>
	/// <param name="maxSize">Maximum queue size. Use -1 for unbounded.</param>
	protected BaseOrderedChannel(TCollection collection, int maxSize)
	{
		_collection = collection ?? throw new ArgumentNullException(nameof(collection));

		if (maxSize == 0 || maxSize < -1)
			throw new ArgumentOutOfRangeException(nameof(maxSize));

		MaxSize = maxSize;
	}

	/// <summary>
	/// Gets a value indicating whether the queue is closed.
	/// </summary>
	public bool IsClosed
	{
		get
		{
			using (_sync.EnterScope())
				return _isClosed;
		}
	}

	/// <summary>
	/// Opens the queue, enabling enqueuing and dequeuing operations.
	/// </summary>
	public void Open()
	{
		using (_sync.EnterScope())
		{
			if (_channel != null && !_isClosed)
				return;

			_isClosed = false;

			if (MaxSize > 0)
			{
				var options = new BoundedChannelOptions(MaxSize)
				{
					SingleReader = true,
					SingleWriter = false,
					FullMode = BoundedChannelFullMode.Wait
				};

				_channel = Channel.CreateBounded<(TSort, TValue)>(options);
			}
			else
			{
				var options = new UnboundedChannelOptions
				{
					SingleReader = true,
					SingleWriter = false
				};

				_channel = Channel.CreateUnbounded<(TSort, TValue)>(options);
			}

			// When the queue is (re-)opened we start with a clean sorted buffer.
			_collection.Clear();
		}
	}

	/// <summary>
	/// Closes the queue to prevent further enqueuing operations.
	/// </summary>
	public void Close()
	{
		Channel<(TSort, TValue)> channel;

		using (_sync.EnterScope())
		{
			if (_isClosed)
				return;

			_isClosed = true;
			channel = _channel;
		}

		// Completing the writer will eventually unblock any pending ReadAsync.
		channel?.Writer.TryComplete();
	}

	/// <summary>
	/// Adds the specified item with associated sort to the queue.
	/// </summary>
	/// <param name="sort">The sort with which to associate the new value.</param>
	/// <param name="value">The value to add to the queue.</param>
	protected void Enqueue(TSort sort, TValue value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		Channel<(TSort, TValue)> channel;
		bool closed;

		using (_sync.EnterScope())
		{
			channel = _channel;
			closed = _isClosed;
		}

		if (closed || channel is null)
			return;

		try
		{
			if (!channel.Writer.TryWrite((sort, value)))
				AsyncHelper.Run(() => channel.Writer.WriteAsync((sort, value)));
		}
		catch (ChannelClosedException)
		{
			// Queue was closed concurrently; value is dropped by design.
		}
	}

	/// <summary>
	/// Removes and returns the next value from the queue, waiting if necessary until a value becomes available.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The next value from the queue.</returns>
	public async ValueTask<TValue> DequeueAsync(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			Channel<(TSort, TValue)> channel;

			using (_sync.EnterScope())
			{
				channel = _channel;

				if (channel is not null)
				{
					var reader = channel.Reader;

					while (reader.TryRead(out var item))
						_collection.Enqueue(item);
				}

				if (_collection.TryDequeue(out var t))
					return t.value;
			}

			if (channel is null)
			{
				await Task.Delay(1, cancellationToken).NoWait();
				continue;
			}

			try
			{
				var (sort, value) = await channel.Reader.ReadAsync(cancellationToken).NoWait();

				using (_sync.EnterScope())
				{
					_collection.Enqueue((sort, value));
				}
			}
			catch (ChannelClosedException)
			{
				// Check if there are any remaining values in the sorted queue
				using (_sync.EnterScope())
				{
					if (_collection.TryDequeue(out var t))
						return t.value;
				}

				throw;
			}
		}
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<TValue> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			TValue value;

			try
			{
				value = await DequeueAsync(cancellationToken).NoWait();
			}
			catch (ChannelClosedException)
			{
				yield break;
			}
			catch
			{
				if (cancellationToken.IsCancellationRequested)
					yield break;

				throw;
			}

			yield return value;
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_collection.Clear();

			var channel = _channel;

			if (channel != null)
			{
				while (channel.Reader.TryRead(out _))
				{
					// Just draining the channel
				}
			}
		}
	}
}