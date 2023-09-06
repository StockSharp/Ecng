namespace Ecng.Compilation
{
	using System.Collections.Generic;

	public class CompilationResult
	{
		public byte[] Assembly { get; set; }

		public string AssemblyLocation { get; set; }

		public IEnumerable<CompilationError> Errors { get; set; }
	}
}