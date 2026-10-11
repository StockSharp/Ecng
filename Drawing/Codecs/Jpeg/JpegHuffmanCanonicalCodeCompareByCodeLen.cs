namespace Ecng.Drawing.Codecs.Jpeg;


using System;
using System.Collections.Generic;

internal class JpegHuffmanCanonicalCodeCompareByCodeLen : Comparer<JpegHuffmanCanonicalCode>
{
	public static JpegHuffmanCanonicalCodeCompareByCodeLen Instance { get; } = new JpegHuffmanCanonicalCodeCompareByCodeLen();

	public override int Compare(JpegHuffmanCanonicalCode x, JpegHuffmanCanonicalCode y)
	{
		if (x.CodeLength > y.CodeLength)
		{
			return 1;
		}
		else if (x.CodeLength < y.CodeLength)
		{
			return -1;
		}
		else
		{
			if (x.Symbol > y.Symbol)
			{
				return 1;
			}
			else
			{
				return -1;
			}
		}
	}
}
