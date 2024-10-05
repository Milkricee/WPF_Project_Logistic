using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static Logistik_Gui.Lieferschein;

namespace Logistik_Gui
{
    /// <summary>
    /// Interaktionslogik für Lieferschein_CSV.xaml
    /// </summary>
    public partial class Lieferschein_CSV : UserControl
    {
        public LieferscheinViewModel ViewModel { get; set; }

        private Artikel _artikelControl;

        private LieferscheinViewModel _confirmedLieferschein;

        private ObservableCollection<string> _autocompleteSuggestions;

        private BestätigteLieferscheinDaten _bestätigteDaten;

        public Lieferschein_CSV()
        {
            InitializeComponent();
            ViewModel = new LieferscheinViewModel();
            this.DataContext = ViewModel;
            _autocompleteSuggestions = new ObservableCollection<string>();
            AutocompleteListBox.ItemsSource = _autocompleteSuggestions;
        }

        public void SetArtikelControl(Artikel artikelControl)
        {
            _artikelControl = artikelControl;
        }

        private DataTable GetDataTableFromCSVFile(string csvFilePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(csvFilePath, System.Text.Encoding.GetEncoding("ISO-8859-1")))
                {
                    string[] headers = sr.ReadLine().Split(';'); // Semikolon als Trennzeichen
                    foreach (string header in headers)
                    {
                        csvData.Columns.Add(header);
                    }
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(';'); // Semikolon als Trennzeichen
                        DataRow dr = csvData.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }
                        csvData.Rows.Add(dr);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
            return csvData;
        }

        private void Lieferscheineingabe_TextChanged(object sender, TextChangedEventArgs e)
        {
            string lieferscheinNr = RemoveDots(Lieferscheineingabe.Text);

            if (string.IsNullOrWhiteSpace(lieferscheinNr))
            {
                // Felder leeren und ListView zurücksetzen, wenn die Eingabe leer ist
                ClearFields();
                ViewModel.Positionen.Clear(); // ViewModel-Positionen ebenfalls leeren
                LieferscheinListView.ItemsSource = null; // ListView leeren

                // Artikel UserControl leeren, falls es initialisiert wurde
                _artikelControl?.InitializeFromLieferschein(new List<LieferscheinPosition>());

                AutocompleteListBox.Visibility = Visibility.Collapsed;
                return;
            }

            UpdateAutocompleteSuggestions(lieferscheinNr);
        }

        private string RemoveDots(string input)
        {
            return input.Replace(".", "");
        }

