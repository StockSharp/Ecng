namespace Ecng.UI
{
	using System;
	using System.Windows.Controls;

	using Ecng.Common;

	public partial class Rating
	{
		public Rating()
		{
			InitializeComponent();
		}

		//private int _maxValue = 100;

		//public int MaxValue
		//{
		//    get { return _maxValue; }
		//    set { _maxValue = value; }
		//}

		//public bool IsEditable
		//{
		//    get { return this.Value.IsEditable; }
		//    set { this.Value.IsEditable = value; }
		//}

		public int CurrentValue
		{
			get { return (int)this.Value.Value; }
			set
			{
				if (value < 0)// || value > this.MaxValue)
					throw new ArgumentOutOfRangeException();

				//this.VoteBar.Width = (value * base.Width) / this.MaxValue;
				this.Value.Value = value;
			}
		}

		public event EventHandler<RatingEventArgs> CurrentValueChanging;

		private void NumericUpDown_ValueChanging(object sender, RoutedPropertyChangingEventArgs<double> e)
		{
			this.CurrentValueChanging.SafeInvoke(this, new RatingEventArgs((int)e.NewValue, (int)e.OldValue), args => e.Cancel = args.IsCancel);
		}
	}
}