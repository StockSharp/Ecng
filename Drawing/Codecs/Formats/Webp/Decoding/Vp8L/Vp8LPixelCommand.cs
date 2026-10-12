namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

internal readonly record struct Vp8LPixelCommand(int Length, int Distance, uint Color, int CacheIndex);
