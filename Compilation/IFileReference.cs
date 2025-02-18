namespace Ecng.Compilation;

/// <summary>
/// Provides an abstraction for retrieving file content as a string.
/// </summary>
public interface IFileReference
{
	/// <summary>
	/// Retrieves the body of the file as a string.
	/// </summary>
	/// <returns>
	/// A string representing the contents of the file.
	/// </returns>
	string GetFileBody();
}