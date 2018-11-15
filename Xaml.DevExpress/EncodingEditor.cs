namespace Ecng.Xaml.DevExp
{
	using System.Text;
	using System.Linq;

	using DevExpress.Xpf.Editors.Settings;

	using ItemType = System.Tuple<System.Text.Encoding, string>;

	public class EncodingEditor : ComboBoxEditSettings
	{
		public EncodingEditor()
		{
			DisplayMember = nameof(ItemType.Item2);
			ValueMember = nameof(ItemType.Item1);

			ItemsSource = Encoding
				.GetEncodings()
				.Select(e => new ItemType(e.GetEncoding(), e.DisplayName))
				.ToArray();
		}
	}
}