using System.Windows.Input;
using ReactiveUI;

namespace XemuHddImageEditor.ViewModels;

public class RenameFileModalViewModel(FileViewModel file)
{
    public event EventHandler? CloseRequested;
    private readonly FileViewModel _file = file;
    public string FullName => _file.FullName;
    public string Filename => _file.Name;

    public string NewFilename { get; set; } = file.Name;

    public bool DialogResult { get; private set; } = false;

    public ICommand OkButtonCommand => ReactiveCommand.Create(SaveAndClose);

    private void SaveAndClose()
    {
        Console.WriteLine("Save and Close Called");
        DialogResult = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public ICommand CancelButtonCommand => ReactiveCommand.Create(CloseWithoutSaving);

    private void CloseWithoutSaving()
    {
        Console.WriteLine("Close Without Saving Called");
        DialogResult = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}