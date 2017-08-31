namespace Ecng.Common
{
	using System;
	using System.Collections.Concurrent;

	public class ObjectPool<T>
		where T : new()
	{
		private int _approxCount;
		private readonly ConcurrentBag<T> _pool = new ConcurrentBag<T>();

		public int Count => _pool.Count;

		public const int DefaultMaxCount = 1000;

		private int _maxCount = DefaultMaxCount;

		public int MaxCount
		{
			get => _maxCount;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value));

				_maxCount = value;
			}
		}

		public T Get()
		{

			if (_pool.TryTake(out T result))
			{
				_approxCount--;
				return result;
			}

			result = new T();
			//_pool.Add(result);
			return result;
		}

		public void Put(T obj)
		{
			if (_approxCount > _maxCount)
				throw new InvalidOperationException();

			_pool.Add(obj);
			_approxCount++;
		}
	}
}