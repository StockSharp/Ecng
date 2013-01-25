using System.Windows.Forms;

namespace TestWinForms
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			elementHost1.Child = new UserControl1();
		}
	}
}
