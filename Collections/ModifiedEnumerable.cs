namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	public class ModifiedEnumerable<T> : IEnumerable<T>
	{
		private sealed class Enumerator : BaseEnumerator<IEnumerable<T>, T>
		{
			private int _index;

			public Enumerator(IEnumerable<T> parent)
				: base(parent)
			{
			}

			protected override T ProcessMove(ref bool canProcess)
			{
				++_index;

				if (_index < Source.Count())
				{
					return Source.ElementAt(_index);
				}
				else
				{
					canProcess = false;
					return default(T);
				}
			}

			public override void Reset()
			{
				base.Reset();
				_index = -1;
			}
		}

		private readonly IEnumerable<T> _source;

		public ModifiedEnumerable(IEnumerable<T> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			_source = source;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(_source);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}