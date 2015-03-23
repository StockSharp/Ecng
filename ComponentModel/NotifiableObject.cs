namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	[Serializable]
	[DataContract]
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
			this.Notify(propertyName);
		}

		protected void NotifyChanging(string propertyName)
		{
			PropertyChanging.SafeInvoke(this, propertyName);
		}
	}
}