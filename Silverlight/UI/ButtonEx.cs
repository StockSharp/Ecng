namespace Ecng.UI
{
	using System.Windows.Controls;
	using System.Windows.Input;

	public class ButtonEx : Button
	{
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			e.Handled = false;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonUp(e);
			e.Handled = false;
		}
	}
}