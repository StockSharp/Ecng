namespace Ecng.Common
{
	#region Using Directives

	using System.Collections;
	using System.Collections.Generic;

	#endregion

	public abstract class Enumerable<T> : IEnumerable<T>
	{
		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		protected abstract IEnumerator<T> GetEnumerator();
	}
}