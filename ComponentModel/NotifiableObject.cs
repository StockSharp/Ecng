namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Runtime.CompilerServices;

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
		public void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
		{
			PropertyChanged?.Invoke(this, propertyName);
		}

		protected void NotifyChanged([CallerMemberName]string propertyName = null)
		{
			this.Notify(propertyName);
		}

		protected void NotifyChanging([CallerMemberName]string propertyName = null)
		{
			PropertyChanging?.Invoke(this, propertyName);
		}
	}
}