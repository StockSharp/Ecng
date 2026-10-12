namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;

using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.ColorConversion;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Upsampling;

internal sealed class Vp8ThumbnailBuffer
{
	private readonly byte[][] _planes;
	private readonly int[] _strides;
	private readonly byte[][] _prediction;
	private readonly byte[][] _filtered;
	private readonly int _width;
	private readonly int _height;
	private readonly int _chromaWidth;
	private readonly int _chromaHeight;
	private readonly ImageDownscaler _downscaler;
	private readonly WebpAlphaRows _alpha;
	private readonly byte[] _u;
	private readonly byte[] _v;
	private readonly byte[] _rgb;
	private readonly byte[] _rgba;

	public Vp8ThumbnailBuffer(byte[] y, byte[] u, byte[] v, int yStride, int uvStride, int width, int height,
		int outputWidth, int outputHeight, WebpAlphaRows alpha, ImageProcessingLimits limits)
	{
		_planes = [y, u, v];
		_strides = [yStride, uvStride, uvStride];
		_prediction = [new byte[yStride], new byte[uvStride], new byte[uvStride]];
		_filtered = [new byte[yStride], new byte[uvStride], new byte[uvStride]];
		_width = width;
		_height = height;
		_chromaWidth = ((width + 15) / 16) * 8;
		_chromaHeight = ((height + 15) / 16) * 8;
		_downscaler = new(width, height, outputWidth, outputHeight, limits);
		_alpha = alpha;
		_u = new byte[width];
		_v = new byte[width];
		_rgb = new byte[checked(width * 3)];
		_rgba = new byte[checked(width * 4)];
	}

	public static long EstimateWorkingBytes(int width, int outputWidth, int outputHeight)
	{
		var columns = (width + 15) / 16;
		var yStride = columns * 16 + 1;
		var uvStride = columns * 8 + 1;
		return 2L * (yStride * 33 + uvStride * 34) + columns * 128L + width * 24L +
			(long)outputWidth * outputHeight * 4 + outputWidth * 80L + 4096;
	}

	public void BeginRow(int macroblockRow)
	{
		for (var plane = 0; plane < 3; plane++)
		{
			var rowSize = plane == 0 ? 16 : 8;
			var border = _planes[plane].AsSpan(rowSize * _strides[plane], _strides[plane]);
			if (macroblockRow == 0)
				border.Fill(127);
			else
			{
				border.CopyTo(_filtered[plane]);
				_prediction[plane].CopyTo(border);
			}
		}
	}

	public void EndPrediction(int macroblockRow)
	{
		for (var plane = 0; plane < 3; plane++)
		{
			var rowSize = plane == 0 ? 16 : 8;
			var stride = _strides[plane];
			_planes[plane].AsSpan(rowSize * 2 * stride, stride).CopyTo(_prediction[plane]);
			if (macroblockRow != 0)
				_filtered[plane].CopyTo(_planes[plane].AsSpan(rowSize * stride, stride));
		}
	}

	public void CommitRow(int macroblockRow)
	{
		if (macroblockRow != 0)
			OutputRows(macroblockRow - 1);
		for (var plane = 0; plane < 3; plane++)
		{
			var rowSize = plane == 0 ? 16 : 8;
			var stride = _strides[plane];
			_planes[plane].AsSpan(rowSize * stride, (rowSize + 1) * stride).CopyTo(_planes[plane]);
		}
	}

	public Vp8DecodedFrame Complete()
	{
		OutputRows((_height - 1) / 16);
		return new Vp8DecodedFrame
		{
			Width = _downscaler.Image.Width,
			Height = _downscaler.Image.Height,
			PixelFormat = CodecPixelFormats.Rgba32,
			Pixels = _downscaler.Image.Pixels,
		};
	}

	private void OutputRows(int macroblockRow)
	{
		var converter = Vp8ColorConverterSelector.Instance;
		var uvStride = _strides[1];
		for (var row = 0; row < 16 && macroblockRow * 16 + row < _height; row++)
		{
			var y = macroblockRow * 16 + row;
			var near = y >> 1;
			var far = Math.Clamp(near + ((y & 1) == 1 ? 1 : -1), 0, _chromaHeight - 1);
			var nearOffset = (near - macroblockRow * 8 + 1) * uvStride + 1;
			var farOffset = (far - macroblockRow * 8 + 1) * uvStride + 1;
			Vp8ChromaUpsampler.UpsampleRow(_planes[1].AsSpan(nearOffset, _chromaWidth), _planes[1].AsSpan(farOffset, _chromaWidth), _u, _width, _chromaWidth);
			Vp8ChromaUpsampler.UpsampleRow(_planes[2].AsSpan(nearOffset, _chromaWidth), _planes[2].AsSpan(farOffset, _chromaWidth), _v, _width, _chromaWidth);
			converter.ConvertRow(_planes[0].AsSpan((row + 1) * _strides[0] + 1, _width), _u, _v, _rgb, _width);
			var alpha = _alpha is null ? ReadOnlySpan<byte>.Empty : _alpha.ReadRow();
			for (var x = 0; x < _width; x++)
			{
				_rgba[x * 4] = _rgb[x * 3];
				_rgba[x * 4 + 1] = _rgb[x * 3 + 1];
				_rgba[x * 4 + 2] = _rgb[x * 3 + 2];
				_rgba[x * 4 + 3] = _alpha is null ? (byte)255 : alpha[x];
			}
			_downscaler.AddRow(_rgba);
		}
	}
}
