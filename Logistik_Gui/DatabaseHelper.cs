using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace Logistik_Gui
{
    public static class DatabaseHelper
    {
        private static string connectionString = "server=127.0.0.1;port=8000;user=root;password=1234;database=test;";

        public static void SaveData(ObservableCollection<SerialCodeData> dataList)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                StringBuilder errorMessages = new StringBuilder();
                bool hasErrors = false; // Flag to check if errors occurred

                try
                {
                    foreach (var item in dataList)
                    {
                        string queryArtikel = @"
                        INSERT INTO Artikel (ArtikelNr, Artikel, Hersteller, Anzahl)
                        VALUES (@ArtikelNr, @Artikel, @Hersteller, 1)
                        ON DUPLICATE KEY UPDATE Anzahl = Anzahl + 1";

                        MySqlCommand cmdArtikel = new MySqlCommand(queryArtikel, conn, transaction);
                        cmdArtikel.Parameters.AddWithValue("@ArtikelNr", item.Artikelnummer);
                        cmdArtikel.Parameters.AddWithValue("@Artikel", item.Artikel);
                        cmdArtikel.Parameters.AddWithValue("@Hersteller", item.Hersteller);
                        cmdArtikel.ExecuteNonQuery();

                        string serialNumber = item.Seriennummer; // Seriennummer zwischenspeichern
                        string querySN = "INSERT INTO Seriennummern (SN, ArtikelNr) VALUES (@SN, @ArtikelNr)";
                        MySqlCommand cmdSN = new MySqlCommand(querySN, conn, transaction);
                        cmdSN.Parameters.AddWithValue("@SN", serialNumber);
                        cmdSN.Parameters.AddWithValue("@ArtikelNr", item.Artikelnummer);
                        try
                        {
                            cmdSN.ExecuteNonQuery();
                        }
                        catch (MySqlException ex)
                        {
                            hasErrors = true; // Set the flag to true if any error occurs
                            if (ex.Number == 1062) // Duplicate entry error code
                            {
                                errorMessages.AppendLine($"Fehler: Die Seriennummer '{serialNumber}' existiert bereits.");
                            }
                            else
                            {
                                errorMessages.AppendLine($"Fehler beim Speichern der Seriennummer '{serialNumber}': {ex.Message}");
                            }
                        }
                    }
                    if (hasErrors)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Es sind Fehler aufgetreten:\n{errorMessages}");
                    }
                    else
                    {
                        transaction.Commit();
                        MessageBox.Show("Daten erfolgreich gespeichert!");
                    }
                }
                catch (MySqlException ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Fehler beim Speichern: {ex.Message}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ein allgemeiner Fehler ist aufgetreten: {ex.Message}");
                }
            }
        }
    }
}
