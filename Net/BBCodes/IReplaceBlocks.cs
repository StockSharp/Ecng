namespace Ecng.Net.BBCodes
{
	/// <summary>
	/// The i add replace block.
	/// </summary>
	public interface IReplaceBlocks
  {
    #region Public Methods

    /// <summary>
    /// The add replacement.
    /// </summary>
    /// <param name="newItem">
    /// The new item.
    /// </param>
    /// <returns>
    /// The add replacement.
    /// </returns>
    int Add(string newItem);

    /// <summary>
    /// Gets the replacement value from the index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    string Get(int index);

    #endregion
  }
}