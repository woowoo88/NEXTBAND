namespace NextBand.Models;

public sealed class UserModel : ObservableModel
{
    private string _fullName = "Lucas Mendes";
    private string _userName = "lucasmendes";
    private string _phone = "(11) 99999-9999";
    private string _email = "lucas@email.com";
    private string _password = "123456";
    private string _instagram = "@lucasmendes";
    private string _linkedIn = "linkedin.com/in/lucasmendes";
    private string _affiliation = string.Empty;
    private string _age = "25";
    private string _bio = "Desenvolvedor Full Stack apaixonado por tecnologia, networking e inovação. Sempre em busca de novas conexões.";

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
