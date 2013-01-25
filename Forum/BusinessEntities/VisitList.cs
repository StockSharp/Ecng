namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	public class VisitList : ForumBaseEntityList<Visit>
	{
		public VisitList(IStorage storage)
			: base(storage)
		{
		}

		public VisitList ByPage(byte pageId)
		{
			return new PageIdVisitList(Database, pageId);
		}

		public VisitList ByIpAddress(IPAddress address)
		{
			return new IpAddressVisitList(Database, address);
		}
	}

	class PageIdVisitList : VisitList
	{
		public PageIdVisitList(IStorage storage, byte pageId)
			: base(storage)
		{
			AddFilter(new Tuple<string, object>("PageId", pageId));
		}
	}

	class IpAddressVisitList : VisitList
	{
		public IpAddressVisitList(IStorage storage, IPAddress address)
			: base(storage)
		{
			AddFilter(new Tuple<string, object>("IpAddress", address.To<long>()));
		}
	}

	class UserVisitList : VisitList
	{
		public UserVisitList(IStorage storage, ForumUser user)
			: base(storage)
		{
			AddFilter(user);
		}
	}
}