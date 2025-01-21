namespace Ecng.Compilation;

using System.Reflection;

public interface ICompilerContext
{
	Assembly LoadFromBinary(byte[] body);
}