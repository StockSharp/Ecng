namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;

/// <summary>
/// Provides a mechanism to retrieve a custom type descriptor for a given type and instance.
/// </summary>
public interface ICustomTypeDescriptorProvider
{
	/// <summary>
	/// Attempts to retrieve the custom type descriptor associated with the specified type and instance.
	/// </summary>
	/// <param name="type">The type for which the custom type descriptor is requested.</param>
	/// <param name="instance">The instance associated with the specified type.</param>
	/// <param name="descriptor">
	/// When this method returns, contains the custom type descriptor if found; otherwise, null.
	/// </param>
	/// <returns>
	/// True if the custom type descriptor was successfully retrieved; otherwise, false.
	/// </returns>
	bool TryGet(Type type, object instance, out ICustomTypeDescriptor descriptor);
}