using ReactiveUI;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace XemuHddImageEditor.ViewModels;

public class CreateSubdirectoryModalViewModel
{
    public event EventHandler? CloseRequested;

    [StringLength(FatX.Net.Constants.FATX_MaxFilenameLen)]
    public string Name { get; set; } = string.Empty;

    public bool DialogResult { get; private set; } = false;

    public ICommand OkButtonCommand => ReactiveCommand.Create(SaveAndClose);

    private void SaveAndClose()
    {
        if(Name.Length > 0 && Name.Length <= FatX.Net.Constants.FATX_MaxFilenameLen)
        {
            DialogResult = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public ICommand CancelButtonCommand => ReactiveCommand.Create(CloseWithoutSaving);

    private void CloseWithoutSaving()
    {
        DialogResult = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}