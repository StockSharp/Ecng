using System.IO;
using System.Text;
using System.Windows;

namespace Ecng.Xaml.AutoUpdater {
    /// <summary>
    /// Interaction logic for DetailsDialog.xaml
    /// </summary>
    partial class DetailsDialog : Window {
        public static readonly DependencyProperty DetailsTitleProperty = DependencyProperty.Register(nameof(DetailsTitle), typeof(string), typeof(DetailsDialog), new PropertyMetadata(null));
        public static readonly DependencyProperty DetailsTextProperty = DependencyProperty.Register(nameof(DetailsText), typeof(string), typeof(DetailsDialog), new PropertyMetadata(null));
        public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(DetailsDialog), new PropertyMetadata(null));
        public static readonly DependencyProperty CloseTextProperty = DependencyProperty.Register(nameof(CloseText), typeof(string), typeof(DetailsDialog), new PropertyMetadata(null));
        public static readonly DependencyProperty ActionButtonVisibilityProperty = DependencyProperty.Register(nameof(ActionButtonVisibility), typeof(Visibility), typeof(DetailsDialog), new PropertyMetadata(Visibility.Visible));

        public string DetailsTitle {get => (string)GetValue(DetailsTitleProperty); set => SetValue(DetailsTitleProperty, value);}
        public string DetailsText {get => (string)GetValue(DetailsTextProperty); set => SetValue(DetailsTextProperty, value);}
        public string ActionText {get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value);}
        public string CloseText {get => (string)GetValue(CloseTextProperty); set => SetValue(CloseTextProperty, value);}
        public Visibility ActionButtonVisibility {get => (Visibility)GetValue(ActionButtonVisibilityProperty); set => SetValue(ActionButtonVisibilityProperty, value);}

        public bool IsActionSelected => DialogResult == true;

        protected DetailsDialog() {
            InitializeComponent();
        }

        void ActionButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        void CloseButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        protected void SetDetailsText(string txt, bool isRtf) {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(txt));
            _richTextBox.Document.Blocks.Clear();
            _richTextBox.Selection.Load(stream, isRtf ? DataFormats.Rtf : DataFormats.Text);
        }
    }

    class ChangesDialog : DetailsDialog {
        public bool UpdateNow => IsActionSelected;

        public ChangesDialog(string version, string changes, bool isRTF, bool showUpdateNow, AUTranslation translation) {
            SetDetailsText(changes, isRTF);

            var title = translation.ChangesInVersion.Replace("%version%", version);

            Title = title;
            DetailsTitle = title + ":";

            ActionText = translation.UpdateNowButton;
            CloseText = translation.CloseButton;

            // update now
            if(!showUpdateNow)
                ActionButtonVisibility = Visibility.Collapsed;
        }
    }

    class ErrorDialog : DetailsDialog {
        public bool TryAgainLater => IsActionSelected;

        public ErrorDialog(FailArgs failArgs, AUTranslation translation) {
            if(!string.IsNullOrEmpty(failArgs.ErrorMessage)) {
                SetDetailsText(failArgs.ErrorMessage, false);
            } else {
                _richTextBox.Visibility = Visibility.Collapsed;
            }

            Title = translation.ErrorTitle;
            CloseText = translation.CloseButton;
            ActionText = translation.TryAgainLater;
            DetailsTitle = failArgs.ErrorTitle;
        }
    }
}
