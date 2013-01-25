namespace Ecng.Forum.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public class DownloadList : ForumBaseEntityList<Download>
	{
		public DownloadList(IStorage storage)
			: base(storage)
		{
		}
	}

	class FileDownloadList : DownloadList
	{
		public FileDownloadList(IStorage storage)
			: base(storage)
		{
		}

		public FileDownloadList(IStorage storage, File file)
			: this(storage)
		{
			AddFilter(file);
		}
	}
}