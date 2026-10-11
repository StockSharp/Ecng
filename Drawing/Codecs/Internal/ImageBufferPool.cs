namespace Ecng.Drawing.Codecs.Internal;

using System.Buffers;

internal static class ImageBufferPool
{
	public static ArrayPool<byte> Shared => ArrayPool<byte>.Shared;
}
