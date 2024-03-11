using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Directory = FatX.Net.Directory;
using XemuHddImageEditor.Helpers;

namespace XemuHddImageEditor.ViewModels;

public class DirectoryViewModel(Directory directory, DirectoryViewModel? parentDirectory) : ViewModelBase
{
    private Directory _directory = directory;
    public DirectoryViewModel? ParentDirectory { get; init; } = parentDirectory;
    public string Name => _directory.Name;
    public string FullName => _directory.FullName;

    private List<DirectoryViewModel>? _subdirectories;
    public List<DirectoryViewModel> Subdirectories
    {
        get
        {
            _subdirectories ??= _directory.Subdirectories
                .Select(d => new DirectoryViewModel(d, this))
                .ToList();

            return _subdirectories;
        }
    }

    private List<FileViewModel>? _files;
    public List<FileViewModel> Files
    {
        get
        {
            _files ??= _directory.Files
                .Select(f => new FileViewModel(f, this))
                .ToList();
            return _files;
        }
    }

    public List<object> Contents
    {
        get
        {
            var list = new List<object>();
            list.AddRange(Subdirectories);
            list.AddRange(Files);
            return list;
        }
    }

    private bool _expanded = false;
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (value && ParentDirectory != null)
                ParentDirectory.Expanded = value;
            _expanded = value;
            OnPropertyChanged(nameof(Expanded));
        }
    }

    public Task Open()
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow.DataContext is MainWindowViewModel mainWindowVm)
            {
                mainWindowVm.SelectedDirectory = this;
            }
        }

        Expanded = true;
        return Task.CompletedTask;
    }
    
    public Task Rename()
    {
        // var dialog = new RenameFileModal()
        // {
        //     ViewModel = new RenameFileModalViewModel(this)
        // };
        //
        // await DialogHelpers.ShowDialog<RenameFileModalViewModel?>(dialog);
        //
        // if (dialog.ViewModel.DialogResult)
        //     Name = dialog.ViewModel.NewFilename;
        return Task.CompletedTask;
    }
    
    public async Task Extract()
    {
        var destination = await DialogHelpers.OpenFolderDialog();
        if (destination != null)
            await _directory.Extract(destination, true);
    }

    public async Task ImportFiles()
    {
        var result = await DialogHelpers.OpenFilesDialog();
        if (result != null)
        {
            foreach (var item in result)
            {
                // TODO: Import the Files
            }
        }
    }
}