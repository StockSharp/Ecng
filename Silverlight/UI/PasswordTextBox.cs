namespace Ecng.UI
{
	using System.Windows.Controls;

	public class PasswordTextBox : TextBox
	{
		public PasswordTextBox()
		{
			this.TextChanged += PasswordTextBox_TextChanged;
			this.KeyDown += PasswordTextBox_KeyDown;
		}

		#region Event Handlers

		public void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (base.Text.Length >= _Text.Length)
				_Text += base.Text.Substring(_Text.Length);
			DisplayMaskedCharacters();
		}

		public void PasswordTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			int cursorPosition = this.SelectionStart;
			int selectionLength = this.SelectionLength;

			// Handle Delete and Backspace Keys Appropriately
			if (e.Key == System.Windows.Input.Key.Back
				|| e.Key == System.Windows.Input.Key.Delete)
			{
				if (cursorPosition < _Text.Length)
					_Text = _Text.Remove(cursorPosition, (selectionLength > 0 ? selectionLength : 1));
			}

			base.Text = _Text;
			this.Select((cursorPosition > _Text.Length ? _Text.Length : cursorPosition), 0);
			DisplayMaskedCharacters();
		}

		#endregion

		#region Private Methods

		private void DisplayMaskedCharacters()
		{
			int cursorPosition = this.SelectionStart;

			// This changes the Text property of the base TextBox class to display all Asterisks in the control
			base.Text = new string(this.PasswordChar, _Text.Length);

			this.Select((cursorPosition > _Text.Length ? _Text.Length : cursorPosition), 0);
		}

		#endregion

		#region Properties

		private string _Text = string.Empty;
		/// <summary>
		/// The text associated with the control.
		/// </summary>
		public new string Text
		{
			get { return _Text; }
			set
			{
				_Text = value;
				DisplayMaskedCharacters();
			}
		}

		private char _passwordChar = '*';
		/// <summary>
		/// Indicates the character to display for password input.
		/// </summary>
		public char PasswordChar
		{
			get { return _passwordChar; }
			set { _passwordChar = value; }
		}

		#endregion
	}
}
