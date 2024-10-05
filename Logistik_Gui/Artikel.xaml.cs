using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Logistik_Gui
{
    public partial class Artikel : UserControl
    {
        public ObservableCollection<ArticleViewModel> Articles { get; set; } = new ObservableCollection<ArticleViewModel>();
        private readonly StringBuilder _scanBuffer = new StringBuilder();
        private LieferscheinViewModel _confirmedLieferschein;
        private Lieferschein_CSV _lieferscheinCsv;
        private ObservableCollection<LieferscheinPosition> _lieferscheinPositionen;

        string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

        public Artikel()
        {
            InitializeComponent();
            DataContext = this;
            ArticlesPanel.ItemsSource = Articles;
        }

        public Artikel(LieferscheinViewModel confirmedLieferschein, Lieferschein_CSV lieferscheinCsv) : this()
        {
            _confirmedLieferschein = confirmedLieferschein;
            _lieferscheinCsv = lieferscheinCsv;
        }

        public void Initialize(Lieferschein_CSV lieferscheinCsv)
        {
            _lieferscheinCsv = lieferscheinCsv;
        }

        public void ConfirmLieferschein(LieferscheinViewModel confirmedLieferschein)
        {
            _confirmedLieferschein = confirmedLieferschein;
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

            // Überprüfen, ob die Artikelnummer im Lieferschein vorhanden ist
            if (_lieferscheinPositionen == null || !_lieferscheinPositionen.Any(p => p.Artikelnr == artikelnummer))
            {
                MessageBox.Show($"Fehler: Die Artikelnummer '{artikelnummer}' ist nicht im Lieferschein enthalten.");
                return;
            }

            // Überprüfen, ob die Seriennummer im Lagerbestand vorhanden ist
            if (!IsSerialNumberInStock(serialNumber))
            {
                MessageBox.Show($"Fehler: Die Seriennummer '{serialNumber}' ist nicht im Lagerbestand vorhanden.");
                return;
            }

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

            article.Count++;
            article.SerialNumbers.Add(new SerialCodeData
            {
                Nummer = article.Count,
                Seriennummer = serialNumber,
                Artikelnummer = artikelnummer,
                Artikel = artikel,
                Hersteller = hersteller
            });

            var index = Articles.IndexOf(article);
            Articles[index] = article;
        }

        private bool IsSerialNumberInStock(string serialNumber)
        {

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Seriennummern WHERE SN = @SN";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SN", serialNumber);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void ProcessInputBuffer(string inputBufferString)
        {
            var parts = inputBufferString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

            article.Count++;
            article.SerialNumbers.Add(new SerialCodeData
            {
                Nummer = article.SerialNumbers.Count + 1, // Fortlaufende Nummer setzen
                Seriennummer = serialNumber,
                Artikelnummer = artikelnummer,
                Artikel = artikel,
                Hersteller = hersteller
            });

            // Aktualisieren der UI zur korrekten Anzeige der Zählung
            var index = Articles.IndexOf(article);
            Articles[index] = article;
        }

        public void InitializeFromLieferschein(List<LieferscheinPosition> positionen)
        {
            _lieferscheinPositionen = new ObservableCollection<LieferscheinPosition>(positionen);
            Articles.Clear();
            //if (positionen == null || positionen.Count == 0)
            //{
              
            //    return;
            //}

            foreach (var position in positionen)
            {
                var article = new ArticleViewModel
                {
                    Artikelnummer = position.Artikelnr,
                    Count = 0,
                    SerialNumbers = new ObservableCollection<SerialCodeData>()
                };
                Articles.Add(article);
            }
        }

        private bool IsDataComplete(string inputData)
        {
            return inputData.Contains("SN: ") &&
                   inputData.Contains("ArtikelNr: ") &&
                   inputData.Contains("Artikel: ") &&
                   inputData.Contains("Hersteller: ");
        }

        private void OnSpeichern(object sender, RoutedEventArgs e)
        {
            // Überprüfung, ob Artikel vorhanden sind und ob mindestens eine Seriennummer in jedem Artikel vorhanden ist
            if (Articles == null || !Articles.Any() || Articles.All(a => a.SerialNumbers == null || !a.SerialNumbers.Any()))
            {
                MessageBox.Show("Es sind keine gescannten Artikel vorhanden, die gespeichert werden können.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Methode verlassen, da keine Artikel oder Seriennummern vorhanden sind
            }

            StringBuilder errorMessages = new StringBuilder();
            bool hasErrors = false;

            foreach (var article in Articles)
            {
                if (!RemoveAndLogScannedSerialNumbers(article.SerialNumbers, errorMessages))
                {
                    hasErrors = true;
                }
            }

            if (hasErrors)
            {
                MessageBox.Show($"Es sind Fehler aufgetreten:\n{errorMessages}");
            }
            else
            {
                MessageBox.Show("Daten erfolgreich gespeichert und Seriennummern entfernt!");
                Articles.Clear();
            }
        }

        private bool RemoveAndLogScannedSerialNumbers(ObservableCollection<SerialCodeData> serialNumbers, StringBuilder errorMessages)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                bool hasErrors = false;

                try
                {
                    foreach (var item in serialNumbers)
                    {
                        string queryRemoveSN = "DELETE FROM Seriennummern WHERE SN = @SN";
                        MySqlCommand cmdRemoveSN = new MySqlCommand(queryRemoveSN, conn, transaction);
                        cmdRemoveSN.Parameters.AddWithValue("@SN", item.Seriennummer);
                        try
                        {
                            cmdRemoveSN.ExecuteNonQuery();
                        }
                        catch (MySqlException ex)
                        {
                            hasErrors = true;
                            errorMessages.AppendLine($"Fehler beim Löschen der Seriennummer '{item.Seriennummer}': {ex.Message}");
                        }

                        string queryUpdateArtikel = @"
                UPDATE Artikel 
                SET Anzahl = Anzahl - 1 
                WHERE ArtikelNr = @ArtikelNr";
                        MySqlCommand cmdUpdateArtikel = new MySqlCommand(queryUpdateArtikel, conn, transaction);
                        cmdUpdateArtikel.Parameters.AddWithValue("@ArtikelNr", item.Artikelnummer);
                        cmdUpdateArtikel.ExecuteNonQuery();

                        string queryLog = @"
                INSERT INTO ausgelagerte_seriennummern (Seriennummer, Artikelnummer, LieferscheinNr)
                VALUES (@Seriennummer, @Artikelnummer, @LieferscheinNr)";
                        MySqlCommand cmdLog = new MySqlCommand(queryLog, conn, transaction);
                        cmdLog.Parameters.AddWithValue("@Seriennummer", item.Seriennummer);
                        cmdLog.Parameters.AddWithValue("@Artikelnummer", item.Artikelnummer);
                        cmdLog.Parameters.AddWithValue("@LieferscheinNr", _confirmedLieferschein.LieferscheinNr); // Lieferscheinnummer aus der bestätigten Lieferung
                        cmdLog.ExecuteNonQuery();
                    }

                    if (hasErrors)
                    {
                        transaction.Rollback();
                    }
                    else
                    {
                        transaction.Commit();
                    }
                }
                catch (MySqlException ex)
                {
                    transaction.Rollback();
                    errorMessages.AppendLine($"Fehler beim Speichern: {ex.Message}");
                    hasErrors = true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    errorMessages.AppendLine($"Ein allgemeiner Fehler ist aufgetreten: {ex.Message}");
                    hasErrors = true;
                }

                return !hasErrors;
            }
        }

        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _scanBuffer.Append(SeriennummerEingabe.Text + "\n");
                SeriennummerEingabe.Clear();

                if (IsDataComplete(_scanBuffer.ToString()))
                {
                    ProcessScannedData(_scanBuffer.ToString().Trim());
                    _scanBuffer.Clear();
                }
            }
        }

        public void SetConfirmedLieferschein(LieferscheinViewModel confirmedLieferschein)
        {
            _confirmedLieferschein = confirmedLieferschein;
        }

    }
}
