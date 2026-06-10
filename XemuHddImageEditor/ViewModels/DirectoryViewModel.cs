using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Directory = FatX.Net.Directory;
using XemuHddImageEditor.Helpers;
using XemuHddImageEditor.Views;

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
            _subdirectories ??= [.._directory.Subdirectories
                .Select(d => new DirectoryViewModel(d, this))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            ];
            return _subdirectories;
        }
    }

    private List<FileViewModel>? _files;
    public List<FileViewModel> Files
    {
        get {
            _files ??= [.._directory.Files
                .Select(f => new FileViewModel(f, this))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            ];
            return _files;
        }
    }

    public List<IFileSystemEntry> Contents => [
        ..Subdirectories,
        ..Files
    ];

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

    public Task Close()
    {
        Expanded = false;
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
            Dictionary<string, Action> tasks = [];
            GetExtractTasks(destination, _directory, tasks);

            var dlg = new ProgressTrackerDialog(tasks);
            await DialogHelpers.ShowDialog<ProgressTrackerViewModel?>(dlg);
        }
    }

    public async Task Delete()
    {
        if (ParentDirectory == null)
            return;

        await ParentDirectory.DeleteSubdirectory(this);
    }

    internal async Task InternalDelete()
    {
        await _directory.Delete();
    }

    public async Task DeleteSubdirectory(DirectoryViewModel subdirectory)
    {
        if (!Subdirectories.Contains(subdirectory))
            return;

        await subdirectory.InternalDelete();
        Subdirectories.Remove(subdirectory);

        OnPropertyChanged(nameof(Subdirectories));
        OnPropertyChanged(nameof(Contents));
    }

    public async Task DeleteFile(FileViewModel file)
    {
        if (!Files.Contains(file))
            return;

        await file.InternalDelete();
        Files.Remove(file);

        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(Contents));
    }

    public static void GetExtractTasks(string destination, Directory directory, Dictionary<string, Action> tasks)
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

    private void Refresh()
    {
        _directory.Refresh();
        _files = null;
        _subdirectories = null;
        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(Subdirectories));
        OnPropertyChanged(nameof(Contents));
    }

    public async Task CreateDirectory()
    {
        var dialog = new CreateSubdirectoryModal();
        await DialogHelpers.ShowDialog<CreateSubdirectoryModalViewModel?>(dialog);

        if (dialog.ViewModel.DialogResult)
        {
            await _directory.CreateSubdirectory(dialog.ViewModel.Name);
            Refresh();
        }
    }

    public async Task ImportFiles()
    {
        var result = await DialogHelpers.OpenFilesDialog();
        if (result != null)
        {
            await ImportFiles(_directory, result ?? []);
            Refresh();
        }
    }

    private static async Task ImportFiles(Directory directory, IEnumerable<string> paths)
    {
        Queue<(Directory, string)> importQueue = new (paths.Select(p => (directory, p)));
        while(importQueue.Count > 0)
        {
            (Directory d, string path) = importQueue.Dequeue();
            FileAttributes attr = File.GetAttributes(path);
            if(attr.HasFlag(FileAttributes.Directory))
            {
                var di = new DirectoryInfo(path);
                // Create a new subdirectory in the current directory if one doesn't already exist 
                // with the same name (case-insensitive)
                var subdir = directory.Subdirectories.FirstOrDefault(sd => sd.Name.Equals(di.Name, StringComparison.OrdinalIgnoreCase)) ??
                    await directory.CreateSubdirectory(di.Name);
                if(subdir != null)
                {
                    List<string> subPaths = [
                        ..System.IO.Directory.GetFiles(di.FullName), 
                        ..System.IO.Directory.GetDirectories(di.FullName)
                    ];
                    foreach(var additionalPath in subPaths)
                        importQueue.Enqueue((subdir, additionalPath));
                }
            }
            else
            {
                using var fileStream = File.OpenRead(path);
                var fileInfo = new FileInfo(path);
                var existingFile = directory.Files
                .FirstOrDefault(f => f.Name.Equals(fileInfo.Name, StringComparison.OrdinalIgnoreCase));
                if(existingFile != null)
                {
                    // TODO: If a file already exists with this name, prompt the user to confirm that they
                    // wish to overwrite the existing file, skip the file, or provide an option to import the
                    // file with a different name. For now, we'll just overwrite the existing file.
                    await existingFile.Delete();
                    
                }
                
                await directory.CreateFile(fileInfo, fileStream);
            }
        }
    }
}