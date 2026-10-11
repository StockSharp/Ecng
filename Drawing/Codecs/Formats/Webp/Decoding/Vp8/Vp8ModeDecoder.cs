namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Vp8ModeDecoder
{
	private readonly int[] _aboveSubblockModes;
	private readonly int[] _leftSubblockModes = new int[4];
	private readonly Vp8SegmentHeader _segmentHeader;
	private readonly bool _useSkipProbability;
	private readonly int _skipFalseProbability;

	public Vp8ModeDecoder(int macroblockCols, Vp8SegmentHeader segmentHeader, bool useSkipProbability, int skipFalseProbability)
	{
		_aboveSubblockModes = new int[macroblockCols * 4];
		Array.Fill(_aboveSubblockModes, Vp8PredictionModes.BDcPred);
		_segmentHeader = segmentHeader;
		_useSkipProbability = useSkipProbability;
		_skipFalseProbability = skipFalseProbability;
	}
	public void StartRow()
	{
		Array.Fill(_leftSubblockModes, Vp8PredictionModes.BDcPred);
	}

	public void DecodeMacroblock(Vp8BoolDecoder br, int mbX, Vp8MacroblockModes info)
	{
		if (_segmentHeader.UpdateMap)
		{
			byte[] p = _segmentHeader.SegmentTreeProbabilities;
			info.Segment = br.GetBit(p[0]) == 0
				? br.GetBit(p[1])
				: br.GetBit(p[2]) + 2;
		}
		else
		{
			info.Segment = 0;
		}

		info.Skip = _useSkipProbability && br.GetBit(_skipFalseProbability) != 0;

		info.IsI4x4 = br.GetBit(145) == 0;

		int baseCol = mbX * 4;
		if (!info.IsI4x4)
		{
			int ymode = br.GetBit(156) != 0
				? (br.GetBit(128) != 0 ? Vp8PredictionModes.TmPred : Vp8PredictionModes.HPred)
				: (br.GetBit(163) != 0 ? Vp8PredictionModes.VPred : Vp8PredictionModes.DcPred);
			info.YMode = ymode;

			for (int i = 0; i < 4; i++)
			{
				_aboveSubblockModes[baseCol + i] = ymode;
				_leftSubblockModes[i] = ymode;
			}
		}
		else
		{
			info.YMode = Vp8PredictionModes.BPred;
			for (int y = 0; y < 4; y++)
			{
				int leftMode = _leftSubblockModes[y];
				for (int x = 0; x < 4; x++)
				{
					int aboveMode = _aboveSubblockModes[baseCol + x];
					int mode = DecodeSubblockMode(br, aboveMode, leftMode);
					info.SubModes[(y * 4) + x] = mode;
					_aboveSubblockModes[baseCol + x] = mode;
					leftMode = mode;
				}

				_leftSubblockModes[y] = leftMode;
			}
		}

		info.UvMode = br.GetBit(142) == 0
			? Vp8PredictionModes.DcPred
			: br.GetBit(114) == 0
				? Vp8PredictionModes.VPred
				: br.GetBit(183) != 0
					? Vp8PredictionModes.TmPred
					: Vp8PredictionModes.HPred;
	}

	private static int DecodeSubblockMode(Vp8BoolDecoder br, int above, int left)
	{
		byte P(int k) => Vp8BModeProbabilities.Table[above, left, k];

		if (br.GetBit(P(0)) == 0)
		{
			return Vp8PredictionModes.BDcPred;
		}

		if (br.GetBit(P(1)) == 0)
		{
			return Vp8PredictionModes.BTmPred;
		}

		if (br.GetBit(P(2)) == 0)
		{
			return Vp8PredictionModes.BVePred;
		}

		if (br.GetBit(P(3)) == 0)
		{
			if (br.GetBit(P(4)) == 0)
			{
				return Vp8PredictionModes.BHePred;
			}

			return br.GetBit(P(5)) == 0 ? Vp8PredictionModes.BRdPred : Vp8PredictionModes.BVrPred;
		}

		if (br.GetBit(P(6)) == 0)
		{
			return Vp8PredictionModes.BLdPred;
		}

		if (br.GetBit(P(7)) == 0)
		{
			return Vp8PredictionModes.BVlPred;
		}

		return br.GetBit(P(8)) == 0 ? Vp8PredictionModes.BHdPred : Vp8PredictionModes.BHuPred;
	}
}
