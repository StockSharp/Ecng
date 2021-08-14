namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	public abstract class BaseEnumerator<TEnumerable, TItem> : SimpleEnumerator<TItem>
		where TEnumerable : IEnumerable<TItem>
	{
		protected BaseEnumerator(TEnumerable source)
		{
			if (source.IsNull())
				throw new ArgumentException(nameof(source));

			Source = source;
			Reset();
		}

		public TEnumerable Source { get; private set; }

		protected override void DisposeManaged()
		{
			Reset();
			Source = default;
		}

		public override bool MoveNext()
		{
			ThrowIfDisposed();

			var canProcess = true;
			Current = ProcessMove(ref canProcess);
			return canProcess;
		}

		public override void Reset()
		{
			Current = default;
		}

		protected abstract TItem ProcessMove(ref bool canProcess);
	}
}