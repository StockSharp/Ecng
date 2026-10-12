namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

internal static class Vp8ImageEncoder
{
	private const int UncompressedHeaderLength = 10;

	private const int MaxFirstPartitionLength = (1 << 19) - 1;

	private const int SkipFalseProbability = 200;

	public static byte[] Encode(byte[] rgb, int width, int height, int quality)
	{
		int baseQIndex = Vp8QualityMapping.QualityToBaseQIndex(quality);
		int filterLevel = Vp8QualityMapping.QualityToFilterLevel(quality);

		Vp8QuantMatrix quant = Vp8HeaderWriter.ResolveBaseQuantMatrix(baseQIndex);

		var partition0Encoder = new Vp8BoolEncoder();
		Vp8HeaderWriter.WritePartition0Header(
			partition0Encoder,
			baseQIndex,
			filterLevel,
			filterSharpness: 0,
			simpleFilter: false,
			skipFalseProbability: SkipFalseProbability);

		var coefficientEncoder = new Vp8BoolEncoder();

		Vp8FrameEncoder.Encode(
			rgb, width, height,
			partition0Encoder, coefficientEncoder,
			quant, useSkipProbability: true, SkipFalseProbability);

		byte[] partition0 = partition0Encoder.Finish();
		byte[] coefficientPartition = coefficientEncoder.Finish();

		if (partition0.Length > MaxFirstPartitionLength)
		{
			throw new InvalidDataException($"VP8 partition 0 ({partition0.Length} bytes) exceeds the 19-bit first_partition_length field's maximum of {MaxFirstPartitionLength} bytes.");
		}

		return AssembleChunk(width, height, partition0, coefficientPartition);
	}

	private static byte[] AssembleChunk(int width, int height, byte[] partition0, byte[] coefficientPartition)
	{
		var chunk = new byte[UncompressedHeaderLength + partition0.Length + coefficientPartition.Length];

		uint tag = (1u << 4) | ((uint)partition0.Length << 5);
		chunk[0] = (byte)tag;
		chunk[1] = (byte)(tag >> 8);
		chunk[2] = (byte)(tag >> 16);

		chunk[3] = 0x9d;
		chunk[4] = 0x01;
		chunk[5] = 0x2a;

		chunk[6] = (byte)width;
		chunk[7] = (byte)(width >> 8);
		chunk[8] = (byte)height;
		chunk[9] = (byte)(height >> 8);

		partition0.CopyTo(chunk, UncompressedHeaderLength);
		coefficientPartition.CopyTo(chunk, UncompressedHeaderLength + partition0.Length);

		return chunk;
	}
}
