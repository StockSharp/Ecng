namespace Ecng.Markdown.Extensions;

using System.Text;

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;

/// <summary>
/// Ordered lists with alphabetic ("a. b. c.") and roman-numeral ("i. ii. iii.") markers, rendered as
/// &lt;ol type="a"&gt; / &lt;ol type="i"&gt; (upper-case markers give type="A"/"I"). Standard markdown only
/// knows "-" and "1.", so these markers would otherwise render as plain paragraphs. Parsed at the AST level
/// (not raw HTML), so it works for regular authors whose HTML is stripped by the safe pipeline.
/// </summary>
public class OrderedMarkerListItemParser : ListItemParser
{
	private static readonly int[] _romanValues = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
	private static readonly string[] _romanSymbols = ["m", "cm", "d", "cd", "c", "xc", "l", "xl", "x", "ix", "v", "iv", "i"];

	public OrderedMarkerListItemParser()
	{
		OpeningCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
	}

	public override bool TryParse(BlockProcessor state, char pendingBulletType, out ListInfo result)
	{
		result = new ListInfo();

		var c = state.CurrentChar;
		var marker = new StringBuilder(8);

		while (char.IsLetter(c) && marker.Length < 8)
		{
			marker.Append(c);
			c = state.NextChar();
		}

		if (marker.Length == 0 || (c != '.' && c != ')'))
			return false;

		var text = marker.ToString();
		var lower = text.ToLowerInvariant();
		var upper = char.IsUpper(text[0]);

		char bulletType;
		int order;

		if (pendingBulletType is 'i' or 'I')
		{
			// Continuing a roman list -- every item must be a roman numeral.
			if (!TryRoman(lower, out order))
				return false;

			bulletType = upper ? 'I' : 'i';
		}
		else if (pendingBulletType is 'a' or 'A')
		{
			// Continuing an alpha list -- a single letter (so "i" here is the 9th letter, not roman 1).
			if (text.Length != 1)
				return false;

			order = lower[0] - 'a' + 1;
			bulletType = upper ? 'A' : 'a';
		}
		else
		{
			// A fresh list: "i" or a multi-letter roman numeral opens a roman list; any other single letter
			// opens an alpha list. That keeps the ambiguous single letters (i/v/x/c/d/l/m) as roman only when
			// the list actually starts at "i", matching how such lists are written.
			if ((lower == "i" || lower.Length > 1) && TryRoman(lower, out order))
			{
				bulletType = upper ? 'I' : 'i';
			}
			else if (text.Length == 1)
			{
				order = lower[0] - 'a' + 1;
				bulletType = upper ? 'A' : 'a';
			}
			else
			{
				return false;
			}
		}

		result.BulletType = bulletType;
		result.OrderedStart = order.ToString();
		result.OrderedDelimiter = c;
		result.DefaultOrderedStart = "1";

		state.NextChar(); // consume the '.' or ')'
		return true;
	}

	private static bool TryRoman(string s, out int value)
	{
		value = 0;

		if (s.IsEmpty())
			return false;

		var total = 0;
		var prev = 0;

		for (var i = s.Length - 1; i >= 0; i--)
		{
			var cur = s[i] switch
			{
				'i' => 1,
				'v' => 5,
				'x' => 10,
				'l' => 50,
				'c' => 100,
				'd' => 500,
				'm' => 1000,
				_ => -1,
			};

			if (cur < 0)
				return false;

			if (cur < prev)
			{
				total -= cur;
			}
			else
			{
				total += cur;
				prev = cur;
			}
		}

		// Reject malformed sequences (e.g. "iic", "vv"): a well-formed roman numeral round-trips exactly.
		if (ToRoman(total) != s)
			return false;

		value = total;
		return true;
	}

	private static string ToRoman(int n)
	{
		var sb = new StringBuilder();

		for (var k = 0; k < _romanValues.Length; k++)
		{
			while (n >= _romanValues[k])
			{
				sb.Append(_romanSymbols[k]);
				n -= _romanValues[k];
			}
		}

		return sb.ToString();
	}
}

/// <summary>
/// Registers <see cref="OrderedMarkerListItemParser"/> with the list block parser.
/// </summary>
public class OrderedMarkerListExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// Swap in a fresh list block parser (its constructor seeds the default unordered/numbered item
		// parsers) with ours added. Mutating the existing instance is unsafe: its item-parser list can be
		// shared between the trusted and safe pipelines, so a second registration would throw on the
		// duplicate opening character.
		var listParser = new ListBlockParser();
		listParser.ItemParsers.Add(new OrderedMarkerListItemParser());

		pipeline.BlockParsers.Replace<ListBlockParser>(listParser);

		// Markdig flags any list that does not start with a digit as unordered, so our alpha/roman lists parse
		// as <ul>. Flip them back to ordered after parsing; the HTML list renderer then emits
		// <ol type="a"> / <ol type="i"> (etc.) from the BulletType our item parser set.
		pipeline.DocumentProcessed += MarkOrderedLists;
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
	}

	private static void MarkOrderedLists(MarkdownDocument document)
	{
		foreach (var list in document.Descendants<ListBlock>())
		{
			if (!list.IsOrdered && list.BulletType is 'a' or 'A' or 'i' or 'I')
				list.IsOrdered = true;
		}
	}
}
