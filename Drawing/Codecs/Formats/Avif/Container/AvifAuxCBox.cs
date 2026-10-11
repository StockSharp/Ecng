namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class AvifAuxCBox
{
	private const string AlphaUrn = "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha";
	private const string AlphaUrnLegacy = "urn:mpeg:hevc:2015:auxid:1";

	public static bool IsAlpha(byte[] data, AvifBox box)
	{
		if (box.PayloadLength < 4)
		{
			throw new AvifDecodingException("Truncated 'auxC' box.");
		}

		int offset = box.PayloadOffset + 4; // version(1) + flags(3)
		int end = box.PayloadOffset + box.PayloadLength;
		string auxType = AvifBinaryReader.ReadCString(data, ref offset, end);
		return auxType is AlphaUrn or AlphaUrnLegacy;
	}
	public static byte[] Build()
	{
		byte[] urn = System.Text.Encoding.ASCII.GetBytes(AlphaUrn);
		var payload = new byte[urn.Length + 1]; // aux_type cstring: URN bytes + null terminator (already 0)
		Array.Copy(urn, payload, urn.Length);
		return AvifBoxWriter.FullBox("auxC", version: 0, flags: 0, payload);
	}
}
