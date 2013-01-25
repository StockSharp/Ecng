namespace Ecng.Web
{
	public interface IWebFile
	{
		string Name { get; }
		byte[] Body { get; }
	}

	public class WebFile : IWebFile
	{
		public string Name { get; set; }
		public byte[] Body { get; set; }
	}
}