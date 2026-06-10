using Avalonia.ReactiveUI;
using XemuHddImageEditor.ViewModels;
using Avalonia.Input;

namespace XemuHddImageEditor.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly MainWindowViewModel _viewModel = new ();
    public MainWindow()
    {
        DataContext = _viewModel;

        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MainWindow.OnDrop");
    }
}