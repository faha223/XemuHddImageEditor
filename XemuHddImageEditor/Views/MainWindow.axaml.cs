using Avalonia.Controls;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views;

public partial class MainWindow : Window
{
    MainWindowViewModel vm;

    public MainWindow()
    {
        InitializeComponent();

        vm = (DataContext as MainWindowViewModel)!;
    }
}