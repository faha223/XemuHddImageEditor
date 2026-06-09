using Avalonia.Controls;
using ReactiveUI;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views;

public partial class CreateSubdirectoryModal : Window
{
    private CreateSubdirectoryModalViewModel _vm = new();
    public CreateSubdirectoryModalViewModel ViewModel => _vm;

    public CreateSubdirectoryModal()
    {
        InitializeComponent();
        _vm.CloseRequested += vm_CloseRequested;
        DataContext = _vm;
    }

    private void vm_CloseRequested(object? sender, EventArgs e)
    {
        _vm.CloseRequested -= vm_CloseRequested;
        Close();
    }
}