        private void UpdateAutocompleteSuggestions(string input)
        {
            string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT DISTINCT LieferscheinNr FROM export WHERE REPLACE(LieferscheinNr, '.', '') LIKE @input LIMIT 10";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@input", input + "%");

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    _autocompleteSuggestions.Clear();
                    while (reader.Read())
                    {
                        _autocompleteSuggestions.Add(reader.GetString("LieferscheinNr"));
                    }
                }
            }

            AutocompleteListBox.Visibility = _autocompleteSuggestions.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AutocompleteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AutocompleteListBox.SelectedItem is string selectedLieferscheinNr)
            {
                Lieferscheineingabe.Text = selectedLieferscheinNr;
                AutocompleteListBox.Visibility = Visibility.Collapsed;
                GetLieferscheinData(selectedLieferscheinNr);
            }
        }

        private void GetLieferscheinData(string lieferscheinNr)
        {
            string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM export WHERE LieferscheinNr = @LieferscheinNr";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@LieferscheinNr", lieferscheinNr);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {  
                        // Hier die Datenfelder entsprechend setzen
                        LieferscheinNr.Content = RemoveDots(reader["LieferscheinNr"].ToString());
                        KundenNr.Content =RemoveDots( reader["KundenNr"].ToString());
                        Sachbearbeiter.Content = reader["SachbearbeiterIn"].ToString();
                        ReferenzNr.Content = reader["ReferenzNr d. Kd."].ToString();

                        // Hier den Code zum Abrufen der Positionen hinzufügen und das ListView füllen
                        Console.WriteLine("LoadPositionen wird aufgerufen."); // Debug-Ausgabe
                        LoadPositionen(lieferscheinNr);
                    }
                    else
                    {
                        // Leere Felder, wenn keine passenden Daten gefunden wurden
                        ClearFields();
                    }
                }
            }
        }

        private void LoadPositionen(string lieferscheinNr)
        {
            string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM export WHERE LieferscheinNr = @LieferscheinNr";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@LieferscheinNr", lieferscheinNr);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    ViewModel.Positionen.Clear();

                    while (reader.Read())
                    {
                        var position = new LieferscheinPosition
                        {
                            Nummer = reader.GetInt32("P_PositionsNr"),
                            Anzahl = reader.GetDecimal("P_Anzahl"),
                            Einheit = reader.GetString("P_Einheit"),
                            Artikelnr = reader.GetString("P_ArtikelNr").Trim(), // Trim the Artikelnummer
                            Bezeichnung = reader.GetString("P_Artikelkategorie")
                        };

                        ViewModel.Positionen.Add(position);
                        Console.WriteLine($"Artikelnummer '{position.Artikelnr}' geladen.");
                    }
                }
            }

            LieferscheinListView.ItemsSource = ViewModel.Positionen;
        }

        private void ClearFields()
        {
            LieferscheinNr.Content = string.Empty;
            KundenNr.Content = string.Empty;
            Sachbearbeiter.Content = string.Empty;
            ReferenzNr.Content = string.Empty;

            // Hier das ListView leeren
            LieferscheinListView.ItemsSource = null;
        }

        public string ConvertToDecimalFormat(string inputValue)
        {
            if (string.IsNullOrEmpty(inputValue))
            {
                return "0"; // oder ein geeigneter Standardwert
            }
            return inputValue.Replace(',', '.');
        }

        private void OnLieferscheinBestätigen(object sender, RoutedEventArgs e)
        {
            // Überprüfung, ob die Lieferscheinnummer vorhanden ist
            if (LieferscheinNr?.Content == null || string.IsNullOrEmpty(LieferscheinNr.Content.ToString()))
            {
                MessageBox.Show("Lieferscheinnummer fehlt oder wurde nicht gefunden.");
                return; // Methode verlassen, da die Lieferscheinnummer erforderlich ist
            }

            // Sicherstellen, dass Positionen im ViewModel vorhanden sind
            if (ViewModel?.Positionen == null || !ViewModel.Positionen.Any())
            {
                MessageBox.Show("Es gibt keine Positionen im Lieferschein.");
                return;
            }

            // Zuweisung der bestätigten Daten
            _bestätigteDaten = new BestätigteLieferscheinDaten
            {
                LieferscheinNr = LieferscheinNr.Content.ToString(),
                KundenNr = KundenNr.Content?.ToString() ?? string.Empty,
                Sachbearbeiter = Sachbearbeiter.Content?.ToString() ?? string.Empty,
                ReferenzNr = ReferenzNr.Content?.ToString() ?? string.Empty,
                Positionen = ViewModel.Positionen.ToList()
            };

            _confirmedLieferschein = new LieferscheinViewModel
            {
                LieferscheinNr = LieferscheinNr.Content.ToString()
            };

            // Positionen hinzufügen
            foreach (var position in ViewModel.Positionen)
            {
                _confirmedLieferschein.AddLieferscheinPosition(position);
            }

            MessageBox.Show("Lieferscheindaten wurden bestätigt und zwischengespeichert.");

            // Übergabe der Artikelnummern und Positionen an das Artikel UserControl
            _artikelControl?.InitializeFromLieferschein(ViewModel.Positionen.ToList());
            _artikelControl?.SetConfirmedLieferschein(_confirmedLieferschein); // Übergabe der bestätigten Lieferung
        }

        public void SaveLieferscheinData(MySqlConnection conn, MySqlTransaction transaction)
        {
            // Lieferschein einfügen
            string insertLieferscheinQuery = @"
                INSERT INTO Lieferscheine (LieferscheinNr, KundenNr, Sachbearbeiter, ReferenzNr)
                VALUES (@LieferscheinNr, @KundenNr, @Sachbearbeiter, @ReferenzNr)
                ON DUPLICATE KEY UPDATE
                KundenNr = VALUES(KundenNr),
                Sachbearbeiter = VALUES(Sachbearbeiter),
                ReferenzNr = VALUES(ReferenzNr)";

            MySqlCommand cmd = new MySqlCommand(insertLieferscheinQuery, conn, transaction);
            cmd.Parameters.AddWithValue("@LieferscheinNr", LieferscheinNr.Content.ToString());
            cmd.Parameters.AddWithValue("@KundenNr", KundenNr.Content.ToString());
            cmd.Parameters.AddWithValue("@Sachbearbeiter", Sachbearbeiter.Content.ToString());
            cmd.Parameters.AddWithValue("@ReferenzNr", ReferenzNr.Content.ToString());
            cmd.ExecuteNonQuery();

            long lieferscheinID = cmd.LastInsertedId;

            // Positionen des Lieferscheins einfügen
            string insertPositionQuery = @"
                INSERT INTO LieferscheinPositionen (LieferscheinID, Nummer, Anzahl, Einheit, Artikelnr, Bezeichnung)
                VALUES (@LieferscheinID, @Nummer, @Anzahl, @Einheit, @Artikelnr, @Bezeichnung)
                ON DUPLICATE KEY UPDATE
                Anzahl = VALUES(Anzahl),
                Einheit = VALUES(Einheit),
                Bezeichnung = VALUES(Bezeichnung)";

            foreach (var position in ViewModel.Positionen)
            {
                MySqlCommand posCmd = new MySqlCommand(insertPositionQuery, conn, transaction);
                posCmd.Parameters.AddWithValue("@LieferscheinID", lieferscheinID);
                posCmd.Parameters.AddWithValue("@Nummer", position.Nummer);
                posCmd.Parameters.AddWithValue("@Anzahl", position.Anzahl);
                posCmd.Parameters.AddWithValue("@Einheit", position.Einheit);
                posCmd.Parameters.AddWithValue("@Artikelnr", position.Artikelnr);
                posCmd.Parameters.AddWithValue("@Bezeichnung", position.Bezeichnung);
                posCmd.ExecuteNonQuery();
            }
        }

        public void AutoImportCSV()
        {
            string filePath = @"C:\Users\Daniel_Miedreich\Desktop\von Paul\Export.csv";
            string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

            if (File.Exists(filePath))
            {
                try
                {
                    DataTable csvData = GetDataTableFromCSVFile(filePath);

                    // Debugging: Überprüfe die Spaltennamen in der DataTable
                    foreach (DataColumn column in csvData.Columns)
                    {
                        Console.WriteLine("Spalte: " + column.ColumnName);
                    }

                    // Debugging: Überprüfe die erste Zeile in der DataTable
                    if (csvData.Rows.Count > 0)
                    {
                        DataRow firstRow = csvData.Rows[0];
                        foreach (var item in firstRow.ItemArray)
                        {
                            Console.WriteLine("Wert: " + item.ToString());
                        }
                    }

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        foreach (DataRow row in csvData.Rows)
                        {
                            string query = @"
                              INSERT INTO export (
                              DELID, LieferscheinNr, Lieferscheindatum, Auftragsart, Status, SachbearbeiterIn, KundenNr, Kundenart, Kundenkategorie, 
                              Quelle, Anrede, Titel, Nachname_Firmenname, Vorname, Namenszusatz, Strasse, PLZ, Ort, Land, 
                              ASP_Anrede, ASP_Titel, ASP_Vorname, ASP_Nachname, AuftragsNr, `ReferenzNr d. Kd.`, `ReferenzDatum d. Kd.`, Lieferbedingung, 
                              LetzteVorlage, Anmerkungen, IndividuellesFeld1, IndividuellesFeld2, IndividuellesFeld3, IndividuellesFeld4, 
                              IndividuellesFeld5, IndividuellesFeld6, IndividuellesFeld7, IndividuellesFeld8, IndividuellesFeld9, 
                              IndividuellesFeld10, IndividuellesFeld11, IndividuellesFeld12, IndividuellesFeld13, IndividuellesFeld14, 
                              IndividuellesFeld15, IndividuellesFeld16, IndividuellesFeld17, IndividuellesFeld18, IndividuellesFeld19, 
                              IndividuellesFeld20, Projekt, Archiviert, Verwendung, Tabellenkategorie, LetzteVerarbeitung, P_PositionsNr, 
                              P_Anzahl, P_ArtikelNr, P_Artikelkategorie, P_ArtikelNrDesKd, P_Eingabemenge, P_EingabeWert1, P_EingabeWert2, 
                              P_EingabeWert3, P_EingabeWert4, P_EingabeWert5, P_RabattProzent, P_RabattBetrag, P_EinzelpreisNetto, 
                              P_EinzelpreisNachRabattNetto, P_Einkaufspreis, P_EinzelpreisBrutto, P_EinzelpreisNachRabattBrutto, 
                              P_MwStProzenz, P_AnzahlAbgerechnet, P_KomplettAbgerechnet, P_Einheit, P_Gewicht, P_Volumen, P_IndividuellesFeld1, 
                              P_IndividuellesFeld2, P_IndividuellesFeld3, P_IndividuellesFeld4, P_IndividuellesFeld5, P_IndividuellesFeld6, 
                              P_IndividuellesFeld7, P_IndividuellesFeld8, P_IndividuellesFeld9, P_IndividuellesFeld10, P_Positionsart, 
                              P_LohnnteilNetto, P_LohnnteilBrutto, `P EAN`, P_AuftragsNr, `P_ReferenzNr d. Kd.`, `P_ReferenzDatum d. Kd.`
                          ) VALUES (
                              @DELID, @LieferscheinNr, @Lieferscheindatum, @Auftragsart, @Status, @SachbearbeiterIn, @KundenNr, @Kundenart, @Kundenkategorie, 
                              @Quelle, @Anrede, @Titel, @Nachname_Firmenname, @Vorname, @Namenszusatz, @Strasse, @PLZ, @Ort, @Land, 
                              @ASP_Anrede, @ASP_Titel, @ASP_Vorname, @ASP_Nachname, @AuftragsNr, @ReferenzNr_d_Kd, @ReferenzDatum_d_Kd, @Lieferbedingung, 
                              @LetzteVorlage, @Anmerkungen, @IndividuellesFeld1, @IndividuellesFeld2, @IndividuellesFeld3, @IndividuellesFeld4, 
                              @IndividuellesFeld5, @IndividuellesFeld6, @IndividuellesFeld7, @IndividuellesFeld8, @IndividuellesFeld9, 
                              @IndividuellesFeld10, @IndividuellesFeld11, @IndividuellesFeld12, @IndividuellesFeld13, @IndividuellesFeld14, 
                              @IndividuellesFeld15, @IndividuellesFeld16, @IndividuellesFeld17, @IndividuellesFeld18, @IndividuellesFeld19, 
                              @IndividuellesFeld20, @Projekt, @Archiviert, @Verwendung, @Tabellenkategorie, @LetzteVerarbeitung, @P_PositionsNr, 
                              @P_Anzahl, @P_ArtikelNr, @P_Artikelkategorie, @P_ArtikelNrDesKd, @P_Eingabemenge, @P_EingabeWert1, @P_EingabeWert2, 
                              @P_EingabeWert3, @P_EingabeWert4, @P_EingabeWert5, @P_RabattProzent, @P_RabattBetrag, @P_EinzelpreisNetto, 
                              @P_EinzelpreisNachRabattNetto, @P_Einkaufspreis, @P_EinzelpreisBrutto, @P_EinzelpreisNachRabattBrutto, 
                              @P_MwStProzenz, @P_AnzahlAbgerechnet, @P_KomplettAbgerechnet, @P_Einheit, @P_Gewicht, @P_Volumen, @P_IndividuellesFeld1, 
                              @P_IndividuellesFeld2, @P_IndividuellesFeld3, @P_IndividuellesFeld4, @P_IndividuellesFeld5, @P_IndividuellesFeld6, 
                              @P_IndividuellesFeld7, @P_IndividuellesFeld8, @P_IndividuellesFeld9, @P_IndividuellesFeld10, @P_Positionsart, 
                              @P_LohnnteilNetto, @P_LohnnteilBrutto, @P_EAN, @P_AuftragsNr, @P_ReferenzNr_d_Kd, @P_ReferenzDatum_d_Kd
                          ) ON DUPLICATE KEY UPDATE
                              Lieferscheindatum = VALUES(Lieferscheindatum),
                              Auftragsart = VALUES(Auftragsart),
                              Status = VALUES(Status),
                              SachbearbeiterIn = VALUES(SachbearbeiterIn),
                              KundenNr = VALUES(KundenNr),
                              Kundenart = VALUES(Kundenart),
                              Kundenkategorie = VALUES(Kundenkategorie),
                              Quelle = VALUES(Quelle),
                              Anrede = VALUES(Anrede),
                              Titel = VALUES(Titel),
                              Nachname_Firmenname = VALUES(Nachname_Firmenname),
                              Vorname = VALUES(Vorname),
                              Namenszusatz = VALUES(Namenszusatz),
                              Strasse = VALUES(Strasse),
                              PLZ = VALUES(PLZ),
                              Ort = VALUES(Ort),
                              Land = VALUES(Land),
                              ASP_Anrede = VALUES(ASP_Anrede),
                              ASP_Titel = VALUES(ASP_Titel),
                              ASP_Vorname = VALUES(ASP_Vorname),
                              ASP_Nachname = VALUES(ASP_Nachname),
                              `ReferenzNr d. Kd.` = VALUES(`ReferenzNr d. Kd.`),
                              `ReferenzDatum d. Kd.` = VALUES(`ReferenzDatum d. Kd.`),
                              Lieferbedingung = VALUES(Lieferbedingung),
                              LetzteVorlage = VALUES(LetzteVorlage),
                              Anmerkungen = VALUES(Anmerkungen),
                              IndividuellesFeld1 = VALUES(IndividuellesFeld1),
                              IndividuellesFeld2 = VALUES(IndividuellesFeld2),
                              IndividuellesFeld3 = VALUES(IndividuellesFeld3),
                              IndividuellesFeld4 = VALUES(IndividuellesFeld4),
                              IndividuellesFeld5 = VALUES(IndividuellesFeld5),
                              IndividuellesFeld6 = VALUES(IndividuellesFeld6),
                              IndividuellesFeld7 = VALUES(IndividuellesFeld7),
                              IndividuellesFeld8 = VALUES(IndividuellesFeld8),
                              IndividuellesFeld9 = VALUES(IndividuellesFeld9),
                              IndividuellesFeld10 = VALUES(IndividuellesFeld10),
                              IndividuellesFeld11 = VALUES(IndividuellesFeld11),
                              IndividuellesFeld12 = VALUES(IndividuellesFeld12),
                              IndividuellesFeld13 = VALUES(IndividuellesFeld13),
                              IndividuellesFeld14 = VALUES(IndividuellesFeld14),
                              IndividuellesFeld15 = VALUES(IndividuellesFeld15),
                              IndividuellesFeld16 = VALUES(IndividuellesFeld16),
                              IndividuellesFeld17 = VALUES(IndividuellesFeld17),
                              IndividuellesFeld18 = VALUES(IndividuellesFeld18),
                              IndividuellesFeld19 = VALUES(IndividuellesFeld19),
                              IndividuellesFeld20 = VALUES(IndividuellesFeld20),
                              Projekt = VALUES(Projekt),
                              Archiviert = VALUES(Archiviert),
                              Verwendung = VALUES(Verwendung),
                              Tabellenkategorie = VALUES(Tabellenkategorie),
                              LetzteVerarbeitung = VALUES(LetzteVerarbeitung),
                              P_Anzahl = VALUES(P_Anzahl),
                              P_ArtikelNr = VALUES(P_ArtikelNr),
                              P_Artikelkategorie = VALUES(P_Artikelkategorie),
                              P_ArtikelNrDesKd = VALUES(P_ArtikelNrDesKd),
                              P_Eingabemenge = VALUES(P_Eingabemenge),
                              P_EingabeWert1 = VALUES(P_EingabeWert1),
                              P_EingabeWert2 = VALUES(P_EingabeWert2),
                              P_EingabeWert3 = VALUES(P_EingabeWert3),
                              P_EingabeWert4 = VALUES(P_EingabeWert4),
                              P_EingabeWert5 = VALUES(P_EingabeWert5),
                              P_RabattProzent = VALUES(P_RabattProzent),
                              P_RabattBetrag = VALUES(P_RabattBetrag),
                              P_EinzelpreisNetto = VALUES(P_EinzelpreisNetto),
                              P_EinzelpreisNachRabattNetto = VALUES(P_EinzelpreisNachRabattNetto),
                              P_Einkaufspreis = VALUES(P_Einkaufspreis),
                              P_EinzelpreisBrutto = VALUES(P_EinzelpreisBrutto),
                              P_EinzelpreisNachRabattBrutto = VALUES(P_EinzelpreisNachRabattBrutto),
                              P_MwStProzenz = VALUES(P_MwStProzenz),
                              P_AnzahlAbgerechnet = VALUES(P_AnzahlAbgerechnet),
                              P_KomplettAbgerechnet = VALUES(P_KomplettAbgerechnet),
                              P_Einheit = VALUES(P_Einheit),
                              P_Gewicht = VALUES(P_Gewicht),
                              P_Volumen = VALUES(P_Volumen),
                              P_IndividuellesFeld1 = VALUES(P_IndividuellesFeld1),
                              P_IndividuellesFeld2 = VALUES(P_IndividuellesFeld2),
                              P_IndividuellesFeld3 = VALUES(P_IndividuellesFeld3),
                              P_IndividuellesFeld4 = VALUES(P_IndividuellesFeld4),
                              P_IndividuellesFeld5 = VALUES(P_IndividuellesFeld5),
                              P_IndividuellesFeld6 = VALUES(P_IndividuellesFeld6),
                              P_IndividuellesFeld7 = VALUES(P_IndividuellesFeld7),
                              P_IndividuellesFeld8 = VALUES(P_IndividuellesFeld8),
                              P_IndividuellesFeld9 = VALUES(P_IndividuellesFeld9),
                              P_IndividuellesFeld10 = VALUES(P_IndividuellesFeld10),
                              P_Positionsart = VALUES(P_Positionsart),
                              P_LohnnteilNetto = VALUES(P_LohnnteilNetto),
                              P_LohnnteilBrutto = VALUES(P_LohnnteilBrutto),
                              `P EAN` = VALUES(`P EAN`),
                              P_AuftragsNr = VALUES(P_AuftragsNr),
                              `P_ReferenzNr d. Kd.` = VALUES(`P_ReferenzNr d. Kd.`),
                              `P_ReferenzDatum d. Kd.` = VALUES(`P_ReferenzDatum d. Kd.`)";


                            MySqlCommand cmd = new MySqlCommand(query, conn);

                            cmd.Parameters.AddWithValue("@DELID", row["DELID"]);
                            cmd.Parameters.AddWithValue("@LieferscheinNr", row["LieferscheinNr"]);
                            cmd.Parameters.AddWithValue("@Lieferscheindatum", row["Lieferscheindatum"]);
                            cmd.Parameters.AddWithValue("@Auftragsart", row["Auftragsart"]);
                            cmd.Parameters.AddWithValue("@Status", row["Status"]);
                            cmd.Parameters.AddWithValue("@SachbearbeiterIn", row["SachbearbeiterIn"]);
                            cmd.Parameters.AddWithValue("@KundenNr", row["KundenNr"]);
                            cmd.Parameters.AddWithValue("@Kundenart", row["Kundenart"]);
                            cmd.Parameters.AddWithValue("@Kundenkategorie", row["Kundenkategorie"]);
                            cmd.Parameters.AddWithValue("@Quelle", row["Quelle"]);
                            cmd.Parameters.AddWithValue("@Anrede", row["Anrede"]);
                            cmd.Parameters.AddWithValue("@Titel", row["Titel"]);
                            cmd.Parameters.AddWithValue("@Nachname_Firmenname", row["Nachname_Firmenname"]);
                            cmd.Parameters.AddWithValue("@Vorname", row["Vorname"]);
                            cmd.Parameters.AddWithValue("@Namenszusatz", row["Namenszusatz"]);
                            cmd.Parameters.AddWithValue("@Strasse", row["Strasse"]);
                            cmd.Parameters.AddWithValue("@PLZ", row["PLZ"]);
                            cmd.Parameters.AddWithValue("@Ort", row["Ort"]);
                            cmd.Parameters.AddWithValue("@Land", row["Land"]);
                            cmd.Parameters.AddWithValue("@ASP_Anrede", row["ASP_Anrede"]);
                            cmd.Parameters.AddWithValue("@ASP_Titel", row["ASP_Titel"]);
                            cmd.Parameters.AddWithValue("@ASP_Vorname", row["ASP_Vorname"]);
                            cmd.Parameters.AddWithValue("@ASP_Nachname", row["ASP_Nachname"]);
                            cmd.Parameters.AddWithValue("@AuftragsNr", row["AuftragsNr"]);
                            cmd.Parameters.AddWithValue("@ReferenzNr_d_Kd", row["ReferenzNr d. Kd."]);
                            cmd.Parameters.AddWithValue("@ReferenzDatum_d_Kd", row["ReferenzDatum d. Kd."]);
                            cmd.Parameters.AddWithValue("@Lieferbedingung", row["Lieferbedingung"]);
                            cmd.Parameters.AddWithValue("@LetzteVorlage", row["LetzteVorlage"]);
                            cmd.Parameters.AddWithValue("@Anmerkungen", row["Anmerkungen"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld1", row["IndividuellesFeld1"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld2", row["IndividuellesFeld2"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld3", row["IndividuellesFeld3"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld4", row["IndividuellesFeld4"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld5", row["IndividuellesFeld5"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld6", row["IndividuellesFeld6"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld7", row["IndividuellesFeld7"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld8", row["IndividuellesFeld8"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld9", row["IndividuellesFeld9"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld10", row["IndividuellesFeld10"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld11", row["IndividuellesFeld11"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld12", row["IndividuellesFeld12"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld13", row["IndividuellesFeld13"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld14", row["IndividuellesFeld14"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld15", row["IndividuellesFeld15"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld16", row["IndividuellesFeld16"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld17", row["IndividuellesFeld17"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld18", row["IndividuellesFeld18"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld19", row["IndividuellesFeld19"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld20", row["IndividuellesFeld20"]);
                            cmd.Parameters.AddWithValue("@Projekt", row["Projekt"]);
                            cmd.Parameters.AddWithValue("@Archiviert", row["Archiviert"]);
                            cmd.Parameters.AddWithValue("@Verwendung", row["Verwendung"]);
                            cmd.Parameters.AddWithValue("@Tabellenkategorie", row["Tabellenkategorie"]);
                            cmd.Parameters.AddWithValue("@LetzteVerarbeitung", row["LetzteVerarbeitung"]);
                            cmd.Parameters.AddWithValue("@P_PositionsNr", row["P_PositionsNr"]);
                            cmd.Parameters.AddWithValue("@P_Anzahl", ConvertToDecimalFormat(row["P_Anzahl"].ToString()));
                            cmd.Parameters.AddWithValue("@P_ArtikelNr", row["P_ArtikelNr"]);
                            cmd.Parameters.AddWithValue("@P_Artikelkategorie", row["P_Artikelkategorie"]);
                            cmd.Parameters.AddWithValue("@P_ArtikelNrDesKd", row["P_ArtikelNrDesKd"]);
                            cmd.Parameters.AddWithValue("@P_Eingabemenge", ConvertToDecimalFormat(row["P_Eingabemenge"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert1", ConvertToDecimalFormat(row["P_EingabeWert1"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert2", ConvertToDecimalFormat(row["P_EingabeWert2"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert3", ConvertToDecimalFormat(row["P_EingabeWert3"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert4", ConvertToDecimalFormat(row["P_EingabeWert4"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert5", ConvertToDecimalFormat(row["P_EingabeWert5"].ToString()));
                            cmd.Parameters.AddWithValue("@P_RabattProzent", ConvertToDecimalFormat(row["P_RabattProzent"].ToString()));
                            cmd.Parameters.AddWithValue("@P_RabattBetrag", ConvertToDecimalFormat(row["P_RabattBetrag"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisNetto", ConvertToDecimalFormat(row["P_EinzelpreisNetto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisNachRabattNetto", ConvertToDecimalFormat(row["P_EinzelpreisNachRabattNetto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_Einkaufspreis", ConvertToDecimalFormat(row["P_Einkaufspreis"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisBrutto", ConvertToDecimalFormat(row["P_EinzelpreisBrutto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisNachRabattBrutto", ConvertToDecimalFormat(row["P_EinzelpreisNachRabattBrutto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_MwStProzenz", ConvertToDecimalFormat(row["P_MwStProzenz"].ToString()));
                            cmd.Parameters.AddWithValue("@P_AnzahlAbgerechnet", ConvertToDecimalFormat(row["P_AnzahlAbgerechnet"].ToString()));
                            cmd.Parameters.AddWithValue("@P_KomplettAbgerechnet", ConvertToDecimalFormat(row["P_KomplettAbgerechnet"].ToString()));
                            cmd.Parameters.AddWithValue("@P_Einheit", row["P_Einheit"]);
                            cmd.Parameters.AddWithValue("@P_Gewicht", ConvertToDecimalFormat(row["P_Gewicht"].ToString()));
                            cmd.Parameters.AddWithValue("@P_Volumen", ConvertToDecimalFormat(row["P_Volumen"].ToString()));
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld1", row["P_IndividuellesFeld1"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld2", row["P_IndividuellesFeld2"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld3", row["P_IndividuellesFeld3"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld4", row["P_IndividuellesFeld4"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld5", row["P_IndividuellesFeld5"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld6", row["P_IndividuellesFeld6"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld7", row["P_IndividuellesFeld7"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld8", row["P_IndividuellesFeld8"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld9", row["P_IndividuellesFeld9"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld10", row["P_IndividuellesFeld10"]);
                            cmd.Parameters.AddWithValue("@P_Positionsart", row["P_Positionsart"]);
                            cmd.Parameters.AddWithValue("@P_LohnnteilNetto", ConvertToDecimalFormat(row["P_LohnnteilNetto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_LohnnteilBrutto", ConvertToDecimalFormat(row["P_LohnnteilBrutto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EAN", row["P EAN"]);
                            cmd.Parameters.AddWithValue("@P_AuftragsNr", row["P_AuftragsNr"]);
                            cmd.Parameters.AddWithValue("@P_ReferenzNr_d_Kd", row["P_ReferenzNr d. Kd."]);
                            cmd.Parameters.AddWithValue("@P_ReferenzDatum_d_Kd", row["P_ReferenzDatum d. Kd."]);


                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Fehler beim Einfügen der Zeile: " + ex.Message);
                                foreach (var item in row.ItemArray)
                                {
                                    Console.WriteLine("Fehlerhafte Zeile Wert: " + item.ToString());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("CSV-Datei wurde nicht gefunden.");
            }
        }

        public void UploadCSV(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Wähle eine CSV-Datei aus"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

                try
                {
                    DataTable csvData = GetDataTableFromCSVFile(filePath);

                    // Debugging: Überprüfe die Spaltennamen in der DataTable
                    foreach (DataColumn column in csvData.Columns)
                    {
                        Console.WriteLine("Spalte: " + column.ColumnName);
                    }

                    // Debugging: Überprüfe die erste Zeile in der DataTable
                    if (csvData.Rows.Count > 0)
                    {
                        DataRow firstRow = csvData.Rows[0];
                        foreach (var item in firstRow.ItemArray)
                        {
                            Console.WriteLine("Wert: " + item.ToString());
                        }
                    }

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        foreach (DataRow row in csvData.Rows)
                        {
                            string query = @"
                      INSERT INTO export (
                          DELID, LieferscheinNr, Lieferscheindatum, Auftragsart, Status, SachbearbeiterIn, KundenNr, Kundenart, Kundenkategorie, 
                          Quelle, Anrede, Titel, Nachname_Firmenname, Vorname, Namenszusatz, Strasse, PLZ, Ort, Land, 
                          ASP_Anrede, ASP_Titel, ASP_Vorname, ASP_Nachname, AuftragsNr, `ReferenzNr d. Kd.`, `ReferenzDatum d. Kd.`, Lieferbedingung, 
                          LetzteVorlage, Anmerkungen, IndividuellesFeld1, IndividuellesFeld2, IndividuellesFeld3, IndividuellesFeld4, 
                          IndividuellesFeld5, IndividuellesFeld6, IndividuellesFeld7, IndividuellesFeld8, IndividuellesFeld9, 
                          IndividuellesFeld10, IndividuellesFeld11, IndividuellesFeld12, IndividuellesFeld13, IndividuellesFeld14, 
                          IndividuellesFeld15, IndividuellesFeld16, IndividuellesFeld17, IndividuellesFeld18, IndividuellesFeld19, 
                          IndividuellesFeld20, Projekt, Archiviert, Verwendung, Tabellenkategorie, LetzteVerarbeitung, P_PositionsNr, 
                          P_Anzahl, P_ArtikelNr, P_Artikelkategorie, P_ArtikelNrDesKd, P_Eingabemenge, P_EingabeWert1, P_EingabeWert2, 
                          P_EingabeWert3, P_EingabeWert4, P_EingabeWert5, P_RabattProzent, P_RabattBetrag, P_EinzelpreisNetto, 
                          P_EinzelpreisNachRabattNetto, P_Einkaufspreis, P_EinzelpreisBrutto, P_EinzelpreisNachRabattBrutto, 
                          P_MwStProzenz, P_AnzahlAbgerechnet, P_KomplettAbgerechnet, P_Einheit, P_Gewicht, P_Volumen, P_IndividuellesFeld1, 
                          P_IndividuellesFeld2, P_IndividuellesFeld3, P_IndividuellesFeld4, P_IndividuellesFeld5, P_IndividuellesFeld6, 
                          P_IndividuellesFeld7, P_IndividuellesFeld8, P_IndividuellesFeld9, P_IndividuellesFeld10, P_Positionsart, 
                          P_LohnnteilNetto, P_LohnnteilBrutto, `P EAN`, P_AuftragsNr, `P_ReferenzNr d. Kd.`, `P_ReferenzDatum d. Kd.`
                      ) VALUES (
                          @DELID, @LieferscheinNr, @Lieferscheindatum, @Auftragsart, @Status, @SachbearbeiterIn, @KundenNr, @Kundenart, @Kundenkategorie, 
                          @Quelle, @Anrede, @Titel, @Nachname_Firmenname, @Vorname, @Namenszusatz, @Strasse, @PLZ, @Ort, @Land, 
                          @ASP_Anrede, @ASP_Titel, @ASP_Vorname, @ASP_Nachname, @AuftragsNr, @ReferenzNr_d_Kd, @ReferenzDatum_d_Kd, @Lieferbedingung, 
                          @LetzteVorlage, @Anmerkungen, @IndividuellesFeld1, @IndividuellesFeld2, @IndividuellesFeld3, @IndividuellesFeld4, 
                          @IndividuellesFeld5, @IndividuellesFeld6, @IndividuellesFeld7, @IndividuellesFeld8, @IndividuellesFeld9, 
                          @IndividuellesFeld10, @IndividuellesFeld11, @IndividuellesFeld12, @IndividuellesFeld13, @IndividuellesFeld14, 
                          @IndividuellesFeld15, @IndividuellesFeld16, @IndividuellesFeld17, @IndividuellesFeld18, @IndividuellesFeld19, 
                          @IndividuellesFeld20, @Projekt, @Archiviert, @Verwendung, @Tabellenkategorie, @LetzteVerarbeitung, @P_PositionsNr, 
                          @P_Anzahl, @P_ArtikelNr, @P_Artikelkategorie, @P_ArtikelNrDesKd, @P_Eingabemenge, @P_EingabeWert1, @P_EingabeWert2, 
                          @P_EingabeWert3, @P_EingabeWert4, @P_EingabeWert5, @P_RabattProzent, @P_RabattBetrag, @P_EinzelpreisNetto, 
                          @P_EinzelpreisNachRabattNetto, @P_Einkaufspreis, @P_EinzelpreisBrutto, @P_EinzelpreisNachRabattBrutto, 
                          @P_MwStProzenz, @P_AnzahlAbgerechnet, @P_KomplettAbgerechnet, @P_Einheit, @P_Gewicht, @P_Volumen, @P_IndividuellesFeld1, 
                          @P_IndividuellesFeld2, @P_IndividuellesFeld3, @P_IndividuellesFeld4, @P_IndividuellesFeld5, @P_IndividuellesFeld6, 
                          @P_IndividuellesFeld7, @P_IndividuellesFeld8, @P_IndividuellesFeld9, @P_IndividuellesFeld10, @P_Positionsart, 
                          @P_LohnnteilNetto, @P_LohnnteilBrutto, @P_EAN, @P_AuftragsNr, @P_ReferenzNr_d_Kd, @P_ReferenzDatum_d_Kd
                      ) ON DUPLICATE KEY UPDATE
                          Lieferscheindatum = VALUES(Lieferscheindatum),
                          Auftragsart = VALUES(Auftragsart),
                          Status = VALUES(Status),
                          SachbearbeiterIn = VALUES(SachbearbeiterIn),
                          KundenNr = VALUES(KundenNr),
                          Kundenart = VALUES(Kundenart),
                          Kundenkategorie = VALUES(Kundenkategorie),
                          Quelle = VALUES(Quelle),
                          Anrede = VALUES(Anrede),
                          Titel = VALUES(Titel),
                          Nachname_Firmenname = VALUES(Nachname_Firmenname),
                          Vorname = VALUES(Vorname),
                          Namenszusatz = VALUES(Namenszusatz),
                          Strasse = VALUES(Strasse),
                          PLZ = VALUES(PLZ),
                          Ort = VALUES(Ort),
                          Land = VALUES(Land),
                          ASP_Anrede = VALUES(ASP_Anrede),
                          ASP_Titel = VALUES(ASP_Titel),
                          ASP_Vorname = VALUES(ASP_Vorname),
                          ASP_Nachname = VALUES(ASP_Nachname),
                          `ReferenzNr d. Kd.` = VALUES(`ReferenzNr d. Kd.`),
                          `ReferenzDatum d. Kd.` = VALUES(`ReferenzDatum d. Kd.`),
                          Lieferbedingung = VALUES(Lieferbedingung),
                          LetzteVorlage = VALUES(LetzteVorlage),
                          Anmerkungen = VALUES(Anmerkungen),
                          IndividuellesFeld1 = VALUES(IndividuellesFeld1),
                          IndividuellesFeld2 = VALUES(IndividuellesFeld2),
                          IndividuellesFeld3 = VALUES(IndividuellesFeld3),
                          IndividuellesFeld4 = VALUES(IndividuellesFeld4),
                          IndividuellesFeld5 = VALUES(IndividuellesFeld5),
                          IndividuellesFeld6 = VALUES(IndividuellesFeld6),
                          IndividuellesFeld7 = VALUES(IndividuellesFeld7),
                          IndividuellesFeld8 = VALUES(IndividuellesFeld8),
                          IndividuellesFeld9 = VALUES(IndividuellesFeld9),
                          IndividuellesFeld10 = VALUES(IndividuellesFeld10),
                          IndividuellesFeld11 = VALUES(IndividuellesFeld11),
                          IndividuellesFeld12 = VALUES(IndividuellesFeld12),
                          IndividuellesFeld13 = VALUES(IndividuellesFeld13),
                          IndividuellesFeld14 = VALUES(IndividuellesFeld14),
                          IndividuellesFeld15 = VALUES(IndividuellesFeld15),
                          IndividuellesFeld16 = VALUES(IndividuellesFeld16),
                          IndividuellesFeld17 = VALUES(IndividuellesFeld17),
                          IndividuellesFeld18 = VALUES(IndividuellesFeld18),
                          IndividuellesFeld19 = VALUES(IndividuellesFeld19),
                          IndividuellesFeld20 = VALUES(IndividuellesFeld20),
                          Projekt = VALUES(Projekt),
                          Archiviert = VALUES(Archiviert),
                          Verwendung = VALUES(Verwendung),
                          Tabellenkategorie = VALUES(Tabellenkategorie),
                          LetzteVerarbeitung = VALUES(LetzteVerarbeitung),
                          P_Anzahl = VALUES(P_Anzahl),
                          P_ArtikelNr = VALUES(P_ArtikelNr),
                          P_Artikelkategorie = VALUES(P_Artikelkategorie),
                          P_ArtikelNrDesKd = VALUES(P_ArtikelNrDesKd),
                          P_Eingabemenge = VALUES(P_Eingabemenge),
                          P_EingabeWert1 = VALUES(P_EingabeWert1),
                          P_EingabeWert2 = VALUES(P_EingabeWert2),
                          P_EingabeWert3 = VALUES(P_EingabeWert3),
                          P_EingabeWert4 = VALUES(P_EingabeWert4),
                          P_EingabeWert5 = VALUES(P_EingabeWert5),
                          P_RabattProzent = VALUES(P_RabattProzent),
                          P_RabattBetrag = VALUES(P_RabattBetrag),
                          P_EinzelpreisNetto = VALUES(P_EinzelpreisNetto),
                          P_EinzelpreisNachRabattNetto = VALUES(P_EinzelpreisNachRabattNetto),
                          P_Einkaufspreis = VALUES(P_Einkaufspreis),
                          P_EinzelpreisBrutto = VALUES(P_EinzelpreisBrutto),
                          P_EinzelpreisNachRabattBrutto = VALUES(P_EinzelpreisNachRabattBrutto),
                          P_MwStProzenz = VALUES(P_MwStProzenz),
                          P_AnzahlAbgerechnet = VALUES(P_AnzahlAbgerechnet),
                          P_KomplettAbgerechnet = VALUES(P_KomplettAbgerechnet),
                          P_Einheit = VALUES(P_Einheit),
                          P_Gewicht = VALUES(P_Gewicht),
                          P_Volumen = VALUES(P_Volumen),
                          P_IndividuellesFeld1 = VALUES(P_IndividuellesFeld1),
                          P_IndividuellesFeld2 = VALUES(P_IndividuellesFeld2),
                          P_IndividuellesFeld3 = VALUES(P_IndividuellesFeld3),
                          P_IndividuellesFeld4 = VALUES(P_IndividuellesFeld4),
                          P_IndividuellesFeld5 = VALUES(P_IndividuellesFeld5),
                          P_IndividuellesFeld6 = VALUES(P_IndividuellesFeld6),
                          P_IndividuellesFeld7 = VALUES(P_IndividuellesFeld7),
                          P_IndividuellesFeld8 = VALUES(P_IndividuellesFeld8),
                          P_IndividuellesFeld9 = VALUES(P_IndividuellesFeld9),
                          P_IndividuellesFeld10 = VALUES(P_IndividuellesFeld10),
                          P_Positionsart = VALUES(P_Positionsart),
                          P_LohnnteilNetto = VALUES(P_LohnnteilNetto),
                          P_LohnnteilBrutto = VALUES(P_LohnnteilBrutto),
                          `P EAN` = VALUES(`P EAN`),
                          P_AuftragsNr = VALUES(P_AuftragsNr),
                          `P_ReferenzNr d. Kd.` = VALUES(`P_ReferenzNr d. Kd.`),
                          `P_ReferenzDatum d. Kd.` = VALUES(`P_ReferenzDatum d. Kd.`)";



                            MySqlCommand cmd = new MySqlCommand(query, conn);

                            foreach (DataColumn column in csvData.Columns)
                            {
                                Console.WriteLine("Spalte: " + column.ColumnName);
                            }
                            foreach (var item in row.ItemArray)
                            {
                                Console.WriteLine("Wert: " + item.ToString());
                            }


                            cmd.Parameters.AddWithValue("@DELID", row["DELID"]);
                            cmd.Parameters.AddWithValue("@LieferscheinNr", row["LieferscheinNr"]);
                            cmd.Parameters.AddWithValue("@Lieferscheindatum", row["Lieferscheindatum"]);
                            cmd.Parameters.AddWithValue("@Auftragsart", row["Auftragsart"]);
                            cmd.Parameters.AddWithValue("@Status", row["Status"]);
                            cmd.Parameters.AddWithValue("@SachbearbeiterIn", row["SachbearbeiterIn"]);
                            cmd.Parameters.AddWithValue("@KundenNr", row["KundenNr"]);
                            cmd.Parameters.AddWithValue("@Kundenart", row["Kundenart"]);
                            cmd.Parameters.AddWithValue("@Kundenkategorie", row["Kundenkategorie"]);
                            cmd.Parameters.AddWithValue("@Quelle", row["Quelle"]);
                            cmd.Parameters.AddWithValue("@Anrede", row["Anrede"]);
                            cmd.Parameters.AddWithValue("@Titel", row["Titel"]);
                            cmd.Parameters.AddWithValue("@Nachname_Firmenname", row["Nachname_Firmenname"]);
                            cmd.Parameters.AddWithValue("@Vorname", row["Vorname"]);
                            cmd.Parameters.AddWithValue("@Namenszusatz", row["Namenszusatz"]);
                            cmd.Parameters.AddWithValue("@Strasse", row["Strasse"]);
                            cmd.Parameters.AddWithValue("@PLZ", row["PLZ"]);
                            cmd.Parameters.AddWithValue("@Ort", row["Ort"]);
                            cmd.Parameters.AddWithValue("@Land", row["Land"]);
                            cmd.Parameters.AddWithValue("@ASP_Anrede", row["ASP_Anrede"]);
                            cmd.Parameters.AddWithValue("@ASP_Titel", row["ASP_Titel"]);
                            cmd.Parameters.AddWithValue("@ASP_Vorname", row["ASP_Vorname"]);
                            cmd.Parameters.AddWithValue("@ASP_Nachname", row["ASP_Nachname"]);
                            cmd.Parameters.AddWithValue("@AuftragsNr", row["AuftragsNr"]);
                            cmd.Parameters.AddWithValue("@ReferenzNr_d_Kd", row["ReferenzNr d. Kd."]);
                            cmd.Parameters.AddWithValue("@ReferenzDatum_d_Kd", row["ReferenzDatum d. Kd."]);
                            cmd.Parameters.AddWithValue("@Lieferbedingung", row["Lieferbedingung"]);
                            cmd.Parameters.AddWithValue("@LetzteVorlage", row["LetzteVorlage"]);
                            cmd.Parameters.AddWithValue("@Anmerkungen", row["Anmerkungen"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld1", row["IndividuellesFeld1"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld2", row["IndividuellesFeld2"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld3", row["IndividuellesFeld3"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld4", row["IndividuellesFeld4"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld5", row["IndividuellesFeld5"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld6", row["IndividuellesFeld6"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld7", row["IndividuellesFeld7"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld8", row["IndividuellesFeld8"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld9", row["IndividuellesFeld9"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld10", row["IndividuellesFeld10"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld11", row["IndividuellesFeld11"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld12", row["IndividuellesFeld12"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld13", row["IndividuellesFeld13"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld14", row["IndividuellesFeld14"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld15", row["IndividuellesFeld15"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld16", row["IndividuellesFeld16"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld17", row["IndividuellesFeld17"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld18", row["IndividuellesFeld18"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld19", row["IndividuellesFeld19"]);
                            cmd.Parameters.AddWithValue("@IndividuellesFeld20", row["IndividuellesFeld20"]);
                            cmd.Parameters.AddWithValue("@Projekt", row["Projekt"]);
                            cmd.Parameters.AddWithValue("@Archiviert", row["Archiviert"]);
                            cmd.Parameters.AddWithValue("@Verwendung", row["Verwendung"]);
                            cmd.Parameters.AddWithValue("@Tabellenkategorie", row["Tabellenkategorie"]);
                            cmd.Parameters.AddWithValue("@LetzteVerarbeitung", row["LetzteVerarbeitung"]);
                            cmd.Parameters.AddWithValue("@P_PositionsNr", row["P_PositionsNr"]);
                            cmd.Parameters.AddWithValue("@P_Anzahl", ConvertToDecimalFormat(row["P_Anzahl"].ToString()));
                            cmd.Parameters.AddWithValue("@P_ArtikelNr", row["P_ArtikelNr"]);
                            cmd.Parameters.AddWithValue("@P_Artikelkategorie", row["P_Artikelkategorie"]);
                            cmd.Parameters.AddWithValue("@P_ArtikelNrDesKd", row["P_ArtikelNrDesKd"]);
                            cmd.Parameters.AddWithValue("@P_Eingabemenge", ConvertToDecimalFormat(row["P_Eingabemenge"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert1", ConvertToDecimalFormat(row["P_EingabeWert1"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert2", ConvertToDecimalFormat(row["P_EingabeWert2"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert3", ConvertToDecimalFormat(row["P_EingabeWert3"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert4", ConvertToDecimalFormat(row["P_EingabeWert4"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EingabeWert5", ConvertToDecimalFormat(row["P_EingabeWert5"].ToString()));
                            cmd.Parameters.AddWithValue("@P_RabattProzent", ConvertToDecimalFormat(row["P_RabattProzent"].ToString()));
                            cmd.Parameters.AddWithValue("@P_RabattBetrag", ConvertToDecimalFormat(row["P_RabattBetrag"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisNetto", ConvertToDecimalFormat(row["P_EinzelpreisNetto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisNachRabattNetto", ConvertToDecimalFormat(row["P_EinzelpreisNachRabattNetto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_Einkaufspreis", ConvertToDecimalFormat(row["P_Einkaufspreis"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisBrutto", ConvertToDecimalFormat(row["P_EinzelpreisBrutto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EinzelpreisNachRabattBrutto", ConvertToDecimalFormat(row["P_EinzelpreisNachRabattBrutto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_MwStProzenz", ConvertToDecimalFormat(row["P_MwStProzenz"].ToString()));
                            cmd.Parameters.AddWithValue("@P_AnzahlAbgerechnet", ConvertToDecimalFormat(row["P_AnzahlAbgerechnet"].ToString()));
                            cmd.Parameters.AddWithValue("@P_KomplettAbgerechnet", ConvertToDecimalFormat(row["P_KomplettAbgerechnet"].ToString()));
                            cmd.Parameters.AddWithValue("@P_Einheit", row["P_Einheit"]);
                            cmd.Parameters.AddWithValue("@P_Gewicht", ConvertToDecimalFormat(row["P_Gewicht"].ToString()));
                            cmd.Parameters.AddWithValue("@P_Volumen", ConvertToDecimalFormat(row["P_Volumen"].ToString()));
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld1", row["P_IndividuellesFeld1"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld2", row["P_IndividuellesFeld2"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld3", row["P_IndividuellesFeld3"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld4", row["P_IndividuellesFeld4"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld5", row["P_IndividuellesFeld5"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld6", row["P_IndividuellesFeld6"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld7", row["P_IndividuellesFeld7"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld8", row["P_IndividuellesFeld8"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld9", row["P_IndividuellesFeld9"]);
                            cmd.Parameters.AddWithValue("@P_IndividuellesFeld10", row["P_IndividuellesFeld10"]);
                            cmd.Parameters.AddWithValue("@P_Positionsart", row["P_Positionsart"]);
                            cmd.Parameters.AddWithValue("@P_LohnnteilNetto", ConvertToDecimalFormat(row["P_LohnnteilNetto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_LohnnteilBrutto", ConvertToDecimalFormat(row["P_LohnnteilBrutto"].ToString()));
                            cmd.Parameters.AddWithValue("@P_EAN", row["P EAN"]);
                            cmd.Parameters.AddWithValue("@P_AuftragsNr", row["P_AuftragsNr"]);
                            cmd.Parameters.AddWithValue("@P_ReferenzNr_d_Kd", row["P_ReferenzNr d. Kd."]);
                            cmd.Parameters.AddWithValue("@P_ReferenzDatum_d_Kd", row["P_ReferenzDatum d. Kd."]);

                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Fehler beim Einfügen der Zeile: " + ex.Message);
                                // Ausgabe der fehlerhaften Zeile
                                foreach (var item in row.ItemArray)
                                {
                                    Console.WriteLine("Fehlerhafte Zeile Wert: " + item.ToString());
                                }
                            }
                        }
                        MessageBox.Show("CSV-Datei erfolgreich hochgeladen!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler: " + ex.Message);
                }
            }
        }
    }

    public class BestätigteLieferscheinDaten
    {
        public string LieferscheinNr { get; set; }
        public string KundenNr { get; set; }
        public string Sachbearbeiter { get; set; }
        public string ReferenzNr { get; set; }
        public List<LieferscheinPosition> Positionen { get; set; }
    }

}


