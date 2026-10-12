namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

internal sealed record Av1DecodedFrame(Av1SequenceHeader Sequence, Av1FrameHeader Frame, int[][] Planes, int[] PlaneWidths, int[] PlaneHeights);
