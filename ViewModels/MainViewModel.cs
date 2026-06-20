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
    private string _loginEmail = "lucas@email.com";
    private string _loginPassword = "123456";
    private string _registerUserName = string.Empty;
    private string _registerPhone = string.Empty;
    private string _registerEmail = string.Empty;
    private string _registerPassword = string.Empty;
    private string _registerConfirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private string _connectionSearch = string.Empty;

    public MainViewModel()
    {
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
            StatusMessage = "Pulseira desconectada.";
        });
        SendBandCommand = new RelayCommand(async () => await SendBandAsync());
        SaveEmergencyCommand = new RelayCommand(async () => await SaveEmergencyAsync());
        PreviewEmergencyCommand = new RelayCommand(() => NavigateTo(AppPage.EmergencyPublic));
        AddEmergencyContactCommand = new RelayCommand(AddEmergencyContact);
        AddCustomInfoCommand = new RelayCommand(AddCustomInfo);
        ViewConnectionCommand = new RelayCommand(connection =>
        {
            if (connection is ConnectionModel model)
            {
                model.IsNew = false;
                StatusMessage = $"Perfil de {model.Name} visualizado.";
                OnPropertyChanged(nameof(FilteredConnections));
            }
        });
        LogoutCommand = new RelayCommand(() =>
        {
            CurrentPage = AppPage.Login;
            StatusMessage = "Voce saiu da conta.";
        });

        _ = LoadAsync();
    }

    public AppDataModel Data { get; private set; } = new();
    public ObservableCollection<string> FoundDevices { get; } = [];

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
    public string RegisterUserName { get => _registerUserName; set => SetProperty(ref _registerUserName, value); }
    public string RegisterPhone { get => _registerPhone; set => SetProperty(ref _registerPhone, value); }
    public string RegisterEmail { get => _registerEmail; set => SetProperty(ref _registerEmail, value); }
    public string RegisterPassword { get => _registerPassword; set => SetProperty(ref _registerPassword, value); }
    public string RegisterConfirmPassword { get => _registerConfirmPassword; set => SetProperty(ref _registerConfirmPassword, value); }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

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

    public string ConnectionCountText => $"{Data.Connections.Count} conexoes";
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
    public ICommand ViewConnectionCommand { get; }
    public ICommand LogoutCommand { get; }

    private async Task LoadAsync()
    {
        Data = await _storageService.LoadAsync();
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
        StatusMessage = string.Empty;
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
            StatusMessage = "Informe um e-mail valido e sua senha.";
            return;
        }

        CurrentPage = AppPage.Dashboard;
        StatusMessage = "Login realizado com sucesso.";
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(RegisterUserName))
        {
            StatusMessage = "Informe um nome de usuario.";
            return;
        }

        if (!_validationService.IsBrazilianPhone(RegisterPhone))
        {
            StatusMessage = "Informe um celular brasileiro valido.";
            return;
        }

        if (!_validationService.IsEmail(RegisterEmail))
        {
            StatusMessage = "Informe um e-mail valido.";
            return;
        }

        if (RegisterPassword.Length < 6 || RegisterPassword != RegisterConfirmPassword)
        {
            StatusMessage = "A senha deve ter 6 caracteres e as confirmacoes precisam ser iguais.";
            return;
        }

        Data.User.UserName = RegisterUserName.TrimStart('@');
        Data.User.FullName = RegisterUserName.Trim();
        Data.User.Phone = RegisterPhone;
        Data.User.Email = RegisterEmail;
        Data.User.Password = RegisterPassword;
        Data.Band.OledText = $"@{Data.User.UserName}";
        CurrentPage = AppPage.Dashboard;
        StatusMessage = "Conta criada com sucesso.";
    }

    private async Task SaveProfileAsync()
    {
        if (!_validationService.IsEmail(Data.User.Email) || string.IsNullOrWhiteSpace(Data.User.UserName))
        {
            StatusMessage = "Revise o e-mail e o nome de usuario.";
            return;
        }

        Data.User.UserName = Data.User.UserName.TrimStart('@');
        Data.Band.OledText = $"@{Data.User.UserName}";
        await _storageService.SaveAsync(Data);
        StatusMessage = "Perfil atualizado com sucesso.";
        CurrentPage = AppPage.Profile;
    }

    private void ShareProfile()
    {
        var payload = _nfcService.BuildProfilePayload(Data.User.UserName);
        StatusMessage = $"Perfil pronto para compartilhar via NFC: {payload}";
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
        StatusMessage = Data.Band.IsConnected
            ? "Pulseira conectada com sucesso."
            : "Nao foi possivel conectar. Tente novamente.";
    }

    private async Task SendBandAsync()
    {
        if (!Data.Band.IsConnected)
        {
            await _storageService.SaveAsync(Data);
            StatusMessage = "Configuracao salva para sincronizar depois.";
            return;
        }

        await _bluetoothService.SendAsync($"{Data.Band.OledText}|{Data.Band.LedHexColor}|{Data.Band.QuickLink}");
        await _storageService.SaveAsync(Data);
        StatusMessage = "Dados enviados para a pulseira.";
    }

    private async Task SaveEmergencyAsync()
    {
        if (string.IsNullOrWhiteSpace(Data.EmergencyProfile.ChildName))
        {
            StatusMessage = "Informe o nome da crianca.";
            return;
        }

        if (!_validationService.IsBloodType(Data.EmergencyProfile.BloodType))
        {
            StatusMessage = "Tipo sanguineo deve ser A+, A-, B+, B-, AB+, AB-, O+ ou O-.";
            return;
        }

        await _storageService.SaveAsync(Data);
        StatusMessage = "Informacoes de emergencia salvas.";
        OnPropertyChanged(nameof(EmergencyUrl));
    }

    private void AddEmergencyContact()
    {
        Data.EmergencyProfile.ExtraContacts.Add(new EmergencyContactModel
        {
            Name = "Contato adicional",
            Relation = "Familiar",
            Phone = "(11) 98888-0000"
        });
    }

    private void AddCustomInfo()
    {
        Data.EmergencyProfile.CustomInfos.Add(new CustomInfoModel
        {
            Title = "Plano de saude",
            Description = "Unimed, carteirinha no 000000"
        });
    }

    private static string NormalizeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "#3B82F6";
        }

        return value.StartsWith('#') ? value : $"#{value}";
    }
}
