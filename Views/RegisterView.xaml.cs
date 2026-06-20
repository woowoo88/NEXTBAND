using System.Windows.Controls;

namespace NextBand.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();
    }

    private void ShowRegisterPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        RegisterPasswordTextBox.Text = RegisterPasswordBox.Password;
        RegisterPasswordBox.Password = RegisterPasswordTextBox.Text;
    }

    private void ShowRegisterConfirmPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        RegisterConfirmPasswordTextBox.Text = RegisterConfirmPasswordBox.Password;
        RegisterConfirmPasswordBox.Password = RegisterConfirmPasswordTextBox.Text;
    }
}
