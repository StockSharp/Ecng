namespace Ecng.Compilation;

using System;
using System.Reflection;

public interface ICompilerContext : IDisposable
{
	Assembly LoadFromBinary(byte[] body);
}