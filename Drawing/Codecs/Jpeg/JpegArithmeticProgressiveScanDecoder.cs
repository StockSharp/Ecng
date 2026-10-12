namespace Ecng.Drawing.Codecs.Jpeg.ScanDecoder;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal class JpegArithmeticProgressiveScanDecoder : JpegArithmeticScanDecoder
{
	private readonly JpegFrameHeader _frameHeader;

	private readonly int _mcusPerLine;
	private readonly int _mcusPerColumn;
	private readonly int _levelShift;

	private ushort _restartInterval;
	private int _mcusBeforeRestart;

	private readonly JpegBlockOutputWriter _outputWriter;
	private readonly JpegBlockAllocator _allocator;
	private readonly JpegArithmeticDecodingComponent[] _components;
	private readonly bool _ownsAllocator;

	public JpegArithmeticProgressiveScanDecoder(JpegDecoder decoder, JpegFrameHeader frameHeader)
		: this(decoder, frameHeader, null)
	{
	}

	public JpegArithmeticProgressiveScanDecoder(JpegDecoder decoder, JpegFrameHeader frameHeader, JpegBlockAllocator allocator) : base(decoder)
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

		JpegBlockOutputWriter outputWriter = decoder.GetOutputWriter();
		if (outputWriter is null)
		{
			ThrowInvalidDataException("Output writer is not set.");
		}
		_outputWriter = outputWriter;
		_ownsAllocator = allocator is null;
		_allocator = allocator ?? new JpegBlockAllocator(decoder.MemoryPool);
		if (_ownsAllocator) _allocator.Allocate(frameHeader);

		// Pre-allocate the JpegDecodeComponent instances
		_components = new JpegArithmeticDecodingComponent[frameHeader.NumberOfComponents];
		for (int i = 0; i < _components.Length; i++)
		{
			_components[i] = new JpegArithmeticDecodingComponent();
		}
	}

	public override IEnumerable<int> ReadRows(JpegReader reader, JpegScanHeader header)
	{
		var components = InitDecodeComponents(_frameHeader, header);
		_restartInterval = Decoder.GetRestartInterval();
		_mcusBeforeRestart = _restartInterval;
		Reset();
		return ReadRowsCore(reader, header, components);
	}

	private IEnumerable<int> ReadRowsCore(JpegReader reader, JpegScanHeader header, JpegArithmeticDecodingComponent[] components)
	{
		var bits = new JpegBitReader(reader.RemainingBytes);
		var interleaved = components.Length > 1;
		var single = components[0];
		var columns = interleaved ? _mcusPerLine : (_frameHeader.SamplesPerLine + single.HorizontalSubsamplingFactor * 8 - 1) / (single.HorizontalSubsamplingFactor * 8);
		var rows = interleaved ? _mcusPerColumn : (_frameHeader.NumberOfLines + single.VerticalSubsamplingFactor * 8 - 1) / (single.VerticalSubsamplingFactor * 8);
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
							ReadBlockAt(bits, component, header, bx, by);
						}
				if (row != rows - 1 || col != columns - 1)
					if (!HandleRestart(ref bits, ref reader, ref header, ref components[0], components.Length))
						ThrowInvalidDataException("Arithmetic JPEG scan ends before its last block.");
			}
			if (interleaved || (row + 1) % single.VerticalSamplingFactor == 0 || row + 1 == rows) yield return row;
		}
	}

	private void ReadBlockAt(JpegBitReader bits, JpegArithmeticDecodingComponent component, JpegScanHeader header, int x, int y)
	{
		ref var block = ref _allocator.GetBlockReference(component.ComponentIndex, x, y);
		if (header.StartOfSpectralSelection == 0) ReadBlockProgressiveDC(ref bits, component, header, ref block);
		else ReadBlockProgressiveAC(ref bits, component, header, ref block);
	}

	public void ReconstructRow(int rowIndex)
	{
		var block = new JpegBlock8x8F();
		var output = new JpegBlock8x8F();
		var scratch = new JpegBlock8x8F();
		for (var i = 0; i < _frameHeader.Components.Length; i++)
		{
			var component = _frameHeader.Components[i];
			var quant = Decoder.GetQuantizationTable(component.QuantizationTableSelector);
			for (var y = 0; y < component.VerticalSamplingFactor; y++)
				for (var x = 0; x < _mcusPerLine * component.HorizontalSamplingFactor; x++)
				{
					ref var coefficients = ref _allocator.GetBlockReference(i, x, rowIndex * component.VerticalSamplingFactor + y);
					DequantizeBlockAndUnZigZag(quant, ref coefficients, ref block);
					FastFloatingPointDCT.TransformIDCT(ref block, ref output, ref scratch);
					ShiftDataLevel(ref output, ref coefficients, _levelShift);
				}
		}
		_allocator.FlushRow(_outputWriter, rowIndex);
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
		Span<JpegArithmeticDecodingComponent> components = _components.AsSpan(0, InitDecodeComponents(_frameHeader, scanHeader, _components));

		foreach (JpegArithmeticDecodingComponent component in _components)
		{
			if (scanHeader.StartOfSpectralSelection == 0 && scanHeader.SuccessiveApproximationBitPositionHigh == 0)
			{
				component.DcPredictor = 0;
				component.DcContext = 0;
				component.DcStatistics?.Reset();
			}
			if (scanHeader.StartOfSpectralSelection != 0)
			{
				component.AcStatistics?.Reset();
			}
		}

		_restartInterval = Decoder.GetRestartInterval();
		_mcusBeforeRestart = _restartInterval;
		Reset();

		if (components.Length == 1)
		{
			DecodeProgressiveDataNonInterleaved(ref reader, scanHeader, components[0]);
		}
		else
		{
			DecodeProgressiveDataInterleaved(ref reader, scanHeader, components);
		}
	}

	private void DecodeProgressiveDataInterleaved(ref JpegReader reader, JpegScanHeader scanHeader, Span<JpegArithmeticDecodingComponent> components)
	{
		foreach (JpegArithmeticDecodingComponent component in components)
		{
			if (component.DcTable is null || component.DcStatistics is null)
			{
				ThrowInvalidDataException("DC table is missing.");
			}
		}

		JpegBlockAllocator allocator = _allocator;
		JpegBitReader bitReader = new JpegBitReader(reader.RemainingBytes);

		int mcusPerColumn = _mcusPerColumn;
		int mcusPerLine = _mcusPerLine;

		for (int rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
		{
			for (int colMcu = 0; colMcu < mcusPerLine; colMcu++)
			{
				foreach (JpegArithmeticDecodingComponent component in components)
				{
					int index = component.ComponentIndex;
					int h = component.HorizontalSamplingFactor;
					int v = component.VerticalSamplingFactor;
					int offsetX = colMcu * h;
					int offsetY = rowMcu * v;

					for (int y = 0; y < v; y++)
					{
						int blockOffsetY = offsetY + y;
						for (int x = 0; x < h; x++)
						{
							ref JpegBlock8x8 blockRef = ref allocator.GetBlockReference(index, offsetX + x, blockOffsetY);

							ReadBlockProgressiveDC(ref bitReader, component, scanHeader, ref blockRef);
						}
					}
				}

				if (!HandleRestart(ref bitReader, ref reader, ref scanHeader, ref MemoryMarshal.GetReference(components), components.Length))
				{
					return;
				}
			}
		}
	}

	private void DecodeProgressiveDataNonInterleaved(ref JpegReader reader, JpegScanHeader scanHeader, JpegArithmeticDecodingComponent component)
	{
		JpegBlockAllocator allocator = _allocator;
		JpegBitReader bitReader = new JpegBitReader(reader.RemainingBytes);

		int componentIndex = component.ComponentIndex;
		int horizontalBlockCount = (_frameHeader.SamplesPerLine + 8 * component.HorizontalSubsamplingFactor - 1) / (8 * component.HorizontalSubsamplingFactor);
		int verticalBlockCount = (_frameHeader.NumberOfLines + 8 * component.VerticalSubsamplingFactor - 1) / (8 * component.VerticalSubsamplingFactor);

		if (scanHeader.StartOfSpectralSelection == 0)
		{
			if (component.DcTable is null || component.DcStatistics is null)
			{
				ThrowInvalidDataException("DC table is missing.");
			}

			for (int blockY = 0; blockY < verticalBlockCount; blockY++)
			{
				for (int blockX = 0; blockX < horizontalBlockCount; blockX++)
				{
					ref JpegBlock8x8 blockRef = ref allocator.GetBlockReference(componentIndex, blockX, blockY);

					ReadBlockProgressiveDC(ref bitReader, component, scanHeader, ref blockRef);

					if (!HandleRestart(ref bitReader, ref reader, ref scanHeader, ref component, 1))
					{
						return;
					}
				}
			}
		}
		else
		{
			if (component.AcTable is null || component.AcStatistics is null)
			{
				ThrowInvalidDataException("AC table is missing");
			}

			for (int blockY = 0; blockY < verticalBlockCount; blockY++)
			{
				for (int blockX = 0; blockX < horizontalBlockCount; blockX++)
				{
					ref JpegBlock8x8 blockRef = ref allocator.GetBlockReference(componentIndex, blockX, blockY);

					ReadBlockProgressiveAC(ref bitReader, component, scanHeader, ref blockRef);

					if (!HandleRestart(ref bitReader, ref reader, ref scanHeader, ref component, 1))
					{
						return;
					}
				}
			}
		}
	}

	private bool HandleRestart(ref JpegBitReader bitReader, ref JpegReader reader, ref JpegScanHeader scanHeader, ref JpegArithmeticDecodingComponent componentRef, int componentCount)
	{
		if (_restartInterval > 0 && (--_mcusBeforeRestart) == 0)
		{
			bitReader.AdvanceAlignByte();

			JpegMarkers marker = bitReader.TryReadMarker();
			if (marker == JpegMarkers.EndOfImage)
			{
				int bytesConsumedEoi = reader.RemainingByteCount - bitReader.RemainingBits / 8;
				reader.TryAdvance(bytesConsumedEoi - 2);
				return false;
			}
			if (!marker.IsRestartMarker())
			{
				throw new InvalidOperationException("Expect restart marker.");
			}

			_mcusBeforeRestart = _restartInterval;

			for (int i = 0; i < componentCount; i++)
			{
				if (scanHeader.StartOfSpectralSelection == 0 && scanHeader.SuccessiveApproximationBitPositionHigh == 0)
				{
					componentRef.DcPredictor = 0;
					componentRef.DcContext = 0;
					componentRef.DcStatistics?.Reset();
				}
				if (scanHeader.StartOfSpectralSelection != 0)
				{
					componentRef.AcStatistics?.Reset();
				}

				componentRef = ref Unsafe.Add(ref componentRef, 1);
			}

			Reset();
		}

		return true;
	}

	private void ReadBlockProgressiveDC(ref JpegBitReader reader, JpegArithmeticDecodingComponent component, JpegScanHeader scanHeader, ref JpegBlock8x8 destinationBlock)
	{
		ref short blockDataRef = ref Unsafe.As<JpegBlock8x8, short>(ref destinationBlock);

		if (scanHeader.SuccessiveApproximationBitPositionHigh == 0)
		{
			// First scan

			// Sections F.2.4.1 & F.1.4.4.1: Decoding of DC coefficients

			// Table F.4: Point to statistics bin S0 for DC coefficient coding
			ref byte st = ref Unsafe.Add(ref component.DcStatistics!.GetReference(), component.DcContext);

			// Figure F.19: Decode_DC_DIFF
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

			blockDataRef = (short)(component.DcPredictor << scanHeader.SuccessiveApproximationBitPositionLow);
		}
		else
		{
			// Refinement scan
			ref byte st = ref GetFixedBinReference();

			blockDataRef |= (short)(DecodeBinaryDecision(ref reader, ref st) << scanHeader.SuccessiveApproximationBitPositionLow);
		}
	}

	private void ReadBlockProgressiveAC(ref JpegBitReader reader, JpegArithmeticDecodingComponent component, JpegScanHeader scanHeader, ref JpegBlock8x8 destinationBlock)
	{
		ref short blockDataRef = ref Unsafe.As<JpegBlock8x8, short>(ref destinationBlock);

		JpegArithmeticStatistics acStatistics = component.AcStatistics!;
		JpegArithmeticDecodingTable acTable = component.AcTable!;

		if (scanHeader.SuccessiveApproximationBitPositionHigh == 0)
		{
			// Sections F.2.4.2 & F.1.4.4.2: Decoding of AC coefficients

			// Figure F.20: Decode_AC_coefficients
			int start = scanHeader.StartOfSpectralSelection;
			int end = scanHeader.EndOfSpectralSelection;
			int low = scanHeader.SuccessiveApproximationBitPositionLow;

			for (int k = start; k <= end; k++)
			{
				ref byte st = ref acStatistics.GetReference(3 * (k - 1));
				if (DecodeBinaryDecision(ref reader, ref st) != 0)
				{
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
				Unsafe.Add(ref blockDataRef, k) = (short)(v << low);
			}
		}
		else
		{
			// Refinement scan
			ReadBlockProgressiveACRefined(ref reader, acStatistics, scanHeader, ref blockDataRef);
		}
	}

	private void ReadBlockProgressiveACRefined(ref JpegBitReader reader, JpegArithmeticStatistics acStatistics, JpegScanHeader scanHeader, ref short blockDataRef)
	{
		int start = scanHeader.StartOfSpectralSelection;
		int end = scanHeader.EndOfSpectralSelection;

		int p1 = 1 << scanHeader.SuccessiveApproximationBitPositionLow;
		int m1 = (-1) << scanHeader.SuccessiveApproximationBitPositionLow;

		// Establish EOBx (previous stage end-of-block) index
		int kex = end;
		for (; kex > 0; kex--)
		{
			if (Unsafe.Add(ref blockDataRef, kex) != 0)
			{
				break;
			}
		}

		for (int k = start; k <= end; k++)
		{
			ref byte st = ref acStatistics.GetReference(3 * (k - 1));
			if (k > kex)
			{
				if (DecodeBinaryDecision(ref reader, ref st) != 0)
				{
					break;
				}
			}
			while (true)
			{
				ref short coef = ref Unsafe.Add(ref blockDataRef, k);
				if (coef != 0) // previously nonzero coef
				{
					if (DecodeBinaryDecision(ref reader, ref Unsafe.Add(ref st, 2)) != 0)
					{
						if (coef < 0)
						{
							coef = (short)(coef + m1);
						}
						else
						{
							coef = (short)(coef + p1);
						}
					}
					break;
				}
				if (DecodeBinaryDecision(ref reader, ref Unsafe.Add(ref st, 1)) != 0) // newly nonzero coef
				{
					if (DecodeBinaryDecision(ref reader, ref GetFixedBinReference()) != 0)
					{
						coef = (short)(coef + m1);
					}
					else
					{
						coef = (short)(coef + p1);
					}
					break;
				}
				st = ref Unsafe.Add(ref st, 3);
				k++;
				if (k > end)
				{
					ThrowInvalidDataException("Invalid arithmetic code.");
				}
			}
		}
	}


	public override void Dispose()
	{
		if (!_ownsAllocator) return;
		JpegBlockAllocator allocator = _allocator;

		int mcusPerColumn = _mcusPerColumn;
		int mcusPerLine = _mcusPerLine;
		int levelShift = _levelShift;
		JpegArithmeticDecodingComponent[] components = _components;

		Unsafe.SkipInit(out JpegBlock8x8F blockFBuffer);
		Unsafe.SkipInit(out JpegBlock8x8F outputFBuffer);
		Unsafe.SkipInit(out JpegBlock8x8F tempFBuffer);

		for (int rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
		{
			for (int colMcu = 0; colMcu < mcusPerLine; colMcu++)
			{
				// Scan an interleaved mcu... process components in order
				foreach (JpegArithmeticDecodingComponent component in components)
				{
					int index = component.ComponentIndex;
					int h = component.HorizontalSamplingFactor;
					int v = component.VerticalSamplingFactor;
					int offsetX = colMcu * h;
					int offsetY = rowMcu * v;

					for (int y = 0; y < v; y++)
					{
						int blockOffsetY = offsetY + y;
						for (int x = 0; x < h; x++)
						{
							ref JpegBlock8x8 blockRef = ref allocator.GetBlockReference(index, offsetX + x, blockOffsetY);

							// Dequantization
							DequantizeBlockAndUnZigZag(component.QuantizationTable, ref blockRef, ref blockFBuffer);

							// IDCT
							FastFloatingPointDCT.TransformIDCT(ref blockFBuffer, ref outputFBuffer, ref tempFBuffer);

							// Level shift
							ShiftDataLevel(ref outputFBuffer, ref blockRef, levelShift);
						}
					}
				}
			}
		}

		allocator.Flush(_outputWriter);
		allocator.Dispose();
	}
}
