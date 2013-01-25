namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Net;

	using Ecng.Serialization;
	using Ecng.Logic.BusinessEntities;

	[Serializable]
	[Audit((byte)AuditSchemas.Download)]
	public class Download : ForumBaseEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.File)]
		public File File { get; set; }

		[IpAddress]
		public IPAddress IpAddress { get; set; }
	}
}