namespace Ecng.ComponentModel;

using System.ComponentModel;

/// <summary>
/// Provides an extended type descriptor that includes access to the underlying instance.
/// </summary>
public interface ICustomTypeDescriptorEx : ICustomTypeDescriptor
{
	/// <summary>
	/// Gets the actual instance that this type descriptor represents.
	/// </summary>
	object Instance { get; }
}