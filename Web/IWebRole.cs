namespace Ecng.Web
{
	public interface IWebRole
	{
		string Name { get; set; }
		IWebUserCollection Users { get; }
	}
}