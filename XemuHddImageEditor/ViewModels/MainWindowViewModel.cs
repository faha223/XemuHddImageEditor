using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using XemuHddImageEditor.Helpers;

namespace XemuHddImageEditor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public DirectoryTreeViewModel DirectoryTreeVM { get; } = new();

    public bool CanGoUp => SelectedDirectory?.ParentDirectory != null;

    public DirectoryViewModel? SelectedDirectory
    {
        get => DirectoryTreeVM.SelectedDirectory;
        set 
        {
            DirectoryTreeVM.SelectedDirectory = value;
            OnPropertyChanged(nameof(SelectedDirectory));
            OnPropertyChanged(nameof(SelectedDirectoryPath));
            OnPropertyChanged(nameof(CanGoUp));
        }
    }

    public string SelectedDirectoryPath
    {
        get => DirectoryTreeVM.SelectedDirectoryPath;
        set 
        {
            DirectoryTreeVM.SelectedDirectoryPath = value;
            OnPropertyChanged(nameof(SelectedDirectory));
            OnPropertyChanged(nameof(SelectedDirectoryPath));
            OnPropertyChanged(nameof(CanGoUp));
        }
    }

    public string? SearchQuery { get; set; } = null;

    public MainWindowViewModel()
    {
        DirectoryTreeVM.SelectedDirectoryChanged += DirectoryTreeVM_SelectedDirectoryChanged;
    }

    private void DirectoryTreeVM_SelectedDirectoryChanged(object? sender, string newValue)
    {
        // IDK
        OnPropertyChanged(nameof(SelectedDirectory));
        OnPropertyChanged(nameof(SelectedDirectoryPath));
        OnPropertyChanged(nameof(CanGoUp));
    }

    public void GoToParentDirectory()
    {
        if (SelectedDirectory?.ParentDirectory != null)
            DirectoryTreeVM.SelectedDirectory = SelectedDirectory.ParentDirectory;
    }

    public async Task LoadImage()
    {
        var result = await DialogHelpers.OpenFileDialog();
        if (result != null)
        {
            DirectoryTreeVM.LoadImage(result);
            OnPropertyChanged(nameof(DirectoryTreeVM));
        }
    }

    public void Close()
    {
        if(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    public void Directory_Drop(object? sender, DragEventArgs e)
    {   
        System.Diagnostics.Debug.WriteLine($"Drop Event: {SelectedDirectoryPath}");
    }
}