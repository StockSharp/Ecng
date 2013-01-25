namespace Ecng.Common
{
	using System.Threading;

	public class IdGenerator
	{
		private long _current;

		public long Current
		{
			get { return _current; }
			set { _current = value; }
		}

		public long Next
		{
			get { return Interlocked.Increment(ref _current); }
		}
	}
}