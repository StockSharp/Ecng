namespace Ecng.Xaml
{
	using System;

	public abstract class BaseObservableCollection
	{
		private int _maxCount = -1;

		public int MaxCount
		{
			get { return _maxCount; }
			set
			{
				if (value < -1 || value == 0)
					throw new ArgumentOutOfRangeException();

				_maxCount = value;
			}
		}

		public abstract int Count { get; }

		public abstract int RemoveRange(int index, int count);

		protected void CheckCount()
		{
			if (MaxCount == -1)
				return;

			var diff = (int)(Count - 1.5 * MaxCount);

			if (diff <= 0)
				return;

			RemoveRange(0, diff);
		}
	}
}