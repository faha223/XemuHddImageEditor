using ReactiveUI;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace XemuHddImageEditor.ViewModels;

public class RenameFileModalViewModel(IFileSystemEntry file)
{
    public event EventHandler? CloseRequested;
    private readonly IFileSystemEntry _file = file;
    public string FullName => _file.FullName;
    public string Filename => _file.Name;

    [StringLength(FatX.Net.Constants.FATX_MaxFilenameLen)]
    public string NewFilename { get; set; } = file.Name;

    public bool DialogResult { get; private set; } = false;

    public ICommand OkButtonCommand => ReactiveCommand.Create(SaveAndClose);

    private void SaveAndClose()
    {
        if(NewFilename.Length > 0 && NewFilename.Length <= FatX.Net.Constants.FATX_MaxFilenameLen)
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