using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NextBand.Models;
using NextBand.Services;

namespace NextBand.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly BluetoothService _bluetoothService = new();
    private readonly NfcService _nfcService = new();
    private readonly StorageService _storageService = new();
    private readonly ValidationService _validationService = new();
    private AppPage _currentPage = AppPage.Login;
    private string _loginEmail = string.Empty;
    private string _loginPassword = string.Empty;
    private string _registerFullName = string.Empty;
    private string _registerUserName = string.Empty;
    private string _registerPhone = string.Empty;
    private string _registerEmail = string.Empty;
    private string _registerPassword = string.Empty;
    private string _registerConfirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isStatusError;
    private bool _showLoginPassword;
    private bool _showRegisterPassword;
    private bool _showRegisterConfirmPassword;
    private string _connectionSearch = string.Empty;
    private CountryDialCodeModel _selectedDialCode;

    public MainViewModel()
    {
        _selectedDialCode = CountryDialCodes[0];
        NavigateCommand = new RelayCommand(page => NavigateTo((AppPage)page!));
        BackCommand = new RelayCommand(GoBack);
        LoginCommand = new RelayCommand(Login);
        RegisterCommand = new RelayCommand(Register);
        SaveProfileCommand = new RelayCommand(async () => await SaveProfileAsync());
        ShareProfileCommand = new RelayCommand(ShareProfile);
        ConnectBandCommand = new RelayCommand(async () => await ConnectBandAsync());
        DisconnectBandCommand = new RelayCommand(() =>
        {
            Data.Band.IsConnected = false;
            SetStatus("Pulseira desconectada.");
        });
        SendBandCommand = new RelayCommand(async () => await SendBandAsync());
        SaveEmergencyCommand = new RelayCommand(async () => await SaveEmergencyAsync());
        PreviewEmergencyCommand = new RelayCommand(() => NavigateTo(AppPage.EmergencyPublic));
        AddEmergencyContactCommand = new RelayCommand(AddEmergencyContact);
        AddCustomInfoCommand = new RelayCommand(AddCustomInfo);
        AddGuardianCommand = new RelayCommand(AddGuardian);
        ViewConnectionCommand = new RelayCommand(connection =>
        {
            if (connection is ConnectionModel model)
            {
                model.IsNew = false;
                SetStatus($"Perfil de {model.Name} visualizado.");
                OnPropertyChanged(nameof(FilteredConnections));
            }
        });
        LogoutCommand = new RelayCommand(() =>
        {
            CurrentPage = AppPage.Login;
            SetStatus("Você saiu da conta.");
        });

        _ = LoadAsync();
    }

    public AppDataModel Data { get; private set; } = new();
    public ObservableCollection<string> FoundDevices { get; } = [];
    public ObservableCollection<CountryDialCodeModel> CountryDialCodes { get; } =
    [
        new() { Flag = "\U0001F1E7\U0001F1F7", Name = "Brasil", DialCode = "+55" },
        new() { Flag = "\U0001F1FA\U0001F1F8", Name = "Estados Unidos", DialCode = "+1" },
        new() { Flag = "\U0001F1F5\U0001F1F9", Name = "Portugal", DialCode = "+351" },
        new() { Flag = "\U0001F1E6\U0001F1F7", Name = "Argentina", DialCode = "+54" },
        new() { Flag = "\U0001F1FA\U0001F1FE", Name = "Uruguai", DialCode = "+598" },
        new() { Flag = "\U0001F1F5\U0001F1FE", Name = "Paraguai", DialCode = "+595" }
    ];

    public AppPage CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsLoginVisible));
                OnPropertyChanged(nameof(IsRegisterVisible));
                OnPropertyChanged(nameof(IsDashboardVisible));
                OnPropertyChanged(nameof(IsProfileVisible));
                OnPropertyChanged(nameof(IsEditProfileVisible));
                OnPropertyChanged(nameof(IsConnectionsVisible));
                OnPropertyChanged(nameof(IsMyBandVisible));
                OnPropertyChanged(nameof(IsCustomUrlVisible));
                OnPropertyChanged(nameof(IsEmergencyPublicVisible));
            }
        }
    }

    public string LoginEmail { get => _loginEmail; set => SetProperty(ref _loginEmail, value); }
    public string LoginPassword { get => _loginPassword; set => SetProperty(ref _loginPassword, value); }
    public bool ShowLoginPassword { get => _showLoginPassword; set => SetProperty(ref _showLoginPassword, value); }
    public string RegisterFullName { get => _registerFullName; set => SetProperty(ref _registerFullName, value); }
    public string RegisterUserName { get => _registerUserName; set => SetProperty(ref _registerUserName, value); }
    public string RegisterPhone
    {
        get => _registerPhone;
        set => SetProperty(ref _registerPhone, FormatPhoneNumber(value));
    }

    public string RegisterEmail { get => _registerEmail; set => SetProperty(ref _registerEmail, value); }
    public CountryDialCodeModel SelectedDialCode { get => _selectedDialCode; set => SetProperty(ref _selectedDialCode, value); }
    public string RegisterPassword
    {
        get => _registerPassword;
        set
        {
            if (SetProperty(ref _registerPassword, value))
            {
                OnPropertyChanged(nameof(IsRegisterPasswordValid));
                OnPropertyChanged(nameof(IsRegisterConfirmPasswordValid));
            }
        }
    }

    public string RegisterConfirmPassword
    {
        get => _registerConfirmPassword;
        set
        {
            if (SetProperty(ref _registerConfirmPassword, value))
            {
                OnPropertyChanged(nameof(IsRegisterConfirmPasswordValid));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(IsStatusVisible));
            }
        }
    }

    public bool IsStatusVisible => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool IsStatusError { get => _isStatusError; set => SetProperty(ref _isStatusError, value); }
    public bool ShowRegisterPassword { get => _showRegisterPassword; set => SetProperty(ref _showRegisterPassword, value); }
    public bool ShowRegisterConfirmPassword { get => _showRegisterConfirmPassword; set => SetProperty(ref _showRegisterConfirmPassword, value); }
    public bool IsRegisterPasswordValid => HasValidPasswordRules(RegisterPassword);
    public bool IsRegisterConfirmPasswordValid => IsRegisterPasswordValid && RegisterPassword == RegisterConfirmPassword;

    public string ConnectionSearch
    {
        get => _connectionSearch;
        set
        {
            if (SetProperty(ref _connectionSearch, value))
            {
                OnPropertyChanged(nameof(FilteredConnections));
            }
        }
    }

    public IEnumerable<ConnectionModel> RecentConnections => Data.Connections.Take(4);

    public IEnumerable<ConnectionModel> FilteredConnections
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ConnectionSearch))
            {
                return Data.Connections;
            }

            var search = ConnectionSearch.Trim().TrimStart('@');
            return Data.Connections.Where(connection =>
                connection.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || connection.UserName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
    }

    public string ConnectionCountText => $"{Data.Connections.Count} conexões";
    public string BandStatusText => Data.Band.IsConnected ? "Conectada" : "Desconectada";
    public string DashboardBandStatusText => Data.Band.IsConnected ? "Pulseira conectada" : "Pulseira desconectada";
    public string OledCounterText => $"{Data.Band.OledText.Length}/20";
    public string LedColorPreview => NormalizeHex(Data.Band.LedHexColor);
    public string EmergencyPublicMedication => string.IsNullOrWhiteSpace(Data.EmergencyProfile.Medications) ? "Nenhum" : Data.EmergencyProfile.Medications;
    public string EmergencyUrl => _nfcService.BuildEmergencyPayload(Data.EmergencyProfile.ChildName);

    public bool IsLoginVisible => CurrentPage == AppPage.Login;
    public bool IsRegisterVisible => CurrentPage == AppPage.Register;
    public bool IsDashboardVisible => CurrentPage == AppPage.Dashboard;
    public bool IsProfileVisible => CurrentPage == AppPage.Profile;
    public bool IsEditProfileVisible => CurrentPage == AppPage.EditProfile;
    public bool IsConnectionsVisible => CurrentPage == AppPage.Connections;
    public bool IsMyBandVisible => CurrentPage == AppPage.MyBand;
    public bool IsCustomUrlVisible => CurrentPage == AppPage.CustomUrl;
    public bool IsEmergencyPublicVisible => CurrentPage == AppPage.EmergencyPublic;

    public ICommand NavigateCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand ShareProfileCommand { get; }
    public ICommand ConnectBandCommand { get; }
    public ICommand DisconnectBandCommand { get; }
    public ICommand SendBandCommand { get; }
    public ICommand SaveEmergencyCommand { get; }
    public ICommand PreviewEmergencyCommand { get; }
    public ICommand AddEmergencyContactCommand { get; }
    public ICommand AddCustomInfoCommand { get; }
    public ICommand AddGuardianCommand { get; }
    public ICommand ViewConnectionCommand { get; }
    public ICommand LogoutCommand { get; }

    private async Task LoadAsync()
    {
        Data = await _storageService.LoadAsync();
        if (string.IsNullOrWhiteSpace(Data.User.Password))
        {
            Data.User.Password = "123456";
        }

        WireDataNotifications();
        RefreshAll();
    }

    private void WireDataNotifications()
    {
        Data.User.PropertyChanged += DataPropertyChanged;
        Data.Band.PropertyChanged += DataPropertyChanged;
        Data.EmergencyProfile.PropertyChanged += DataPropertyChanged;
    }

    private void DataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Data));
        OnPropertyChanged(nameof(RecentConnections));
        OnPropertyChanged(nameof(FilteredConnections));
        OnPropertyChanged(nameof(BandStatusText));
        OnPropertyChanged(nameof(DashboardBandStatusText));
        OnPropertyChanged(nameof(OledCounterText));
        OnPropertyChanged(nameof(LedColorPreview));
        OnPropertyChanged(nameof(EmergencyPublicMedication));
        OnPropertyChanged(nameof(EmergencyUrl));
    }

    private void RefreshAll()
    {
        OnPropertyChanged(nameof(Data));
        OnPropertyChanged(nameof(RecentConnections));
        OnPropertyChanged(nameof(FilteredConnections));
        OnPropertyChanged(nameof(ConnectionCountText));
        OnPropertyChanged(nameof(BandStatusText));
        OnPropertyChanged(nameof(DashboardBandStatusText));
        OnPropertyChanged(nameof(OledCounterText));
        OnPropertyChanged(nameof(EmergencyUrl));
    }

    private void NavigateTo(AppPage page)
    {
        CurrentPage = page;
        SetStatus(string.Empty);
    }

    private void GoBack()
    {
        CurrentPage = CurrentPage switch
        {
            AppPage.Register => AppPage.Login,
            AppPage.Profile => AppPage.Dashboard,
            AppPage.EditProfile => AppPage.Profile,
            AppPage.Connections => AppPage.Dashboard,
            AppPage.MyBand => AppPage.Dashboard,
            AppPage.CustomUrl => AppPage.MyBand,
            AppPage.EmergencyPublic => AppPage.CustomUrl,
            _ => AppPage.Dashboard
        };
    }

    private void Login()
    {
        if (!_validationService.IsEmail(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            SetStatus("Informe um e-mail válido e sua senha.", true);
            return;
        }

        var emailMatches = string.Equals(LoginEmail.Trim(), Data.User.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        var passwordMatches = LoginPassword == Data.User.Password;

        if (!emailMatches || !passwordMatches)
        {
            SetStatus("E-mail ou senha incorretos. Confira os dados e tente novamente.", true);
            return;
        }

        CurrentPage = AppPage.Dashboard;
        SetStatus("Login realizado com sucesso.");
    }

    private async void Register()
    {
        if (string.IsNullOrWhiteSpace(RegisterFullName))
        {
            SetStatus("Informe seu nome completo.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterUserName))
        {
            SetStatus("Informe um nome de usuário.", true);
            return;
        }

        if (!_validationService.IsBrazilianPhone(RegisterPhone))
        {
            SetStatus("Informe um celular brasileiro válido.", true);
            return;
        }

        if (!_validationService.IsEmail(RegisterEmail))
        {
            SetStatus("Informe um e-mail válido.", true);
            return;
        }

        if (!IsRegisterConfirmPasswordValid)
        {
            SetStatus("A senha precisa ter no mínimo 6 caracteres, pelo menos 1 letra, 1 número, e a confirmação deve ser igual.", true);
            return;
        }

        Data = CreateCleanAccountData();
        WireDataNotifications();
        RefreshAll();
        await _storageService.SaveAsync(Data);
        CurrentPage = AppPage.Dashboard;
        SetStatus("Conta criada com sucesso.");
    }

    private async Task SaveProfileAsync()
    {
        if (!_validationService.IsEmail(Data.User.Email) || string.IsNullOrWhiteSpace(Data.User.UserName))
        {
            SetStatus("Revise o e-mail e o nome de usuário.", true);
            return;
        }

        Data.User.UserName = Data.User.UserName.TrimStart('@');
        Data.Band.OledText = $"@{Data.User.UserName}";
        await _storageService.SaveAsync(Data);
        SetStatus("Perfil atualizado com sucesso.");
        CurrentPage = AppPage.Profile;
    }

    private void ShareProfile()
    {
        var payload = _nfcService.BuildProfilePayload(Data.User.UserName);
        SetStatus($"Perfil pronto para compartilhar via NFC: {payload}");
    }

    private async Task ConnectBandAsync()
    {
        FoundDevices.Clear();
        foreach (var device in await _bluetoothService.ScanAsync())
        {
            FoundDevices.Add(device);
        }

        var selected = FoundDevices.FirstOrDefault(device => device.Contains("ESP32", StringComparison.OrdinalIgnoreCase)) ?? FoundDevices.First();
        Data.Band.IsConnected = await _bluetoothService.ConnectAsync(selected);
        Data.Band.DeviceName = selected;
        SetStatus(Data.Band.IsConnected
            ? "Pulseira conectada com sucesso."
            : "Não foi possível conectar. Tente novamente.",
            !Data.Band.IsConnected);
    }

    private async Task SendBandAsync()
    {
        if (!Data.Band.IsConnected)
        {
            await _storageService.SaveAsync(Data);
            SetStatus("Configuracao salva para sincronizar depois.");
            return;
        }

        await _bluetoothService.SendAsync($"{Data.Band.OledText}|{Data.Band.LedHexColor}|{Data.Band.QuickLink}");
        await _storageService.SaveAsync(Data);
        SetStatus("Dados enviados para a pulseira.");
    }

    private async Task SaveEmergencyAsync()
    {
        if (string.IsNullOrWhiteSpace(Data.EmergencyProfile.ChildName))
        {
            SetStatus("Informe o nome da criança.", true);
            return;
        }

        if (!_validationService.IsBloodType(Data.EmergencyProfile.BloodType))
        {
            SetStatus("Tipo sanguineo deve ser A+, A-, B+, B-, AB+, AB-, O+ ou O-.", true);
            return;
        }

        await _storageService.SaveAsync(Data);
        SetStatus("Informações de emergência salvas.");
        OnPropertyChanged(nameof(EmergencyUrl));
    }

    private void AddEmergencyContact()
    {
        Data.EmergencyProfile.ExtraContacts.Add(new EmergencyContactModel
        {
            Name = string.Empty,
            Relation = string.Empty,
            Phone = string.Empty
        });
    }

    private void AddCustomInfo()
    {
        Data.EmergencyProfile.CustomInfos.Add(new CustomInfoModel
        {
            Title = string.Empty,
            Description = string.Empty
        });
    }

    private void AddGuardian()
    {
        Data.EmergencyProfile.Guardians = string.IsNullOrWhiteSpace(Data.EmergencyProfile.Guardians)
            ? "Novo responsável"
            : $"{Data.EmergencyProfile.Guardians} e Novo responsável";
    }

    private static string NormalizeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "#3B82F6";
        }

        return value.StartsWith('#') ? value : $"#{value}";
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }

    private AppDataModel CreateCleanAccountData()
    {
        var userName = RegisterUserName.Trim().TrimStart('@');
        var data = new AppDataModel();
        data.Connections.Clear();

        data.User.FullName = RegisterFullName.Trim();
        data.User.UserName = userName;
        data.User.Phone = $"{SelectedDialCode.DialCode} {RegisterPhone}".Trim();
        data.User.Email = RegisterEmail.Trim();
        data.User.Password = RegisterPassword;
        data.User.Instagram = string.Empty;
        data.User.LinkedIn = string.Empty;
        data.User.Affiliation = string.Empty;
        data.User.Age = string.Empty;
        data.User.Bio = string.Empty;

        data.Band.DeviceName = "ESP32";
        data.Band.IsConnected = false;
        data.Band.OledText = string.IsNullOrWhiteSpace(userName) ? string.Empty : $"@{userName}";
        data.Band.QuickLink = string.Empty;
        data.Band.QuickLinkType = "URL";
        data.Band.ChildMode = false;
        data.Band.NotificationsEnabled = false;
        data.Band.LocationEnabled = false;

        data.EmergencyProfile.ChildName = string.Empty;
        data.EmergencyProfile.Age = string.Empty;
        data.EmergencyProfile.BloodType = string.Empty;
        data.EmergencyProfile.Guardians = string.Empty;
        data.EmergencyProfile.MainPhone = string.Empty;
        data.EmergencyProfile.Address = string.Empty;
        data.EmergencyProfile.Allergies = string.Empty;
        data.EmergencyProfile.MedicalConditions = string.Empty;
        data.EmergencyProfile.Disabilities = string.Empty;
        data.EmergencyProfile.SpecialNeeds = string.Empty;
        data.EmergencyProfile.Medications = string.Empty;
        data.EmergencyProfile.EmergencyInstructions = string.Empty;
        data.EmergencyProfile.ExtraContacts.Clear();
        data.EmergencyProfile.CustomInfos.Clear();

        return data;
    }

    private static bool HasValidPasswordRules(string value)
    {
        return value.Length >= 6
            && value.Any(char.IsLetter)
            && value.Any(char.IsDigit);
    }

    private static string FormatPhoneNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("55") && digits.Length > 2)
        {
            digits = digits[2..];
        }

        if (digits.Length > 11)
        {
            digits = digits[..11];
        }

        if (digits.Length <= 2)
        {
            return digits.Length == 0 ? string.Empty : $"({digits}";
        }

        var ddd = digits[..2];
        var number = digits[2..];
        if (number.Length <= 5)
        {
            return $"({ddd}) {number}";
        }

        return $"({ddd}) {number[..5]}-{number[5..]}";
    }
}
