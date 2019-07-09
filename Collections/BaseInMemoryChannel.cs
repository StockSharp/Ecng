namespace Ecng.Collections
{
	using System;
	using System.Globalization;

	using Ecng.Common;

	public abstract class BaseInMemoryChannel<TItem> : IDisposable
	{
		private readonly IBlockingQueue<TItem> _queue;
		private readonly Action<Exception> _errorHandler;

		protected BaseInMemoryChannel(IBlockingQueue<TItem> queue, string name, Action<Exception> errorHandler)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			Name = name;

			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
			
			_queue.Close();
		}

		/// <summary>
		/// Handler name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Queue count.
		/// </summary>
		public int Count => _queue.Count;

		/// <summary>
		/// Max queue count.
		/// </summary>
		/// <remarks>
		/// The default value is -1, which corresponds to the size without limitations.
		/// </remarks>
		public int MaxCount
		{
			get => _queue.MaxSize;
			set => _queue.MaxSize = value;
		}

		///// <summary>
		///// Channel closing event.
		///// </summary>
		//public event Action Closed;

		/// <summary>
		/// Is channel opened.
		/// </summary>
		public bool IsOpened => !_queue.IsClosed;

		/// <summary>
		/// <see cref="IsOpened"/> change event.
		/// </summary>
		public event Action StateChanged;

		/// <summary>
		/// Open channel.
		/// </summary>
		public void Open()
		{
			_queue.Open();
			StateChanged?.Invoke();

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					while (!_queue.IsClosed)
					{
						try
						{
							if (!_queue.TryDequeue(out var item))
							{
								break;
							}

							OnNewOut(item);
						}
						catch (Exception ex)
						{
							_errorHandler(ex);
						}
					}

					//Closed?.Invoke();
					StateChanged?.Invoke();
				}))
				.Name($"{Name} channel thread.")
				//.Culture(CultureInfo.InvariantCulture)
				.Launch();
		}

		/// <summary>
		/// Process output item.
		/// </summary>
		/// <param name="item">Item.</param>
		protected abstract void OnNewOut(TItem item);

		/// <summary>
		/// Close channel.
		/// </summary>
		public void Close()
		{
			_queue.Close();
		}

		/// <summary>
		/// Send item.
		/// </summary>
		/// <param name="item">Item.</param>
		public void SendIn(TItem item)
		{
			if (!IsOpened)
				throw new InvalidOperationException();

			_queue.Enqueue(item);
		}

		///// <summary>
		///// New item event.
		///// </summary>
		//public event Action<TItem> NewOutItem;

		void IDisposable.Dispose()
		{
			Close();
		}
	}
}