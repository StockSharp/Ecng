namespace Ecng.Compilation;

using Ecng.Common;

/// <summary>
/// Represents a compilation error with details about its location and type.
/// </summary>
public class CompilationError : Equatable<CompilationError>
{
	/// <summary>
	/// Gets or sets the identifier of the compilation error.
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Gets or sets the line number where the error occurred.
	/// </summary>
	public int Line { get; set; }

	/// <summary>
	/// Gets or sets the character position where the error occurred.
	/// </summary>
	public int Character { get; set; }

	/// <summary>
	/// Gets or sets the message that describes the error.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// Gets or sets the type of the compilation error.
	/// </summary>
	public CompilationErrorTypes Type { get; set; }

	/// <inheritdoc />
	public override string ToString()
		=> $"{Type} ({Id}) {Message} ({Line}-{Character})";

	/// <inheritdoc />
	public override CompilationError Clone()
		=> new()
		{
			Id = Id,
			Character = Character,
			Type = Type,
			Message = Message,
			Line = Line
		};

	/// <inheritdoc />
	protected override bool OnEquals(CompilationError other)
		=>
		Id == other.Id &&
		Line == other.Line &&
		Character == other.Character &&
		Message == other.Message &&
		Type == other.Type;

	/// <inheritdoc />
	public override int GetHashCode()
		=> (Id?.GetHashCode() ?? 0) ^ Line.GetHashCode() ^ Character.GetHashCode() ^ (Message?.GetHashCode() ?? 0) ^ Type.GetHashCode();
}