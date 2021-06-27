namespace Ecng.Net.BBCodes
{
	using System;
	using System.Text;

	/// <summary>
	/// The high lighter.
	/// </summary>
	public class HighLighter
    {
        // Default Constructor
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "HighLighter" /> class.
        /// </summary>
        public HighLighter()
        {
            ReplaceEnter = false;
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Gets or sets a value indicating whether ReplaceEnter.
        /// </summary>
        public bool ReplaceEnter { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Colors the text.
        /// </summary>
        /// <param name="tmpCode">The tmp code.</param>
        /// <param name="language">The language.</param>
        /// <returns>
        /// The color text.
        /// </returns>
        public string ColorText(string tmpCode, string language)
        {
            language = language.ToLower();

            language = language.Replace("\"", string.Empty);

            if (language.Equals("cs"))
            {
                language = "csharp";
            }

            var tmpOutput = new StringBuilder();

			var highlight = string.Empty;
	  
             // extract highlight
			if (language.Contains(";"))
			{
				highlight = language.Substring(language.IndexOf(";") + 1);
				language = language.Remove(language.IndexOf(";"));
			}

        	// Create Output
            tmpOutput.AppendFormat(
				"<pre class=\"brush:{0}{1}\">{2}",
                language,
				!string.IsNullOrEmpty(highlight) ? $";highlight: [{highlight}];" : string.Empty,
                Environment.NewLine);

            tmpOutput.Append(tmpCode);
            tmpOutput.AppendFormat("</pre>{0}", Environment.NewLine);

            return tmpOutput.ToString();
        }

        #endregion
    }
}