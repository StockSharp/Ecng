namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

internal static class Vp8HeaderWriter
{
	public static void WritePartition0Header(
		Vp8BoolEncoder bw,
		int baseQIndex,
		int filterLevel,
		int filterSharpness,
		bool simpleFilter,
		int skipFalseProbability)
	{
		bw.PutFlag(false); // color_space -- always standard studio-range YUV.
		bw.PutFlag(false); // clamping_type -- reconstructed pixels are always clamped to [0,255].

		bw.PutFlag(false); // segmentation_enabled = false (UseSegment).

		bw.PutFlag(simpleFilter);
		bw.PutValue((uint)filterLevel, 6);
		bw.PutValue((uint)filterSharpness, 3);
		bw.PutFlag(false); // loop_filter_adj_enable = false (UseLfDelta).

		bw.PutValue(0, 2); // log2(partition count) = 0 -> a single coefficient partition.

		bw.PutValue((uint)baseQIndex, 7);
		bw.PutFlag(false); // y1dc_delta_q present
		bw.PutFlag(false); // y2dc_delta_q present
		bw.PutFlag(false); // y2ac_delta_q present
		bw.PutFlag(false); // uvdc_delta_q present
		bw.PutFlag(false); // uvac_delta_q present

		bw.PutFlag(true); // refresh_entropy_probs -- irrelevant for a lone keyframe, matching the decoder's own comment.

		WriteNoCoefficientProbabilityUpdates(bw);

		bw.PutFlag(true); // mb_no_skip_coeff (useSkipProbability) = true.
		bw.PutValue((uint)skipFalseProbability, 8);
	}

	private static void WriteNoCoefficientProbabilityUpdates(Vp8BoolEncoder bw)
	{
		byte[] updateProbabilities = Vp8CoefficientProbabilities.UpdateProbabilityFlat;
		for (int i = 0; i < Vp8CoefficientProbabilities.FlatLength; i++)
		{
			bw.PutBit(0, updateProbabilities[i]);
		}
	}

	public static Vp8QuantMatrix ResolveBaseQuantMatrix(int baseQIndex)
	{
		var scratch = new Vp8BoolEncoder();
		scratch.PutFlag(false); // segmentation_enabled = false.
		scratch.PutValue((uint)baseQIndex, 7);
		scratch.PutFlag(false); // y1dc_delta_q present
		scratch.PutFlag(false); // y2dc_delta_q present
		scratch.PutFlag(false); // y2ac_delta_q present
		scratch.PutFlag(false); // uvdc_delta_q present
		scratch.PutFlag(false); // uvac_delta_q present
		byte[] bytes = scratch.Finish();

		var decoder = new Vp8BoolDecoder(bytes, 0, bytes.Length);
		Vp8SegmentHeader segmentHeader = Vp8SegmentHeader.Parse(decoder);
		Vp8QuantIndices quantIndices = Vp8QuantIndices.Parse(decoder);
		return Vp8Dequantizer.Resolve(quantIndices, segmentHeader)[0];
	}
}
