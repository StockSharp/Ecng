namespace Ecng.Web
{
	using System.Collections.Generic;

	public interface IWebRoleCollection : ICollection<IWebRole>
	{
		IWebRole GetByName(string roleName);
	}
}