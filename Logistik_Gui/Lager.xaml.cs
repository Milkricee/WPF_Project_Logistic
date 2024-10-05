using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Logistik_Gui
{
    public partial class Lager : Window
    {
        public ObservableCollection<ArticleViewModel> Articles { get; set; } = new ObservableCollection<ArticleViewModel>();
        private readonly StringBuilder _scanBuffer = new StringBuilder();

        public Lager()
        {
            InitializeComponent();
            DataContext = this; // Set the DataContext for binding
            ArticlesPanel.ItemsSource = Articles;
        }

        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _scanBuffer.Append(BarcodeInput.Text + "\n");
                BarcodeInput.Clear();

                if (IsDataComplete(_scanBuffer.ToString()))
                {
                    ProcessScannedData(_scanBuffer.ToString().Trim());
                    _scanBuffer.Clear();
                }
            }
        }

        private bool IsDataComplete(string inputData)
        {
            return inputData.Contains("SN: ") &&
                   inputData.Contains("ArtikelNr: ") &&
                   inputData.Contains("Artikel: ") &&
                   inputData.Contains("Hersteller: ");
        }

        private void ProcessScannedData(string scannedData)
        {
            var parts = scannedData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                MessageBox.Show("Fehler beim Verarbeiten der gescannten Daten: Ungültiger Barcode-Scan.");
                return;
            }

            var serialNumber = parts[0].Split(':')[1].Trim();
            var artikelnummer = parts[1].Split(':')[1].Trim();
            var artikel = parts[2].Split(':')[1].Trim();
            var hersteller = parts[3].Split(':')[1].Trim();

            var article = Articles.FirstOrDefault(a => a.Artikelnummer == artikelnummer);
            if (article == null)
            {
                article = new ArticleViewModel
                {
                    Artikelnummer = artikelnummer,
                    Count = 0,
                    SerialNumbers = new ObservableCollection<SerialCodeData>()
                };
                Articles.Add(article);
            }

            if (article.SerialNumbers.Any(sn => sn.Seriennummer == serialNumber))
            {
                MessageBox.Show($"Fehler: Die Seriennummer '{serialNumber}' wurde bereits gescannt.");
                return;
            }

            article.SerialNumbers.Add(new SerialCodeData
            {
                Seriennummer = serialNumber,
                Artikelnummer = artikelnummer,
                Artikel = artikel,
                Hersteller = hersteller
            });

            article.Count++; // This will now trigger the PropertyChanged event
        }

        private void OnSpeichern(object sender, RoutedEventArgs e)
        {
            foreach (var article in Articles)
            {
                DatabaseHelper.SaveData(article.SerialNumbers);
            }
            Articles.Clear();
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnLagerAnzeigen(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
