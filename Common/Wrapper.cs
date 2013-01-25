namespace Ecng.Common
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	#endregion

	/// <summary>
	/// This class implement patters "Wrapper".
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public abstract class Wrapper<T> : Equatable<Wrapper<T>>, IDisposable
	{
		#region Wrapper.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="Wrapper{T}"/> class.
		/// </summary>
		protected Wrapper()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Wrapper{T}"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		protected Wrapper(T value)
		{
			Value = value;
		}

		#endregion

		#region Value

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		public virtual T Value { get; set; }

		#endregion

		#region HasValue

		/// <summary>
		/// Gets a value indicating whether this instance has value.
		/// </summary>
		/// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
		public bool HasValue
		{
			get { return !object.ReferenceEquals(Value, default(T)); }
		}

		#endregion

		/// <summary>
		/// Explicit operators the specified wrapper.
		/// </summary>
		/// <param name="wrapper">The wrapper.</param>
		/// <returns></returns>
		public static explicit operator T(Wrapper<T> wrapper)
		{
			return wrapper.Value;
		}

		#region Equatable<Wrapper<T>> Members

		/// <summary>
		/// Called when [equals].
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		protected override bool OnEquals(Wrapper<T> other)
		{
			if (Value is IEnumerable<T>)
				return ((IEnumerable<T>)Value).SequenceEqual((IEnumerable<T>)other.Value);
			else
				return Value.Equals(other.Value);
		}

		#endregion

		#region Object Members

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			int hash = 0;

			if (HasValue)
			{
				if (Value is ICollection)
					throw new NotImplementedException();
				else
					hash = Value.GetHashCode();
			}

			return hash;
		}

		public bool IsDisposed { get; private set; }

		public void Dispose()
		{
			lock (this)
			{
				if (!IsDisposed)
				{
					DisposeManaged();
					DisposeNative();
					IsDisposed = true;
					GC.SuppressFinalize(this);
				}
			}
		}

		protected virtual void DisposeManaged()
		{
			Value.DoDispose();
		}

		protected virtual void DisposeNative()
		{
		}

		~Wrapper()
		{
            DisposeNative();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			if (HasValue)
				return Value.ToString();
			else
				return string.Empty;
		}

		#endregion

		//#region Serializable Members

		///// <summary>
		///// Serializes the specified source.
		///// </summary>
		///// <param name="serializer"></param>
		///// <param name="source">Serialized state.</param>
		//protected override void Serialize(Serializer<Wrapper<T>> serializer, SerializationItemCollection source)
		//{
		//    source.Add(new SerializationItem("Value", Serializer<T>.Default.Serialize(Value)));
		//}

		///// <summary>
		///// Deserializes the specified data.
		///// </summary>
		///// <param name="serializer"></param>
		///// <param name="source">Serialized state.</param>
		//protected override void Deserialize(Serializer<Wrapper<T>> serializer, SerializationItemCollection source)
		//{
		//    Value = Serializer<T>.Default.Deserialize((byte[])source["Value"].Value);
		//}

		//#endregion
	}
}