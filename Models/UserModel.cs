namespace NextBand.Models;

public sealed class UserModel : ObservableModel
{
    private string _fullName = string.Empty;
    private string _userName = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _instagram = string.Empty;
    private string _linkedIn = string.Empty;
    private string _affiliation = string.Empty;
    private string _age = string.Empty;
    private string _bio = string.Empty;

    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string Instagram { get => _instagram; set => SetProperty(ref _instagram, value); }
    public string LinkedIn { get => _linkedIn; set => SetProperty(ref _linkedIn, value); }
    public string Affiliation { get => _affiliation; set => SetProperty(ref _affiliation, value); }
    public string Age { get => _age; set => SetProperty(ref _age, value); }
    public string Bio { get => _bio; set => SetProperty(ref _bio, value); }
}
