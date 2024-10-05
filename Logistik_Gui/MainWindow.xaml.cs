using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Logistik_Gui
{
    public partial class MainWindow : Window
    {
        private string inputBuffer = string.Empty;
        private LieferscheinViewModel _confirmedLieferschein;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded; // Event-Handler für den automatischen CSV-Import hinzufügen

            LieferscheinCSVControl.SetArtikelControl(ArtikelControl);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Automatischer CSV-Import beim Start
            LieferscheinCSVControl.AutoImportCSV();
            ConfirmLieferschein();

            ArtikelControl.Initialize(LieferscheinCSVControl); // Initialisieren der Artikel-Control mit der Lieferschein_CSV-Referenz
        }

        private void UploadCSV(object sender, RoutedEventArgs e)
        {
            LieferscheinCSVControl.UploadCSV(sender, e);
            ConfirmLieferschein();  // Bestätigen Sie den Lieferschein nachdem die CSV hochgeladen wurde
        }

        private void ConfirmLieferschein()
        {
            _confirmedLieferschein = new LieferscheinViewModel();

            //// Beispiel-Artikelnummern hinzufügen
            //_confirmedLieferschein.AddLieferscheinPosition(new LieferscheinPosition { Artikelnr = "2-0001-00003" });
            //_confirmedLieferschein.AddLieferscheinPosition(new LieferscheinPosition { Artikelnr = "3-0035-00065" });

            // Bestätigen Sie den Lieferschein im Artikel Control
            ArtikelControl?.ConfirmLieferschein(_confirmedLieferschein);

            // Debugging-Ausgabe zur Überprüfung der Artikelnummern
            Console.WriteLine("Bestätigte Artikelnummern im Lieferschein:");
            foreach (var position in _confirmedLieferschein.Positionen)
            {
                Console.WriteLine($"'{position.Artikelnr}'");
            }
        }

        private void OnEinlagern(object sender, RoutedEventArgs e)
        {
            Lager lagerWindow = new Lager();
            lagerWindow.Closed += LagerWindow_Closed; // Handle the Closed event of the Lager window
            lagerWindow.Show();
            this.Hide();
        }

        private void LagerWindow_Closed(object sender, EventArgs e)
        {
            this.Show();
        }
    }
}
