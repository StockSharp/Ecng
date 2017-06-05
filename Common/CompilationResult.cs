namespace Ecng.Common
{
	using System.Collections.Generic;
	using System.Reflection;

	public class CompilationResult
	{
		public Assembly Assembly { get; set; }

		public string AssemblyLocation { get; set; }

		public IEnumerable<CompilationError> Errors { get; set; }
	}
}