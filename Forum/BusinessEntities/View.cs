namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	//[Audit((byte)AuditSchemas.View)]
	public class View : ForumBaseEntity
	{
		[RelationSingle]
		//[AuditField((byte)AuditFields.Topic)]
		public Topic Topic { get; set; }
	}
}