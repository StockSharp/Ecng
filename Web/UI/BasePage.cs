namespace Ecng.Web.UI
{
	#region Using Directives

	using System.Web.UI;

	#endregion

	public abstract class BasePage : Page
	{
		protected E GetDataItem<E>()
		{
			return (E)base.GetDataItem();
		}
	}
}