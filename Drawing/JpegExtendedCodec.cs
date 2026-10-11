namespace Ecng.Drawing;

using System;
using System.IO;

using JpegDecoder = Ecng.Drawing.Codecs.Jpeg.JpegDecoder;

internal static class JpegExtendedCodec
{
	public static bool IsExtended(ReadOnlySpan<byte> data)
	{
		var cursor = 2;
		while (JpegCodec.NextSegment(data, ref cursor, out var marker, out _))
		{
			if (marker is >= 0xc0 and <= 0xcf and not (0xc4 or 0xc8 or 0xcc)) return marker is 0xc3 or 0xc9 or 0xca;
			if (marker == 0xda) break;
		}
		return false;
	}

	public static RasterImage Decode(byte[] data)
	{
		if (data.Length > 128 * 1024 * 1024) throw new InvalidDataException("Oversized JPEG input.");
		JpegCodec.ValidateScanHeader(data);
		var cursor = 2;
		JpegComponent[] components = null;
		var hasJfif = false;
		var adobeTransform = -1;
		var pointTransform = 0;
		var isLossless = false;
		while (JpegCodec.NextSegment(data, ref cursor, out var marker, out var segment))
		{
			if (marker == JpegSegments.Jfif) hasJfif |= JpegSegments.IsJfif(segment);
			if (marker == JpegSegments.Adobe && JpegSegments.TryReadAdobeTransform(segment, out var transform)) adobeTransform = transform;
			if (marker is 0xc3 or 0xc9 or 0xca)
			{
				isLossless = marker == 0xc3;
				components = JpegSegments.ReadFrame(segment, out _, out _, 1, isLossless);
			}
			if (marker == 0xda)
			{
				if (isLossless) pointTransform = segment[^1] & 15;
				break;
			}
		}
		if (components is null) throw new InvalidDataException("JPEG has no supported frame.");
		try
		{
			var decoder = new JpegDecoder();
			decoder.SetInput(data);
			decoder.Identify();
			RasterImage.EnsureDecodable(decoder.Width, decoder.Height);
			if (pointTransform >= decoder.Precision) throw new InvalidDataException("Invalid lossless JPEG point transform.");
			var maxH = decoder.GetMaximumHorizontalSampling();
			var maxV = decoder.GetMaximumVerticalSampling();
			foreach (var component in components)
			{
				if (maxH % component.H != 0 || maxV % component.V != 0) throw new NotSupportedException("Non-integral subsampling in arithmetic or lossless JPEG.");
				component.H = component.V = 1;
				component.BlocksWide = (decoder.Width + 7) / 8;
				component.BlocksHigh = (decoder.Height + 7) / 8;
				component.Samples = new byte[checked(component.Stride * component.BlocksHigh * 8)];
			}
			decoder.SetOutputWriter(new JpegSampleWriter(components, decoder.Width, decoder.Height, decoder.Precision, pointTransform, isLossless));
			decoder.Decode();
			return JpegTransforms.ToImage(decoder.Width, decoder.Height, components, JpegTransforms.ResolveColorModel(components, hasJfif, adobeTransform));
		}
		catch (Exception error) when (error is ArgumentException or IndexOutOfRangeException or OverflowException or InvalidOperationException)
		{
			throw new InvalidDataException("Invalid arithmetic or lossless JPEG.", error);
		}
	}
}
