using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Directory = FatX.Net.Directory;
using XemuHddImageEditor.Helpers;
using XemuHddImageEditor.Views;
using System.Reflection.Metadata;

namespace XemuHddImageEditor.ViewModels;

public class DirectoryViewModel(Directory directory, DirectoryViewModel? parentDirectory) : ViewModelBase, IFileSystemEntry
{
    private Directory _directory = directory;
    public DirectoryViewModel? ParentDirectory { get; init; } = parentDirectory;
    public string Name
    {
        get => _directory.Name;
        set 
        { 
            _directory.Name = value;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(FullName));
        }
    }
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

    public List<IFileSystemEntry> Contents
    {
        get
        {
            var list = new List<IFileSystemEntry>();
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
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainWindowViewModel mainWindowVm)
            {
                mainWindowVm.SelectedDirectory = this;
            }
        }

        Expanded = true;
        return Task.CompletedTask;
    }
    
    public async Task Rename()
    {
        var dialog = new RenameFileModal()
        {
            ViewModel = new RenameFileModalViewModel(this)
        };

        await DialogHelpers.ShowDialog<RenameFileModalViewModel?>(dialog);

        if (dialog.ViewModel.DialogResult)
            Name = dialog.ViewModel.NewFilename;
    }
    
    public async Task Extract()
    {
        var destination = await DialogHelpers.OpenFolderDialog();
        if (destination != null)
        {
            Dictionary<string, Action> tasks = new Dictionary<string, Action>();
            GetExtractTasks(destination, _directory, tasks);

            var dlg = new ProgressTrackerDialog(tasks);
            await DialogHelpers.ShowDialog<ProgressTrackerViewModel?>(dlg);
        }
    }

    public void GetExtractTasks(string destination, Directory directory, Dictionary<string, Action> tasks)
    {
        tasks.Add("Extracting " + directory.FullName, async () => { await directory.Extract(destination); });
        var nextDestination = Path.Combine(destination, directory.Name);
        foreach(var file in directory.Files)
        {
            tasks.Add("Extracting " + file.FullName, async () => { await file.ExtractToDirectory(nextDestination); });
        }
        foreach(var subdirectory in directory.Subdirectories)
        {
            GetExtractTasks(nextDestination, subdirectory, tasks);
        }
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