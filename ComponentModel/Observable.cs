namespace Ecng.ComponentModel
{
	public class Observable<T> : NotifiableObject
	{
		private T _value;

		public T Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyChanged("Value");
			}
		}
	}
}