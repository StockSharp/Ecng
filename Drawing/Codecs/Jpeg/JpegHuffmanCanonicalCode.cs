namespace Ecng.Drawing.Codecs.Jpeg;


using System;
using System.Collections.Generic;

internal struct JpegHuffmanCanonicalCode
{
	public ushort Code { get; set; }
	public byte Symbol { get; set; }
	public byte CodeLength { get; set; }
	public override string ToString()
	{
		return $"JpegCanonicalCode(Symbol={Symbol},Code={Convert.ToString(Code, 2).PadLeft(CodeLength, '0')},CodeLength={CodeLength})";
	}
}
