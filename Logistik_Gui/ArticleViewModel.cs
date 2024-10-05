using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Logistik_Gui
{
    public class ArticleViewModel : INotifyPropertyChanged
    {
        private int _count;

        public string Artikelnummer { get; set; }
        public ObservableCollection<SerialCodeData> SerialNumbers { get; set; }

        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
