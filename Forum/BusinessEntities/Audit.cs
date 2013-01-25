namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Ignore(FieldName = "ModificationDate")]
	[Ignore(FieldName = "Deleted")]
	[Ignore(FieldName = "Changes")]
	public class Audit : BaseEntity
	{
		public byte SchemaId { get; set; }
		public byte FieldId { get; set; }
		public Guid TransactionId { get; set; }
		public long EntityId { get; set; }

		[Primitive(IsNullable = true)]
		public object Value { get; set; }
	}
}