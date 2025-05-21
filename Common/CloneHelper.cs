namespace Ecng.Common;

using System;

/// <summary>
/// Provides helper methods for performing cloning operations on objects that implement the ICloneable interface.
/// </summary>
public static class CloneHelper
{
	/// <summary>
	/// Creates a clone of the specified object.
	/// </summary>
	/// <typeparam name="T">A type that implements ICloneable.</typeparam>
	/// <param name="value">The object to clone.</param>
	/// <returns>A cloned instance of the object.</returns>
	public static T TypedClone<T>(this T value)
		where T : ICloneable
	{
		return (T)value.Clone();
	}

	/// <summary>
	/// Represents a marker helper class indicating that UI-related parts should be excluded during cloning.
	/// </summary>
	[Obsolete]
	public class CloneWithoutUI {}
}
