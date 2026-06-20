using System.Windows.Controls;

namespace NextBand.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void ShowLoginPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        LoginPasswordTextBox.Text = LoginPasswordBox.Password;
        LoginPasswordBox.Password = LoginPasswordTextBox.Text;
    }
}
