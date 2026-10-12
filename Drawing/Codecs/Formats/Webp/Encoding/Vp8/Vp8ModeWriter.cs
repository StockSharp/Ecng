namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

internal sealed class Vp8ModeWriter
{
	private readonly int[] _aboveSubblockModes;
	private readonly int[] _leftSubblockModes = new int[4];

	public Vp8ModeWriter(int macroblockCols)
	{
		_aboveSubblockModes = new int[macroblockCols * 4];
		Array.Fill(_aboveSubblockModes, Vp8PredictionModes.BDcPred);
	}

	public void StartRow() => Array.Fill(_leftSubblockModes, Vp8PredictionModes.BDcPred);

	public void WriteMacroblockModes(Vp8BoolEncoder bw, int mbX, bool skip, bool useSkipProbability, int skipFalseProbability, bool isI4x4, int yMode, ReadOnlySpan<int> subModes, int uvMode)
	{
		if (useSkipProbability)
		{
			bw.PutBit(skip ? 1 : 0, skipFalseProbability);
		}

		bw.PutBit(isI4x4 ? 0 : 1, 145); // Vp8ModeDecoder: IsI4x4 = GetBit(145) == 0.

		int baseCol = mbX * 4;
		if (!isI4x4)
		{
			WriteWholeBlockYMode(bw, yMode);
			for (int i = 0; i < 4; i++)
			{
				_aboveSubblockModes[baseCol + i] = yMode;
				_leftSubblockModes[i] = yMode;
			}
		}
		else
		{
			for (int y = 0; y < 4; y++)
			{
				int leftMode = _leftSubblockModes[y];
				for (int x = 0; x < 4; x++)
				{
					int aboveMode = _aboveSubblockModes[baseCol + x];
					int mode = subModes[(y * 4) + x];
					WriteSubblockMode(bw, aboveMode, leftMode, mode);
					_aboveSubblockModes[baseCol + x] = mode;
					leftMode = mode;
				}

				_leftSubblockModes[y] = leftMode;
			}
		}

		WriteUvMode(bw, uvMode);
	}

	private static void WriteWholeBlockYMode(Vp8BoolEncoder bw, int mode)
	{
		switch (mode)
		{
			case Vp8PredictionModes.DcPred:
				bw.PutBit(0, 156);
				bw.PutBit(0, 163);
				break;
			case Vp8PredictionModes.VPred:
				bw.PutBit(0, 156);
				bw.PutBit(1, 163);
				break;
			case Vp8PredictionModes.HPred:
				bw.PutBit(1, 156);
				bw.PutBit(0, 128);
				break;
			case Vp8PredictionModes.TmPred:
				bw.PutBit(1, 156);
				bw.PutBit(1, 128);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown VP8 whole-block prediction mode.");
		}
	}

	private static void WriteUvMode(Vp8BoolEncoder bw, int mode)
	{
		switch (mode)
		{
			case Vp8PredictionModes.DcPred:
				bw.PutBit(0, 142);
				break;
			case Vp8PredictionModes.VPred:
				bw.PutBit(1, 142);
				bw.PutBit(0, 114);
				break;
			case Vp8PredictionModes.HPred:
				bw.PutBit(1, 142);
				bw.PutBit(1, 114);
				bw.PutBit(0, 183);
				break;
			case Vp8PredictionModes.TmPred:
				bw.PutBit(1, 142);
				bw.PutBit(1, 114);
				bw.PutBit(1, 183);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown VP8 whole-block prediction mode.");
		}
	}

	private static void WriteSubblockMode(Vp8BoolEncoder bw, int above, int left, int mode)
	{
		byte P(int k) => Vp8BModeProbabilities.Table[above, left, k];

		switch (mode)
		{
			case Vp8PredictionModes.BDcPred:
				bw.PutBit(0, P(0));
				return;
			case Vp8PredictionModes.BTmPred:
				bw.PutBit(1, P(0));
				bw.PutBit(0, P(1));
				return;
			case Vp8PredictionModes.BVePred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(0, P(2));
				return;
			case Vp8PredictionModes.BHePred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(0, P(3));
				bw.PutBit(0, P(4));
				return;
			case Vp8PredictionModes.BRdPred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(0, P(3));
				bw.PutBit(1, P(4));
				bw.PutBit(0, P(5));
				return;
			case Vp8PredictionModes.BVrPred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(0, P(3));
				bw.PutBit(1, P(4));
				bw.PutBit(1, P(5));
				return;
			case Vp8PredictionModes.BLdPred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(1, P(3));
				bw.PutBit(0, P(6));
				return;
			case Vp8PredictionModes.BVlPred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(1, P(3));
				bw.PutBit(1, P(6));
				bw.PutBit(0, P(7));
				return;
			case Vp8PredictionModes.BHdPred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(1, P(3));
				bw.PutBit(1, P(6));
				bw.PutBit(1, P(7));
				bw.PutBit(0, P(8));
				return;
			case Vp8PredictionModes.BHuPred:
				bw.PutBit(1, P(0));
				bw.PutBit(1, P(1));
				bw.PutBit(1, P(2));
				bw.PutBit(1, P(3));
				bw.PutBit(1, P(6));
				bw.PutBit(1, P(7));
				bw.PutBit(1, P(8));
				return;
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown VP8 4x4 subblock prediction mode.");
		}
	}
}
