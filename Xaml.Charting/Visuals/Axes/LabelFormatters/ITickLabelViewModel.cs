using System.ComponentModel;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    public interface ITickLabelViewModel
    {
        string Text { get; set; }
    }

    public class DefaultTickLabelViewModel : ITickLabelViewModel, INotifyPropertyChanged
    {
        private string _text;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NumericTickLabelViewModel : DefaultTickLabelViewModel
    {
        private bool _hasExponent;
        private string _separator;
        private string _exponent;

        public bool HasExponent
        {
            get { return _hasExponent; }
            set
            {
                if (_hasExponent != value)
                {
                    _hasExponent = value;
                    OnPropertyChanged("HasExponent");
                }
            }
        }

        public string Separator
        {
            get { return _separator; }
            set
            {
                if (_separator != value)
                {
                    _separator = value;
                    OnPropertyChanged("Separator");
                }
            }
        }

        public string Exponent
        {
            get { return _exponent; }
            set
            {
                if (_exponent != value)
                {
                    _exponent = value;
                    OnPropertyChanged("Exponent");
                }
            }
        }
    }
}
