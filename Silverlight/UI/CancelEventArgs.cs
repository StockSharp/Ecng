namespace Ecng.UI
{
	using System;

	public class CancelEventArgs : EventArgs
	{
		public CancelEventArgs()
		{
			this.IsCancel = true;
		}

		public bool IsCancel { get; set; }
	}
}