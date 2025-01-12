namespace Ecng.Compilation;

using System.Collections.Generic;

public interface IAssembly
{
	byte[] AsBytes { get; }
	IEnumerable<IType> GetExportTypes(object context);
}