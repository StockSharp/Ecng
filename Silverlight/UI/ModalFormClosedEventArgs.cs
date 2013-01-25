namespace Ecng.UI
{
	using System;

	public class ModalFormClosedEventArgs : EventArgs
	{
		public ModalFormClosedEventArgs(bool isOk)
		{
			this.IsOk = isOk;
		}

		public bool IsOk { get; private set; }
	}
}