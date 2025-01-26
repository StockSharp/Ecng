namespace Ecng.ComponentModel;

using System.ComponentModel;

public interface ICustomTypeDescriptorEx : ICustomTypeDescriptor
{
	object Instance { get; }
}