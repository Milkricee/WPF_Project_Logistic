using Logistik_Gui;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

public class LieferscheinViewModel : INotifyPropertyChanged
{
    private ObservableCollection<LieferscheinPosition> _positionen;
    private HashSet<string> _artikelnummern;
    private string _lieferscheinNr;

    public ObservableCollection<LieferscheinPosition> Positionen
    {
        get { return _positionen; }
        set
        {
            _positionen = value;
            OnPropertyChanged(nameof(Positionen));
        }
    }

    public string LieferscheinNr
    {
        get { return _lieferscheinNr; }
        set
        {
            _lieferscheinNr = value;
            OnPropertyChanged(nameof(LieferscheinNr));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public LieferscheinViewModel()
    {
        Positionen = new ObservableCollection<LieferscheinPosition>();
        _artikelnummern = new HashSet<string>();
    }

    public bool ValidateSerialCode(SerialCodeData serialCodeData)
    {
        string trimmedArtikelnummer = serialCodeData.Artikelnummer.Trim();
        return _artikelnummern.Contains(trimmedArtikelnummer);
    }

    public void AddLieferscheinPosition(LieferscheinPosition position)
    {
        string trimmedArtikelnummer = position.Artikelnr.Trim();
        Positionen.Add(position);
        _artikelnummern.Add(trimmedArtikelnummer);
    }
}
