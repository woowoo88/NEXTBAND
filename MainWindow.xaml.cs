using System.Windows;
using NextBand.ViewModels;

namespace NextBand;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
