namespace NextBand.Models;

public sealed class EmergencyContactModel : ObservableModel
{
    private string _name = string.Empty;
    private string _relation = string.Empty;
    private string _phone = string.Empty;
    private string _note = string.Empty;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Relation { get => _relation; set => SetProperty(ref _relation, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
}
