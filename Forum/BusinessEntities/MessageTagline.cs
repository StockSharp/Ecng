namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Logic.BusinessEntities;

	[Serializable]
	[Audit((byte)AuditSchemas.MessageTagline)]
	public class MessageTagline : ForumBaseEntity
	{
		[String(1, 2048)]
		[Validation]
		[AuditField((byte)AuditFields.Body)]
		public string Body { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.Message)]
		public Message Message { get; set; }
	}
}