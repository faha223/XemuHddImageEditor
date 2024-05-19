using Avalonia.ReactiveUI;
using ReactiveUI;
using Avalonia.Controls;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}