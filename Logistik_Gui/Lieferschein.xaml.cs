using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;  // Notwendig für INotifyPropertyChanged
using System.Linq;
using System.Windows.Controls;

namespace Logistik_Gui
{
    public partial class Lieferschein : UserControl, INotifyPropertyChanged
    {
        public class Position
        {
            public int Nummer { get; set; }
            public int Anzahl { get; set; }
            public string Einheit { get; set; }
            public string Artikelnr { get; set; }
            public string Bezeichnung { get; set; }
        }

        private string _lieferscheinNr;
        private string _kundenNr;
        private string _sachbearbeiter;
        private string _referenzNr;

        public string LieferscheinNr
        {
            get => _lieferscheinNr;
            set
            {
                _lieferscheinNr = value;
                OnPropertyChanged(nameof(LieferscheinNr));
            }
        }

        public string KundenNr
        {
            get => _kundenNr;
            set
            {
                _kundenNr = value;
                OnPropertyChanged(nameof(KundenNr));
            }
        }

        public string Sachbearbeiter
        {
            get => _sachbearbeiter;
            set
            {
                _sachbearbeiter = value;
                OnPropertyChanged(nameof(Sachbearbeiter));
            }
        }

        public string ReferenzNr
        {
            get => _referenzNr;
            set
            {
                _referenzNr = value;
                OnPropertyChanged(nameof(ReferenzNr));
            }
        }

        public ObservableCollection<Position> Positionen { get; set; }
        public List<string> Seriennummern { get; private set; }
        public List<string> Artikelnummern { get; private set; }

        public Lieferschein()
        {
            InitializeComponent();
            DataContext = this; // Setzt den DataContext für die Bindungen
            Positionen = new ObservableCollection<Position>();
            Seriennummern = new List<string>();
            Artikelnummern = new List<string>();
            LieferscheinListView.ItemsSource = Positionen;
        }

        public void AddScannedData(string scannedData)
        {
            Console.WriteLine($"AddScannedData called with data: {scannedData}");

            if (scannedData.StartsWith("Lieferschein Nr.:"))
            {
                LieferscheinNr = scannedData.Substring("Lieferschein Nr.:".Length).Trim();
            }
            else if (scannedData.StartsWith("Kunden-Nr.:"))
            {
                KundenNr = scannedData.Substring("Kunden-Nr.:".Length).Trim();
            }
            else if (scannedData.StartsWith("Sachbearbeiter/-in:"))
            {
                Sachbearbeiter = scannedData.Substring("Sachbearbeiter/-in:".Length).Trim();
            }
            else if (scannedData.StartsWith("Referenz-Nr.:"))
            {
                ReferenzNr = scannedData.Substring("Referenz-Nr.:".Length).Trim();
            }
            else if (scannedData.StartsWith("Pos."))
            {
                // Skip header line
                return;
            }
            else
            {
                var parts = scannedData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    try
                    {
                        int nummer = int.Parse(parts[0]);
                        int anzahl = int.Parse(parts[1]);
                        string einheit = parts[2];
                        string artikelNr = parts[3];
                        string bezeichnung = string.Join(" ", parts.Skip(4));

                        var position = new Position
                        {
                            Nummer = nummer,
                            Anzahl = anzahl,
                            Einheit = einheit,
                            Artikelnr = artikelNr,
                            Bezeichnung = bezeichnung
                        };

                        Positionen.Add(position);
                        Artikelnummern.Add(position.Artikelnr);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing scanned data: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Unexpected scanned data format.");
                }
            }
        }

        public bool ValidateSerialNumber(string artikelnummer, string seriennummer)
        {
            if (Artikelnummern.Contains(artikelnummer))
            {
                Seriennummern.Add(seriennummer);
                return true;
            }
            else
            {
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
