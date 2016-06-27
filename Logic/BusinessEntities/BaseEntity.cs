namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	sealed class AuditPageLoadAttribute : RelationManyAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			UnderlyingListType = typeof(BaseEntityAuditList<,>).Make(field.Type.GetGenericArguments());
			return base.GetFactoryType(field);
		}
	}

	[Serializable]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	public abstract class BaseEntity<TUser, TRole> : Equatable<BaseEntity<TUser, TRole>>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		protected BaseEntity()
		{
			Id = -1;
			CreationDate = ModificationDate = DateTime.Now;
		}

		[Identity]
		[Field("Id", ReadOnly = true)]
		public virtual long Id { get; set; }

		[AuditField((byte)AuditFields.CreationDate)]
		public DateTime CreationDate { get; set; }

		//[AuditField((byte)AuditFields.ModificationDate)]
		public DateTime ModificationDate { get; set; }

		[AuditField((byte)AuditFields.Deleted)]
		public bool Deleted { get; set; }

		[RelationSingle]
		[AuditField((byte)AuditFields.User)]
		public TUser User { get; set; }

		[NonSerialized]
		private AuditList<TUser, TRole> _changes;

		[AuditPageLoad]
		[IgnoreDataMember]
		public AuditList<TUser, TRole> Changes
		{
			get { return _changes; }
			protected set { _changes = value; }
		}

		#region Equatable<BaseEntity<TUser, TRole>> Members

		protected override bool OnEquals(BaseEntity<TUser, TRole> other)
		{
			if (Id != -1)
				return Id == other.Id;
			else
				return object.ReferenceEquals(this, other);
		}

		#endregion

		#region Cloneable<BaseEntity<TUser, TRole>> Members

		public override BaseEntity<TUser, TRole> Clone()
		{
			throw new NotImplementedException();
		}

		#endregion

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override string ToString()
		{
			return "{{Type={0}, Id={1}}}".Put(GetType(), Id);
		}
	}
}