namespace Ecng.Drawing.Codecs;

internal sealed class RawMetadataProfile
{
	public MetadataProfileKinds Kind { get; init; }
	public byte[] Data { get; init; }
}
