namespace Ecng.Drawing.Codecs.Jpeg.ScanDecoder;


using System;
using System.Runtime.CompilerServices;

internal sealed class JpegHuffmanLosslessScanDecoder : JpegHuffmanScanDecoder
{
	private readonly JpegFrameHeader _frameHeader;

	private readonly ushort _restartInterval;
	private readonly int _mcusPerLine;
	private readonly int _mcusPerColumn;

	private readonly JpegPartialScanlineAllocator _allocator;
	private readonly JpegHuffmanDecodingComponent[] _components;

	public JpegHuffmanLosslessScanDecoder(JpegDecoder decoder, JpegFrameHeader frameHeader) : base(decoder)
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

		_restartInterval = decoder.GetRestartInterval();
		_mcusPerLine = (frameHeader.SamplesPerLine + maxHorizontalSampling - 1) / maxHorizontalSampling;
		_mcusPerColumn = (frameHeader.NumberOfLines + maxVerticalSampling - 1) / maxVerticalSampling;

		JpegBlockOutputWriter outputWriter = decoder.GetOutputWriter();
		if (outputWriter is null)
		{
			ThrowInvalidDataException("Output writer is not set.");
		}
		_allocator = new JpegPartialScanlineAllocator(outputWriter, decoder.MemoryPool);
		_allocator.Allocate(frameHeader);

		// Pre-allocate the JpegDecodeComponent instances
		_components = new JpegHuffmanDecodingComponent[frameHeader.NumberOfComponents];
		for (int i = 0; i < _components.Length; i++)
		{
			_components[i] = new JpegHuffmanDecodingComponent();
		}
	}

	public override void ProcessScan(ref JpegReader reader, JpegScanHeader scanHeader)
	{
		if (scanHeader.Components is null)
		{
			throw new InvalidOperationException();
		}
		if (Decoder.GetOutputWriter() is null)
		{
			throw new InvalidOperationException();
		}

		// Resolve each component
		Span<JpegHuffmanDecodingComponent> components = _components.AsSpan(0, InitDecodeComponents(_frameHeader, scanHeader, _components));
		foreach (JpegHuffmanDecodingComponent component in components)
		{
			if (component.DcTable is null)
			{
				ThrowInvalidDataException($"Huffman table of component {component.ComponentIndex} is not defined.");
			}
		}

		JpegPartialScanlineAllocator allocator = _allocator;
		int mcusPerLine = _mcusPerLine;
		int mcusPerColumn = _mcusPerColumn;

		// Prepare
		JpegBitReader bitReader = new JpegBitReader(reader.RemainingBytes);
		int restartInterval = _restartInterval;
		int mcusBeforeRestart = restartInterval;
		int predictor = scanHeader.StartOfSpectralSelection;
		if (predictor is < 1 or > 7 || scanHeader.EndOfSpectralSelection != 0 || scanHeader.SuccessiveApproximationBitPositionHigh != 0 || scanHeader.SuccessiveApproximationBitPositionLow >= _frameHeader.SamplePrecision)
			ThrowInvalidDataException("Invalid lossless JPEG scan parameters.");
		if (restartInterval > 0 && restartInterval % mcusPerLine != 0)
			throw new NotSupportedException("Lossless JPEG restart intervals must cover complete MCU rows.");
		int initialPrediction = 1 << (_frameHeader.SamplePrecision - scanHeader.SuccessiveApproximationBitPositionLow - 1);
		int sampleMask = (1 << (_frameHeader.SamplePrecision - scanHeader.SuccessiveApproximationBitPositionLow)) - 1;
		int restartRow = 0;
		int restartNumber = 0;

		for (int rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
		{
			for (int colMcu = 0; colMcu < mcusPerLine; colMcu++)
			{
				if (restartInterval > 0 && mcusBeforeRestart == restartInterval)
					restartRow = rowMcu;
				// Scan an interleaved mcu... process components in order
				foreach (JpegHuffmanDecodingComponent component in components)
				{
					int index = component.ComponentIndex;
					JpegHuffmanDecodingTable losslessTable = component.DcTable!;
					int h = component.HorizontalSamplingFactor;
					int v = component.VerticalSamplingFactor;
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
					allocator.FlushLastMcu(component.ComponentIndex, (rowMcu + 1) * component.VerticalSamplingFactor);
				}
			}
			else
			{
				foreach (JpegHuffmanDecodingComponent component in components)
				{
					allocator.FlushMcu(component.ComponentIndex, (rowMcu + 1) * component.VerticalSamplingFactor);
				}
			}
		}

		bitReader.AdvanceAlignByte();
		int bytesConsumed = reader.RemainingByteCount - bitReader.RemainingBits / 8;
		if (bitReader.TryPeekMarker() != 0)
		{
			if (!bitReader.TryPeekMarker().IsRestartMarker())
			{
				bytesConsumed -= 2;
			}
		}
		reader.TryAdvance(bytesConsumed);
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

		allocator.Dispose();
	}

}
