namespace Ecng.Drawing.Codecs.Jpeg.ScanDecoder;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal class JpegArithmeticSequentialScanDecoder : JpegArithmeticScanDecoder
{
	private readonly JpegFrameHeader _frameHeader;

	private JpegBitReader _rowBits;
	private readonly int _mcusPerLine;
	private readonly int _mcusPerColumn;
	private readonly int _levelShift;

	public JpegArithmeticSequentialScanDecoder(JpegDecoder decoder, JpegFrameHeader frameHeader) : base(decoder)
	{
		_frameHeader = frameHeader;

		// Compute maximum sampling factor
		int maxHorizontalSampling = 1;
		int maxVerticalSampling = 1;
		foreach (JpegFrameComponentSpecificationParameters currentFrameComponent in frameHeader.Components!)
		{
			maxHorizontalSampling = Math.Max(maxHorizontalSampling, currentFrameComponent.HorizontalSamplingFactor);
			maxVerticalSampling = Math.Max(maxVerticalSampling, currentFrameComponent.VerticalSamplingFactor);
		}
		_mcusPerLine = (frameHeader.SamplesPerLine + 8 * maxHorizontalSampling - 1) / (8 * maxHorizontalSampling);
		_mcusPerColumn = (frameHeader.NumberOfLines + 8 * maxVerticalSampling - 1) / (8 * maxVerticalSampling);
		_levelShift = 1 << (frameHeader.SamplePrecision - 1);
	}

	public override void ProcessScan(ref JpegReader reader, JpegScanHeader scanHeader)
	{
		foreach (var _ in ReadRows(reader, scanHeader)) { }
		var bits = _rowBits;
		bits.AdvanceAlignByte();
		var consumed = reader.RemainingByteCount - bits.RemainingBits / 8;
		if (bits.TryPeekMarker() != 0 && !bits.TryPeekMarker().IsRestartMarker()) consumed -= 2;
		if (!reader.TryAdvance(consumed)) ThrowInvalidDataException("Truncated arithmetic JPEG scan.");
	}

	public override IEnumerable<int> ReadRows(JpegReader reader, JpegScanHeader scanHeader)
	{
		if (_frameHeader.Components is null || scanHeader.Components is null || Decoder.GetOutputWriter() is null)
			ThrowInvalidDataException("JPEG frame, scan or output parameters are missing.");
		var components = InitDecodeComponents(_frameHeader, scanHeader);
		foreach (var component in components)
		{
			component.DcPredictor = 0;
			component.DcContext = 0;
			component.DcStatistics?.Reset();
			component.AcStatistics?.Reset();
		}
		Reset();
		return ReadRowsCore(reader, components, Decoder.GetRestartInterval());
	}

	private IEnumerable<int> ReadRowsCore(JpegReader reader, JpegArithmeticDecodingComponent[] components, int restartInterval)
	{
		var bits = _rowBits = new JpegBitReader(reader.RemainingBytes);
		var interleaved = components.Length > 1;
		var single = components[0];
		var columns = interleaved ? _mcusPerLine : (_frameHeader.SamplesPerLine + single.HorizontalSubsamplingFactor * 8 - 1) / (single.HorizontalSubsamplingFactor * 8);
		var rows = interleaved ? _mcusPerColumn : (_frameHeader.NumberOfLines + single.VerticalSubsamplingFactor * 8 - 1) / (single.VerticalSubsamplingFactor * 8);
		var remaining = restartInterval;
		var restartNumber = 0;
		for (var row = 0; row < rows; row++)
		{
			for (var col = 0; col < columns; col++)
			{
				foreach (var component in components)
					for (var y = 0; y < (interleaved ? component.VerticalSamplingFactor : 1); y++)
						for (var x = 0; x < (interleaved ? component.HorizontalSamplingFactor : 1); x++)
						{
							var bx = interleaved ? col * component.HorizontalSamplingFactor + x : col;
							var by = interleaved ? row * component.VerticalSamplingFactor + y : row;
							ReadAndWriteBlock(bits, component, bx, by);
						}
				if (restartInterval > 0 && --remaining == 0 && (row != rows - 1 || col != columns - 1))
				{
					bits.AdvanceAlignByte();
					var marker = bits.TryReadMarker();
					if (marker != (JpegMarkers)((int)JpegMarkers.DefineRestart0 + (restartNumber++ & 7)))
						ThrowInvalidDataException("Arithmetic JPEG restart marker is out of order.");
					remaining = restartInterval;
					foreach (var component in components)
					{
						component.DcPredictor = 0;
						component.DcContext = 0;
						component.DcStatistics?.Reset();
						component.AcStatistics?.Reset();
					}
					Reset();
				}
			}
			if (interleaved || (row + 1) % single.VerticalSamplingFactor == 0 || row + 1 == rows) yield return row;
		}
	}

