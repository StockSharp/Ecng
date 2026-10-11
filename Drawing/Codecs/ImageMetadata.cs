namespace Ecng.Drawing.Codecs;

using System.Collections.Generic;

internal sealed class ImageMetadata
{
	public IList<RawMetadataProfile> Profiles { get; } = new List<RawMetadataProfile>();
}
