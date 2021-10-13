namespace Ecng.Net.BBCodes
{
	using System;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Syntax Highlighted code block regular express replace
	/// </summary>
	public class SyntaxHighlightedCodeRegexReplaceRule<TContext, TDomain> : SimpleRegexReplaceRule<TContext, TDomain>
		where TContext : BBCodesContext<TDomain>
	{
        #region Constants and Fields

        /// <summary>
        ///   The _syntax highlighter.
        /// </summary>
        private readonly HighLighter _syntaxHighlighter = new();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxHighlightedCodeRegexReplaceRule"/> class.
        /// </summary>
        /// <param name="regExSearch">
        /// The reg ex search.
        /// </param>
        /// <param name="regExReplace">
        /// The reg ex replace.
        /// </param>
        public SyntaxHighlightedCodeRegexReplaceRule(Regex regExSearch, Func<TDomain, string> regExReplace)
            : base(regExSearch, regExReplace)
        {
            _syntaxHighlighter.ReplaceEnter = true;
            RuleRank = 1;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The replace.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="replacement">
        /// The replacement.
        /// </param>
        public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
        {
            Match m = RegExSearch.Match(text);
            while (m.Success)
            {
				cancellationToken.ThrowIfCancellationRequested();

				string inner = _syntaxHighlighter.ColorText(
                    GetInnerValue(m.Groups["inner"].Value), m.Groups["language"].Value);

                string replaceItem = RegExReplace(context.Domain).Replace("${inner}", inner);

                // pulls the htmls into the replacement collection before it's inserted back into the main text
                int replaceIndex = replacement.Add(replaceItem);

                text = text.Substring(0, m.Groups[0].Index) + replacement.Get(replaceIndex)
                       + text.Substring(m.Groups[0].Index + m.Groups[0].Length);

                m = RegExSearch.Match(text);
            }

			return Task.FromResult(text);
        }

        #endregion

        #region Methods

        /// <summary>
        /// This just overrides how the inner value is handled
        /// </summary>
        /// <param name="innerValue">The inner value.</param>
        /// <returns>
        /// The get inner value.
        /// </returns>
        protected override string GetInnerValue(string innerValue)
        {
            return innerValue;
        }

        #endregion
    }
}