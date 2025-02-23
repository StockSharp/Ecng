namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

	/// <summary>
	/// Defines a method that creates a new object that is a deep copy of the current instance.
	/// </summary>
	/// <typeparam name="T">The type of the object that is cloned.</typeparam>
	public interface ICloneable<T> : ICloneable
	{
		/// <summary>
		/// Creates a new object that is a deep copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a deep copy of this instance.</returns>
		new T Clone();
	}
}