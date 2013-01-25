namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.IO;

	using Ecng.Logic.BusinessEntities;
    using Ecng.Serialization;

	[Serializable]
	[Audit((byte)AuditSchemas.File)]
	public class File : ForumBaseNamedEntity
	{
#if !SILVERLIGHT
		[RelationMany(typeof(FileDownloadList), CacheCount = true)]
		public DownloadList Downloads { get; protected set; }
#endif
		[Stream]
		public Stream Body { get; set; }

		[AuditField((byte)AuditFields.AuditDownloads)]
		public bool AuditDownloads { get; set; }
	}
}