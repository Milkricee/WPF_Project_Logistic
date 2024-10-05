public class LieferscheinPosition
{
    private string _artikelnr;

    public int Nummer { get; set; }
    public decimal Anzahl { get; set; }
    public string Einheit { get; set; }
    public string Bezeichnung { get; set; }

    public string Artikelnr
    {
        get { return _artikelnr; }
        set { _artikelnr = value?.Trim(); }
    }
}
