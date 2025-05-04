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
        InitializeComponent();

        this.AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Drop");
    }
}