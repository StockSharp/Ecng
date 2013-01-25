namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Ignore(FieldName = "ModificationDate")]
	[Ignore(FieldName = "Deleted")]
	[Ignore(FieldName = "Changes")]
	[Entity("Audit")]
	public class Audit<TUser, TRole> : BaseEntity<TUser, TRole>
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		public byte SchemaId { get; set; }
		public byte FieldId { get; set; }
		public Guid TransactionId { get; set; }
		public long EntityId { get; set; }

		[Primitive]
		public object Value { get; set; }
	}
}