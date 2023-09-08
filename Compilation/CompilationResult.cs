namespace Ecng.Compilation
{
	using System.Collections.Generic;

	public class CompilationResult
	{
		public byte[] Assembly { get; set; }

		public IEnumerable<CompilationError> Errors { get; set; }
	}
}