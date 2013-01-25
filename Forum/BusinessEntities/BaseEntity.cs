namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;

#if !SILVERLIGHT
	using Ecng.Data;
#endif
	using Ecng.Common;
	using Ecng.Serialization;

	#endregion

	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	public abstract class BaseEntity : Equatable<BaseEntity>
	{
		protected BaseEntity()
		{
			this.Id = -1;
#if !SILVERLIGHT
			this.CreationDate = this.ModificationDate = DateTime.Now;
#endif
		}

		[Identity]
		[Field("Id", ReadOnly = true)]
		public long Id { get; set; }

#if SILVERLIGHT
		[Ignore]
		private Guid _uniqueId = Guid.NewGuid();
#else
		[AuditField((byte)AuditFields.CreationDate)]
		public DateTime CreationDate { get; set; }

		//[AuditField((byte)AuditFields.ModificationDate)]
		public DateTime ModificationDate { get; set; }

		[AuditField((byte)AuditFields.Deleted)]
		public bool Deleted { get; set; }

		[ForumRelation]
		[AuditField((byte)AuditFields.User)]
		public User User { get; set; }

		[PageLoad(ListType = typeof(BaseEntityAuditList))]
		public AuditList Changes { get; protected set; }
#endif

		#region Equatable<BaseEntity> Members

		protected override bool OnEquals(BaseEntity other)
		{
			if (this.Id != -1)
				return this.Id == other.Id;
			else
				return object.ReferenceEquals(this, other);
		}

		#endregion

		#region Cloneable<BaseEntity> Members

		public override BaseEntity Clone()
		{
			throw new NotImplementedException();
		}

		#endregion

		public override int GetHashCode()
		{
#if SILVERLIGHT
			return _uniqueId.GetHashCode();
#else
			return this.Id.GetHashCode();
#endif
			
		}

		public override string ToString()
		{
			return "{{Type={0}, Id={1}}}".Put(base.GetType(), this.Id);
		}
	}
}