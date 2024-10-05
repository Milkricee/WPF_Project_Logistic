using System;

namespace Logistik_Gui
{
    public class SerialCodeData
    {
        public int Nummer { get; set; } // Fortlaufende Nummer
        public string Seriennummer { get; set; }
        public string Artikelnummer { get; set; }
        public string Artikel { get; set; }
        public string Hersteller { get; set; }
        public int Count { get; set; } // Neue Eigenschaft für die Zählung

        public override bool Equals(object obj)
        {
            return obj is SerialCodeData data &&
                   Seriennummer == data.Seriennummer;
        }

        public override int GetHashCode()
        {
            return Seriennummer != null ? Seriennummer.GetHashCode() : 0;
        }
    }
}
