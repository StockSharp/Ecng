namespace Ecng.Common
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	[DataContract]
	public class NullableEx<T> : Equatable<NullableEx<T>>
	{
		#region HasValue

		public bool HasValue { get; private set; }

		#endregion

		#region Value

		private T _value;

		[DataMember]
		public T Value
		{
			get
			{
				if (HasValue)
					return _value;
				else
					throw new InvalidOperationException();
			}
			set
			{
				_value = value;
				HasValue = true;
			}
		}

		#endregion

		#region Equatable<NullableEx<T>> Members

		protected override bool OnEquals(NullableEx<T> other)
		{
			if (HasValue != other.HasValue)
				return false;

			return !HasValue || Value.Equals(other.Value);
		}

		public override NullableEx<T> Clone()
		{
			var retVal = new NullableEx<T>();

			if (HasValue)
				retVal.Value = Value;

			return retVal;
		}

		#endregion

		public override int GetHashCode()
		{
			return HasValue ? Value.GetHashCode() : typeof(T).GetHashCode();
		}
	}
}