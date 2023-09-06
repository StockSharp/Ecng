namespace Ecng.Compilation;

using Ecng.Common;

public class CompilationError : Equatable<CompilationError>
{
	public string Id { get; set; }
	public int Line { get; set; }
	public int Character { get; set; }
	public string Message { get; set; }
	public CompilationErrorTypes Type { get; set; }

	public override string ToString()
		=> $"{Type} ({Id}) {Message} ({Line}-{Character})";

	public override CompilationError Clone()
		=> new()
		{
			Id = Id,
			Character = Character,
			Type = Type,
			Message = Message,
			Line = Line
		};

	protected override bool OnEquals(CompilationError other)
		=>
		Id == other.Id &&
		Line == other.Line &&
		Character == other.Character &&
		Message == other.Message &&
		Type == other.Type;

	public override int GetHashCode()
		=> (Id?.GetHashCode() ?? 0) ^ Line.GetHashCode() ^ Character.GetHashCode() ^ (Message?.GetHashCode() ?? 0) ^ Type.GetHashCode();
}