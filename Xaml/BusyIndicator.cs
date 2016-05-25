namespace Ecng.Xaml
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Shapes;

	[StyleTypedProperty(Property = "OverlayStyle", StyleTargetType = typeof(Rectangle))]
	public class BusyIndicator : ContentControl
	{
		private const string _stateBusy = "Busy";
		private const string _stateIdle = "Idle";
		private const string _stateVisible = "Visible";
		private const string _stateHidden = "Hidden";

		#region IsBusy

		public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool), typeof(BusyIndicator), new PropertyMetadata(false, OnIsBusyChanged));

		public bool IsBusy
		{
			get { return (bool)GetValue(IsBusyProperty); }
			set { SetValue(IsBusyProperty, value); }
		}

		private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((BusyIndicator)d).OnIsBusyChanged(e);
		}

		protected virtual void OnIsBusyChanged(DependencyPropertyChangedEventArgs e)
		{
			IsContentVisible = IsBusy;
			ChangeVisualState(true);
		}

		#endregion

		#region Busy Content

		public static readonly DependencyProperty BusyContentProperty = DependencyProperty.Register("BusyContent", typeof(object), typeof(BusyIndicator), new PropertyMetadata(null));

		public object BusyContent
		{
			get { return GetValue(BusyContentProperty); }
			set { SetValue(BusyContentProperty, value); }
		}

		#endregion

		#region Busy Content Template

		public static readonly DependencyProperty BusyContentTemplateProperty = DependencyProperty.Register("BusyContentTemplate", typeof(DataTemplate), typeof(BusyIndicator), new PropertyMetadata(null));

		public DataTemplate BusyContentTemplate
		{
			get { return (DataTemplate)GetValue(BusyContentTemplateProperty); }
			set { SetValue(BusyContentTemplateProperty, value); }
		}

		#endregion

		#region Overlay Style

		public static readonly DependencyProperty OverlayStyleProperty = DependencyProperty.Register("OverlayStyle", typeof(Style), typeof(BusyIndicator), new PropertyMetadata(null));

		public Style OverlayStyle
		{
			get { return (Style)GetValue(OverlayStyleProperty); }
			set { SetValue(OverlayStyleProperty, value); }
		}

		#endregion

		static BusyIndicator()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyIndicator), new FrameworkPropertyMetadata(typeof(BusyIndicator)));
		}

		protected bool IsContentVisible { get; set; }

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ChangeVisualState(false);
		}

		protected virtual void ChangeVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, IsBusy ? _stateBusy : _stateIdle, useTransitions);
			VisualStateManager.GoToState(this, IsContentVisible ? _stateVisible : _stateHidden, useTransitions);
		}
	}
}
