namespace Ecng.Drawing.Codecs.Jpeg.ScanDecoder;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal sealed class JpegHuffmanLosslessScanDecoder : JpegHuffmanScanDecoder
{
	private readonly JpegFrameHeader _frameHeader;

	private JpegBitReader _rowBits;
	private readonly int _mcusPerLine;
	private readonly int _mcusPerColumn;

	private readonly JpegPartialScanlineAllocator _allocator;
	private readonly bool _ownsAllocator;

	public JpegHuffmanLosslessScanDecoder(JpegDecoder decoder, JpegFrameHeader frameHeader) : this(decoder, frameHeader, null)
	{
	}

	public JpegHuffmanLosslessScanDecoder(JpegDecoder decoder, JpegFrameHeader frameHeader, JpegPartialScanlineAllocator allocator) : base(decoder)
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

		_mcusPerLine = (frameHeader.SamplesPerLine + maxHorizontalSampling - 1) / maxHorizontalSampling;
		_mcusPerColumn = (frameHeader.NumberOfLines + maxVerticalSampling - 1) / maxVerticalSampling;

		JpegBlockOutputWriter outputWriter = decoder.GetOutputWriter();
		if (outputWriter is null)
		{
			ThrowInvalidDataException("Output writer is not set.");
		}
		_ownsAllocator = allocator is null;
		_allocator = allocator ?? new JpegPartialScanlineAllocator(outputWriter, decoder.MemoryPool);
		if (_ownsAllocator) _allocator.Allocate(frameHeader);
	}

	public override void ProcessScan(ref JpegReader reader, JpegScanHeader scanHeader)
	{
		foreach (var _ in ReadRows(reader, scanHeader)) { }
		var bits = _rowBits;
		bits.AdvanceAlignByte();
		var consumed = reader.RemainingByteCount - bits.RemainingBits / 8;
		if (bits.TryPeekMarker() != 0 && !bits.TryPeekMarker().IsRestartMarker()) consumed -= 2;
		if (!reader.TryAdvance(consumed)) ThrowInvalidDataException("Truncated lossless JPEG scan.");
	}

	public override IEnumerable<int> ReadRows(JpegReader reader, JpegScanHeader scanHeader)
	{
		if (scanHeader.Components is null || Decoder.GetOutputWriter() is null)
			ThrowInvalidDataException("Lossless JPEG scan or output parameters are missing.");
		var components = InitDecodeComponents(_frameHeader, scanHeader);
		foreach (var component in components)
			if (component.DcTable is null) ThrowInvalidDataException("Lossless JPEG requires a DC Huffman table.");
		var predictor = scanHeader.StartOfSpectralSelection;
		if (predictor is < 1 or > 7 || scanHeader.EndOfSpectralSelection != 0 || scanHeader.SuccessiveApproximationBitPositionHigh != 0 || scanHeader.SuccessiveApproximationBitPositionLow >= _frameHeader.SamplePrecision)
			ThrowInvalidDataException("Invalid lossless JPEG scan parameters.");
		return ReadRowsCore(reader, scanHeader, components, Decoder.GetRestartInterval());
	}

	private IEnumerable<int> ReadRowsCore(JpegReader reader, JpegScanHeader scanHeader, JpegHuffmanDecodingComponent[] components, int restartInterval)
	{
		var bits = _rowBits = new JpegBitReader(reader.RemainingBytes);
		var interleaved = components.Length > 1;
		var single = components[0];
		var columns = interleaved ? _mcusPerLine : (_frameHeader.SamplesPerLine + single.HorizontalSubsamplingFactor - 1) / single.HorizontalSubsamplingFactor;
		var rows = interleaved ? _mcusPerColumn : (_frameHeader.NumberOfLines + single.VerticalSubsamplingFactor - 1) / single.VerticalSubsamplingFactor;
		if (restartInterval > 0 && restartInterval % columns != 0)
			throw new NotSupportedException("Lossless JPEG restart intervals must cover complete MCU rows.");
		var remaining = restartInterval;
		var predictor = scanHeader.StartOfSpectralSelection;
		var initialPrediction = 1 << (_frameHeader.SamplePrecision - scanHeader.SuccessiveApproximationBitPositionLow - 1);
		var sampleMask = (1 << (_frameHeader.SamplePrecision - scanHeader.SuccessiveApproximationBitPositionLow)) - 1;
		var restartRow = 0;
		var restartNumber = 0;
		for (var row = 0; row < rows; row++)
		{
			ReadRow(bits, components, interleaved, row, columns, rows, predictor, initialPrediction, sampleMask, restartInterval,
				ref remaining, ref restartRow, ref restartNumber);
			if (interleaved || (row + 1) % single.VerticalSamplingFactor == 0 || row + 1 == rows) yield return row;
		}
	}

	private void ReadRow(JpegBitReader bitReader, JpegHuffmanDecodingComponent[] components, bool interleaved, int rowMcu, int mcusPerLine, int mcusPerColumn,
		int predictor, int initialPrediction, int sampleMask, int restartInterval, ref int mcusBeforeRestart, ref int restartRow, ref int restartNumber)
	{
		var allocator = _allocator;
		for (int colMcu = 0; colMcu < mcusPerLine; colMcu++)
		{
			if (restartInterval > 0 && mcusBeforeRestart == restartInterval)
				restartRow = rowMcu;
			// Coded samples follow scan component order, including single-component scans.
			foreach (JpegHuffmanDecodingComponent component in components)
			{
				int index = component.ComponentIndex;
				JpegHuffmanDecodingTable losslessTable = component.DcTable!;
				int h = interleaved ? component.HorizontalSamplingFactor : 1;
				int v = interleaved ? component.VerticalSamplingFactor : 1;
				int offsetX = colMcu * h;
				int offsetY = rowMcu * v;

				for (int y = 0; y < v; y++)
				{
					Span<short> scanline = allocator.GetScanlineSpan(index, offsetY + y);
					Span<short> lastScanline = (y == 0 && rowMcu == 0) ? default : allocator.GetScanlineSpan(index, offsetY + y - 1);

					for (int x = 0; x < h; x++)
					{
						int diffValue = ReadSampleLossless(ref bitReader, losslessTable);

						int pixelX = offsetX + x;
						int pixelY = offsetY + y;
						if (pixelY == restartRow * v)
							diffValue += pixelX == 0 ? initialPrediction : (ushort)scanline[pixelX - 1];
						else if (pixelX == 0)
							diffValue += (ushort)lastScanline[pixelX];
						else
						{
							int left = (ushort)scanline[pixelX - 1];
							int above = (ushort)lastScanline[pixelX];
							int corner = (ushort)lastScanline[pixelX - 1];
							diffValue += predictor switch
							{
								1 => left,
								2 => above,
								3 => corner,
								4 => left + above - corner,
								5 => left + ((above - corner) >> 1),
								6 => above + ((left - corner) >> 1),
								_ => (left + above) >> 1,
							};
						}
						diffValue &= sampleMask;
						scanline[offsetX + x] = (short)diffValue;
					}
				}
			}

			// Handle restart
			if (restartInterval > 0 && (--mcusBeforeRestart) == 0 && !(rowMcu == mcusPerColumn - 1 && colMcu == mcusPerLine - 1))
			{
				bitReader.AdvanceAlignByte();

				JpegMarkers marker = bitReader.TryReadMarker();
				if (marker != (JpegMarkers)((int)JpegMarkers.DefineRestart0 + (restartNumber++ & 7)))
				{
					throw new InvalidOperationException("Expect restart marker.");
				}

				mcusBeforeRestart = restartInterval;
			}
		}

		// Flush allocator
		if (rowMcu == mcusPerColumn - 1)
		{
			foreach (JpegHuffmanDecodingComponent component in components)
			{
				allocator.FlushLastMcu(component.ComponentIndex, (rowMcu + 1) * (interleaved ? component.VerticalSamplingFactor : 1));
			}
		}
		else
		{
			foreach (JpegHuffmanDecodingComponent component in components)
			{
				allocator.FlushMcu(component.ComponentIndex, (rowMcu + 1) * (interleaved ? component.VerticalSamplingFactor : 1));
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ReadSampleLossless(ref JpegBitReader reader, JpegHuffmanDecodingTable losslessTable)
	{
		int t = DecodeHuffmanCode(ref reader, losslessTable);
		if (t == 16)
		{
			t = 32768;
		}
		else if (t != 0)
		{
			t = ReceiveAndExtend(ref reader, t);
		}

		return t;
	}

	public override void Dispose()
	{
		JpegPartialScanlineAllocator allocator = _allocator;

		if (_ownsAllocator) allocator.Dispose();
	}

}
