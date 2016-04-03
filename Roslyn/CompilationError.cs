namespace Ecng.Roslyn
{
	using System;

	using Microsoft.CodeAnalysis;

	public enum CompilationErrorTypes
	{
		Info,
		Warning,
		Error,
	}

	public class CompilationError
	{
		internal CompilationError(Diagnostic diagnostic)
		{
			Id = diagnostic.Id;

			switch (diagnostic.Severity)
			{
				case DiagnosticSeverity.Hidden:
				case DiagnosticSeverity.Info:
					Type = CompilationErrorTypes.Info;
					break;
				case DiagnosticSeverity.Warning:
					Type = CompilationErrorTypes.Warning;
					break;
				case DiagnosticSeverity.Error:
					Type = CompilationErrorTypes.Error;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var pos = diagnostic.Location.GetLineSpan().StartLinePosition;

			Line = pos.Line;
			Character = pos.Character;
			Message = diagnostic.GetMessage();
		}

		public string Id { get; private set; }
		public int Line { get; private set; }
		public int Character { get; private set; }
		public string Message { get; private set; }
		public CompilationErrorTypes Type { get; private set; }
	}
}