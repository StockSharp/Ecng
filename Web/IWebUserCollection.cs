namespace Ecng.Web
{
	using System.Collections.Generic;

	public interface IWebUserCollection : ICollection<IWebUser>
	{
		IWebUser GetByName(string userName);
		IWebUser GetByEmail(string email);
		IWebUser GetByKey(object key);
	}
}