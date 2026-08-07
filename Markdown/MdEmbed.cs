namespace Ecng.Markdown;

/// <summary>
/// Content the author embedded from elsewhere by address.
/// </summary>
/// <param name="url">Address of the embedded page or media.</param>
/// <param name="width">Width the author asked for, zero when unset.</param>
/// <param name="height">Height the author asked for, zero when unset.</param>
public sealed class MdEmbed(string url, int width, int height) : MdBlock
{
	/// <summary>
	/// Address of the embedded page or media.
	/// </summary>
	public string Url { get; } = url;

	/// <summary>
	/// Width the author asked for.
	/// </summary>
	public int Width { get; } = width;

	/// <summary>
	/// Height the author asked for.
	/// </summary>
	public int Height { get; } = height;
}
