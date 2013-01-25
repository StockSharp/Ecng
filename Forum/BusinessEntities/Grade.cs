namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	public enum GradeTypes
	{
		Funny,
		NotFunny,
		Agree,
		Disagree,
		Value,
	}

	[Serializable]
	[Audit((byte)AuditSchemas.Grade)]
	public class Grade : ForumBaseEntity
	{
		[RelationSingle]
		[AuditField((byte)AuditFields.Message)]
		public Message Message { get; set; }

		[AuditField((byte)AuditFields.Type)]
		public GradeTypes Type { get; set; }

		[AuditField((byte)AuditFields.Value)]
		public short Value { get; set; }
	}
}