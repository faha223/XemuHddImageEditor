using Avalonia.Controls;
using ReactiveUI;
using XemuHddImageEditor.ViewModels;

namespace XemuHddImageEditor.Views;

public partial class RenameFileModal : Window
{
    private RenameFileModalViewModel? _vm;
    public RenameFileModalViewModel? ViewModel
    {
        get => _vm;
        set
        {
            if(_vm != null)
                _vm.CloseRequested -= vm_CloseRequested;

            DataContext = _vm = value;

            if(_vm != null)
                _vm.CloseRequested += vm_CloseRequested;
        }
    }

    public RenameFileModal()
    {
        InitializeComponent();
    }

    private void vm_CloseRequested(object sender, EventArgs e)
    {
        Close();
    }
}