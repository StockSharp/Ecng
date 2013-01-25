namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

	[Serializable]
	public abstract class Cloneable<T> : ICloneable<T>
		//where T : Cloneable<T>
	{
		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public abstract T Clone();

		#region ICloneable Members

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}
}