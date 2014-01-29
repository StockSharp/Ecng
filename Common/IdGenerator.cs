namespace Ecng.Common
{
	using System;
	using System.Threading;

	/// <summary>
	/// Базовый генератор идентификаторов.
	/// </summary>
	public abstract class IdGenerator
	{
		/// <summary>
		/// Инициализировать <see cref="IdGenerator"/>.
		/// </summary>
		protected IdGenerator()
		{
		}

		/// <summary>
		/// Получить следующий идентификатор.
		/// </summary>
		/// <returns>Следующий идентификатор.</returns>
		public abstract long GetNextId();
	}

	/// <summary>
	/// Генератор идентификаторов, основанный на автоматическом увеличении идентификатора на 1.
	/// </summary>
	public class IncrementalIdGenerator : IdGenerator
	{
		/// <summary>
		/// Создать <see cref="IncrementalIdGenerator"/>.
		/// </summary>
		public IncrementalIdGenerator()
		{
		}

		private long _current;

		/// <summary>
		/// Текущий идентификатор.
		/// </summary>
		public long Current
		{
			get { return _current; }
			set { _current = value; }
		}

		/// <summary>
		/// Получить следующий идентификатор.
		/// </summary>
		/// <returns>Следующий идентификатор.</returns>
		public override long GetNextId()
		{
			return Interlocked.Increment(ref _current);
		}
	}

	/// <summary>
	/// Генератор идентификаторов, основанный на автоматическом увеличении идентификатора на 1.
	/// Первоначальное значение равно количество миллисекунд, прошедшее с начала дня.
	/// </summary>
	public class MillisecondIncrementalIdGenerator : IncrementalIdGenerator
	{
		/// <summary>
		/// Создать <see cref="MillisecondIncrementalIdGenerator"/>.
		/// </summary>
		public MillisecondIncrementalIdGenerator()
		{
			Current = (long)(DateTime.Now - DateTime.Today).TotalMilliseconds;
		}
	}

	/// <summary>
	/// Генератор идентификаторов, основанный на миллисекундах. Каждый следующий вызов метода <see cref="GetNextId"/>
	/// будет возвращать количество миллисекунд, прошедшее с начала создания генератора.
	/// </summary>
	public class MillisecondIdGenerator : IdGenerator
	{
		private readonly DateTime _start;

		/// <summary>
		/// Создать <see cref="MillisecondIdGenerator"/>.
		/// </summary>
		public MillisecondIdGenerator()
		{
			_start = DateTime.Now;
		}

		/// <summary>
		/// Получить следующий идентификатор.
		/// </summary>
		/// <returns>Следующий идентификатор.</returns>
		public override long GetNextId()
		{
			return (long)(DateTime.Now - _start).TotalMilliseconds;
		}
	}
}