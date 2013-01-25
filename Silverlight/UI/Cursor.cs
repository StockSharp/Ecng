namespace Ecng.UI
{
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Xaml;

	public class Cursor : Control
	{
		private const string _cursorTemplate =
			 "<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
					  "<Image x:Name=\"MyCursor\" />" +
			 "</ControlTemplate>";

		private Image _cursor;

		public Cursor()
		{
			base.Template = _cursorTemplate.ToXaml<ControlTemplate>();
			base.ApplyTemplate();
		}

		public override void OnApplyTemplate()
		{
			_cursor = (Image)base.GetTemplateChild("MyCursor");
		}

		public ImageSource ImageSource
		{
			get { return _cursor.Source; }
			set { _cursor.Source = value; }
		}
	}
}