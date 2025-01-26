namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;

public interface ICustomTypeDescriptorProvider
{
	bool TryGet(Type type, object instance, out ICustomTypeDescriptor descriptor);
}