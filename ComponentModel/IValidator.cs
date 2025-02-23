namespace Ecng.ComponentModel;

/// <summary>
/// Provides functionality for validating objects.
/// </summary>
public interface IValidator
{
	/// <summary>
	/// Gets or sets a value indicating whether null checks should be disabled.
	/// </summary>
	bool DisableNullCheck { get; set; }
}