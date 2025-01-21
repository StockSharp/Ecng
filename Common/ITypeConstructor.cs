namespace Ecng.Common;

public interface ITypeConstructor
{
	object CreateInstance(object[] args);
}