	private void ReadAndWriteBlock(JpegBitReader bits, JpegArithmeticDecodingComponent component, int x, int y)
	{
		var block = new JpegBlock8x8();
		var values = new JpegBlock8x8F();
		var output = new JpegBlock8x8F();
		var scratch = new JpegBlock8x8F();
		ReadBlock(ref bits, component, ref block);
		DequantizeBlockAndUnZigZag(component.QuantizationTable, ref block, ref values);
		FastFloatingPointDCT.TransformIDCT(ref values, ref output, ref scratch);
		ShiftDataLevel(ref output, ref block, _levelShift);
		WriteBlock(Decoder.GetOutputWriter(), ref Unsafe.As<JpegBlock8x8, short>(ref block), component.ComponentIndex,
			x * component.HorizontalSubsamplingFactor * 8, y * component.VerticalSubsamplingFactor * 8,
			component.HorizontalSubsamplingFactor, component.VerticalSubsamplingFactor);
	}

	private void ReadBlock(ref JpegBitReader reader, JpegArithmeticDecodingComponent component, ref JpegBlock8x8 destinationBlock)
	{
		ref short destinationRef = ref Unsafe.As<JpegBlock8x8, short>(ref destinationBlock);

		// Sections F.2.4.1 & F.1.4.4.1: Decoding of DC coefficients

		// Table F.4: Point to statistics bin S0 for DC coefficient coding
		ref byte st = ref Unsafe.Add(ref component.DcStatistics!.GetReference(), component.DcContext);

		/* Figure F.19: Decode_DC_DIFF */
		if (DecodeBinaryDecision(ref reader, ref st) == 0)
		{
			component.DcContext = 0;
		}
		else
		{
			// Figure F.21: Decoding nonzero value v
			// Figure F.22: Decoding the sign of v
			int sign = DecodeBinaryDecision(ref reader, ref Unsafe.Add(ref st, 1));
			st = ref Unsafe.Add(ref st, 2 + sign);
			// Figure F.23: Decoding the magnitude category of v
			int m = DecodeBinaryDecision(ref reader, ref st);
			if (m != 0)
			{
				st = ref component.DcStatistics!.GetReference(20);
				while (DecodeBinaryDecision(ref reader, ref st) != 0)
				{
					if ((m <<= 1) == 0x8000)
					{
						ThrowInvalidDataException("Invalid arithmetic code.");
					}
					st = ref Unsafe.Add(ref st, 1);
				}
			}
			// Section F.1.4.4.1.2: Establish dc_context conditioning category
			if (m < (int)((1L << component.DcTable!.DcL) >> 1))
			{
				component.DcContext = 0; // zero diff category
			}
			else if (m > (int)((1L << component.DcTable!.DcU) >> 1))
			{
				component.DcContext = 12 + (sign * 4); // large diff category
			}
			else
			{
				component.DcContext = 4 + (sign * 4);  // small diff category
			}
			int v = m;
			// Figure F.24: Decoding the magnitude bit pattern of v
			st = ref Unsafe.Add(ref st, 14);
			while ((m >>= 1) != 0)
			{
				if (DecodeBinaryDecision(ref reader, ref st) != 0)
				{
					v |= m;
				}
			}
			v += 1;
			if (sign != 0)
			{
				v = -v;
			}
			component.DcPredictor = (short)(component.DcPredictor + v);
		}

		destinationRef = (short)component.DcPredictor;

		// Sections F.2.4.2 & F.1.4.4.2: Decoding of AC coefficients
		JpegArithmeticStatistics acStatistics = component.AcStatistics!;
		JpegArithmeticDecodingTable acTable = component.AcTable!;

		for (int k = 1; k <= 63; k++)
		{
			st = ref acStatistics.GetReference(3 * (k - 1));
			if (DecodeBinaryDecision(ref reader, ref st) != 0)
			{
				// EOB flag
				break;
			}
			while (DecodeBinaryDecision(ref reader, ref Unsafe.Add(ref st, 1)) == 0)
			{
				st = ref Unsafe.Add(ref st, 3);
				k++;
				if (k > 63)
				{
					ThrowInvalidDataException("Invalid arithmetic code.");
				}
			}
			// Figure F.21: Decoding nonzero value v
			// Figure F.22: Decoding the sign of v
			int sign = DecodeBinaryDecision(ref reader, ref GetFixedBinReference());
			st = ref Unsafe.Add(ref st, 2);
			// Figure F.23: Decoding the magnitude category of v
			int m = DecodeBinaryDecision(ref reader, ref st);
			if (m != 0)
			{
				if (DecodeBinaryDecision(ref reader, ref st) != 0)
				{
					m <<= 1;
					st = ref acStatistics.GetReference(k <= acTable.AcKx ? 189 : 217);
					while (DecodeBinaryDecision(ref reader, ref st) != 0)
					{
						if ((m <<= 1) == 0x8000)
						{
							ThrowInvalidDataException("Invalid arithmetic code.");
						}
						st = ref Unsafe.Add(ref st, 1);
					}
				}
			}
			int v = m;
			// Figure F.24: Decoding the magnitude bit pattern of v
			st = ref Unsafe.Add(ref st, 14);
			while ((m >>= 1) != 0)
			{
				if (DecodeBinaryDecision(ref reader, ref st) != 0)
				{
					v |= m;
				}
			}
			v += 1;
			if (sign != 0)
			{
				v = -v;
			}
			Unsafe.Add(ref destinationRef, k) = (short)v;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteBlock(JpegBlockOutputWriter outputWriter, ref short blockRef, int componentIndex, int x, int y, int horizontalSubsamplingFactor, int verticalSubsamplingFactor)
	{
		if (horizontalSubsamplingFactor == 1 && verticalSubsamplingFactor == 1)
		{
			outputWriter!.WriteBlock(ref blockRef, componentIndex, x, y);
		}
		else
		{
			WriteBlockSlow(outputWriter, ref blockRef, componentIndex, x, y, horizontalSubsamplingFactor, verticalSubsamplingFactor);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void WriteBlockSlow(JpegBlockOutputWriter outputWriter, ref short blockRef, int componentIndex, int x, int y, int horizontalSubsamplingFactor, int verticalSubsamplingFactor)
	{
		Unsafe.SkipInit(out JpegBlock8x8 tempBlock);

		int hShift = JpegMathHelper.Log2((uint)horizontalSubsamplingFactor);
		int vShift = JpegMathHelper.Log2((uint)verticalSubsamplingFactor);

		ref short tempRef = ref Unsafe.As<JpegBlock8x8, short>(ref Unsafe.AsRef(in tempBlock));

		for (int v = 0; v < verticalSubsamplingFactor; v++)
		{
			for (int h = 0; h < horizontalSubsamplingFactor; h++)
			{
				int vBlock = 8 * v;
				int hBlock = 8 * h;
				// Fill tempBlock
				for (int i = 0; i < 8; i++)
				{
					ref short tempRowRef = ref Unsafe.Add(ref tempRef, 8 * i);
					ref short blockRowRef = ref Unsafe.Add(ref blockRef, ((vBlock + i) >> vShift) * 8);
					for (int j = 0; j < 8; j++)
					{
						Unsafe.Add(ref tempRowRef, j) = Unsafe.Add(ref blockRowRef, (hBlock + j) >> hShift);
					}
				}

				// Write tempBlock to output
				outputWriter.WriteBlock(ref tempRef, componentIndex, x + 8 * h, y + 8 * v);
			}
		}
	}

	public override void Dispose()
	{
		// Do nothing
	}

}
