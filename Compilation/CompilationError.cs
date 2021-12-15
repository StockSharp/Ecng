namespace Ecng.Compilation
{
	public class CompilationError
	{
		public string Id { get; set; }
		public int Line { get; set; }
		public int Character { get; set; }
		public string Message { get; set; }
		public CompilationErrorTypes Type { get; set; }
	}
}