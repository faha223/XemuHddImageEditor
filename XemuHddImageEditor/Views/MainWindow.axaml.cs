using Avalonia.ReactiveUI;
using ReactiveUI;
using Avalonia.Controls;
using XemuHddImageEditor.ViewModels;
using Avalonia.Input;

namespace XemuHddImageEditor.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        DataContext = new MainWindowViewModel();

        InitializeComponent();
        
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MainWindow.OnDrop");
    }
}