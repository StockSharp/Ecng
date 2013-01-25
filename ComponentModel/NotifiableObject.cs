namespace Ecng.ComponentModel
{
	using System.ComponentModel;

	using Ecng.Common;

	public abstract class NotifiableObject : INotifyPropertyChangedEx, INotifyPropertyChanging
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>
		/// Raise event <see cref="INotifyPropertyChanged.PropertyChanged"/>.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		public void NotifyPropertyChanged(string propertyName)
		{
			PropertyChanged.SafeInvoke(this, propertyName);
		}

		protected void NotifyChanged(string propertyName)
		{
			NotifyPropertyChangedExHelper.Notify(this, propertyName);
		}

		protected void NotifyChanging(string propertyName)
		{
			PropertyChanging.SafeInvoke(this, propertyName);
		}
	}
}