namespace Ecng.Compilation;

using System;
using System.IO;

public class FileReference : BaseFileReference
{
	public virtual string GetFileBody()
	{
		if (!IsValid)
			throw new InvalidOperationException();

		return File.ReadAllText(FileName);
	}
}