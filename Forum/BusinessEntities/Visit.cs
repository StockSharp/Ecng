namespace Ecng.Forum.BusinessEntities
{
	using System.Net;

	using Ecng.Serialization;

	[Ignore(FieldName = "ModificationDate")]
	[Ignore(FieldName = "Deleted")]
	[Ignore(FieldName = "Changes")]
	public class Visit : ForumBaseEntity
	{
		public byte PageId { get; set; }

		[Nullable]
		public byte? PrevPageId { get; set; }

		[IpAddress]
		public IPAddress IpAddress { get; set; }

		[Primitive]
		public string QueryString { get; set; }

		[Primitive]
		public string UrlReferrer { get; set; }

		[Primitive]
		public string UserAgent { get; set; }
	}
}