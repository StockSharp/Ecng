namespace Ecng.UI
{
	public class RatingEventArgs : CancelEventArgs
	{
		public RatingEventArgs(int newValue, int oldValue)
		{
			this.NewValue = newValue;
			this.OldValue = oldValue;
		}

		public int NewValue { get; private set; }
		public int OldValue { get; private set; }
	}
}