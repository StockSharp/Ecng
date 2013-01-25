namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Common;

	public abstract class SimpleEnumerator<T> : Disposable, IEnumerator<T>
	{
		public abstract bool MoveNext();

		public virtual void Reset()
		{
		}

		public T Current { get; protected set; }

		object IEnumerator.Current
		{
			get { return Current; }
		}
	}
}