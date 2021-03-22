namespace Ecng.Net.BBCodes
{
	using System;
	using System.Collections.Generic;
	using System.Text;

    using Ecng.Common;

	/// <summary>
	/// Handles the collection of replacement tags and can also pull the HTML out of the text making a new replacement tag
	/// </summary>
	public class ReplaceBlocksCollection : IReplaceBlocks
  {
    #region Constants and Fields

    /// <summary>
    ///   The _replacement dictionary.
    /// </summary>
    private readonly Dictionary<int, string> _replacementDictionary;

    /// <summary>
    ///   The _current index.
    /// </summary>
    private int _currentIndex;

    /// <summary>
    ///   The _random instance.
    /// </summary>
    private int _randomInstance;

    /// <summary>
    ///  REPLACEMENT UNIQUE VALUE -- USED TO CREATE A UNIQUE VALUE TO REPLACE -- IT IS NOT SUPPOSED TO BE HUMAN READABLE.
    /// </summary>
    private string _replaceFormat = "÷ñÒ{1}êÖ{0}õæ÷";

    #endregion

    #region Constructors and Destructors

    /// <summary>
    ///   Initializes a new instance of the <see cref = "ReplaceBlocksCollection" /> class.
    /// </summary>
    public ReplaceBlocksCollection()
    {
      this._replacementDictionary = new Dictionary<int, string>();
      this.RandomizeInstance();
    }

    #endregion

    #region Properties

    /// <summary>
    ///   Gets ReplacementDictionary.
    /// </summary>
    public Dictionary<int, string> ReplacementDictionary
    {
      get
      {
        return this._replacementDictionary;
      }
    }

    #endregion

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
    public int Add(string newItem)
    {
      this._replacementDictionary.Add(this._currentIndex, newItem);
      return this._currentIndex++;
    }

    /// <summary>
    /// The get replace value.
    /// </summary>
    /// <param name="index">
    /// The index.
    /// </param>
    /// <returns>
    /// The get replace value.
    /// </returns>
    public string Get(int index)
    {
      return this._replaceFormat.Put(index, this._randomInstance);
    }

    /// <summary>
    /// get a random number for the instance
    ///   so it's harder to guess the replacement format
    /// </summary>
    public void RandomizeInstance()
    {
      var rand = new Random();
      this._randomInstance = rand.Next();
    }

    /// <summary>
    /// Reconstructs the text from the collection elements...
    /// </summary>
    /// <param name="text">
    /// </param>
    public void Reconstruct(ref string text)
    {
      var sb = new StringBuilder(text);

      foreach (int index in this._replacementDictionary.Keys)
      {
        sb.Replace(this.Get(index), this._replacementDictionary[index]);
      }

      text = sb.ToString();
    }

    #endregion
  }
}